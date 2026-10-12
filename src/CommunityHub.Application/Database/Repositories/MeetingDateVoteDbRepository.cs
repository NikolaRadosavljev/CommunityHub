using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class MeetingDateVoteDbRepository : IMeetingDateVoteRepository
{
    public void Create(MeetingDateVote vote)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO meeting_date_votes (meeting_id, citizen_id, voted_date)
            VALUES (@meetingId, @citizenId, @votedDate)";

        AddParameter(command, "@meetingId", vote.MeetingId);
        AddParameter(command, "@citizenId", vote.CitizenId);
        AddParameter(command, "@votedDate", vote.VotedDate.ToDateTime(TimeOnly.MinValue));

        command.ExecuteNonQuery();
    }

    public void Update(long meetingId, long citizenId, DateOnly newDate)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE meeting_date_votes
            SET voted_date = @votedDate
            WHERE meeting_id = @meetingId AND citizen_id = @citizenId";

        AddParameter(command, "@votedDate", newDate.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@meetingId", meetingId);
        AddParameter(command, "@citizenId", citizenId);

        command.ExecuteNonQuery();
    }

    public List<MeetingDateVote> GetByMeetingId(long meetingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, meeting_id, citizen_id, voted_date
            FROM meeting_date_votes
            WHERE meeting_id = @meetingId";

        AddParameter(command, "@meetingId", meetingId);

        using IDataReader reader = command.ExecuteReader();
        var votes = new List<MeetingDateVote>();

        while (reader.Read())
        {
            var votedDateValue = reader["voted_date"];
            DateOnly votedDate = votedDateValue is DateOnly d
                ? d
                : DateOnly.FromDateTime(Convert.ToDateTime(votedDateValue));
            votes.Add(new MeetingDateVote(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["meeting_id"]),
                Convert.ToInt64(reader["citizen_id"]),
                votedDate
            ));
        }

        return votes;
    }

    public DateOnly? GetWinningDate(long meetingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT voted_date, COUNT(*) as vote_count
            FROM meeting_date_votes
            WHERE meeting_id = @meetingId
            GROUP BY voted_date
            ORDER BY vote_count DESC
            LIMIT 2";

        AddParameter(command, "@meetingId", meetingId);

        using IDataReader reader = command.ExecuteReader();
        var results = new List<(DateOnly date, long count)>();

        while (reader.Read())
        {
            results.Add((
                DateOnly.FromDateTime(Convert.ToDateTime(reader["voted_date"])),
                Convert.ToInt64(reader["vote_count"])
            ));
        }

        if (results.Count == 0) return null;
        if (results.Count == 1) return results[0].date;
        if (results[0].count == results[1].count) return null; // tie

        return results[0].date;
    }

    public bool HasVote(long meetingId, long citizenId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM meeting_date_votes
            WHERE meeting_id = @meetingId AND citizen_id = @citizenId";

        AddParameter(command, "@meetingId", meetingId);
        AddParameter(command, "@citizenId", citizenId);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}