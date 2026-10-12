using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class MeetingDbRepository : IMeetingRepository
{
    public long Create(Meeting meeting)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO meetings (neighborhood_id, topic, time, start_date, end_date, status, scheduled_date)
            VALUES (@neighborhoodId, @topic, @time, @startDate, @endDate, @status, @scheduledDate)
            RETURNING id";

        AddParameter(command, "@neighborhoodId", meeting.NeighborhoodId);
        AddParameter(command, "@topic", meeting.Topic.ToString());
        AddParameter(command, "@time", meeting.Time.ToTimeSpan());
        AddParameter(command, "@startDate", meeting.StartDate.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@endDate", meeting.EndDate.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@status", meeting.Status.ToString());
        AddParameter(command, "@scheduledDate", (object?)null ?? DBNull.Value);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void UpdateStatus(long meetingId, MeetingStatus status, DateOnly? scheduledDate)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE meetings
            SET status = @status, scheduled_date = @scheduledDate
            WHERE id = @id";

        AddParameter(command, "@status", status.ToString());
        AddParameter(command, "@scheduledDate", scheduledDate.HasValue
            ? (object)scheduledDate.Value.ToDateTime(TimeOnly.MinValue)
            : DBNull.Value);
        AddParameter(command, "@id", meetingId);

        command.ExecuteNonQuery();
    }

    public List<Meeting> GetByNeighborhoodId(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, neighborhood_id, topic, time, start_date, end_date, status, scheduled_date
            FROM meetings
            WHERE neighborhood_id = @neighborhoodId
            ORDER BY start_date";

        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        return ReadMeetings(reader);
    }

    public List<Meeting> GetByCoordinatorId(long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT m.id, m.neighborhood_id, m.topic, m.time, m.start_date, m.end_date, m.status, m.scheduled_date
            FROM meetings m
            JOIN neighborhoods n ON n.id = m.neighborhood_id
            WHERE n.coordinator_id = @coordinatorId
            ORDER BY m.start_date";

        AddParameter(command, "@coordinatorId", coordinatorId);

        using IDataReader reader = command.ExecuteReader();
        return ReadMeetings(reader);
    }

    public List<Meeting> GetInPreparationWithStartDateTomorrow()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, neighborhood_id, topic, time, start_date, end_date, status, scheduled_date
            FROM meetings
            WHERE status = 'InPreparation'
            AND start_date = @tomorrow";

        AddParameter(command, "@tomorrow", DateTime.UtcNow.Date.AddDays(1));

        using IDataReader reader = command.ExecuteReader();
        return ReadMeetings(reader);
    }

    private List<Meeting> ReadMeetings(IDataReader reader)
    {
        var meetings = new List<Meeting>();

        while (reader.Read())
        {
            long id = Convert.ToInt64(reader["id"]);
            long neighborhoodId = Convert.ToInt64(reader["neighborhood_id"]);
            MeetingTopic topic = Enum.Parse<MeetingTopic>(reader["topic"].ToString()!);

            TimeOnly time;
            var timeVal = reader["time"];
            if (timeVal is TimeSpan ts)
                time = TimeOnly.FromTimeSpan(ts);
            else
                time = TimeOnly.Parse(timeVal.ToString()!);

            DateOnly startDate = reader["start_date"] is DateOnly sd ? sd : DateOnly.FromDateTime(Convert.ToDateTime(reader["start_date"]));
            DateOnly endDate = reader["end_date"] is DateOnly ed ? ed : DateOnly.FromDateTime(Convert.ToDateTime(reader["end_date"]));

            MeetingStatus status = Enum.Parse<MeetingStatus>(reader["status"].ToString()!);
            DateOnly? scheduledDate = reader["scheduled_date"] == DBNull.Value
    ? null
    : reader["scheduled_date"] is DateOnly scd ? scd : DateOnly.FromDateTime(Convert.ToDateTime(reader["scheduled_date"]));

            meetings.Add(new Meeting(id, neighborhoodId, topic, time, startDate, endDate, status, scheduledDate));
        }

        return meetings;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
} 