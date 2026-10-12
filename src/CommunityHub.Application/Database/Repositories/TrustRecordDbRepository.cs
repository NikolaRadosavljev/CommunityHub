using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class TrustRecordDbRepository : ITrustRecordRepository
{
    public TrustRecord? GetByCitizenId(long citizenId, long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = BuildGetByCitizenQuery();
        AddParameter(command, "@citizenId", citizenId);
        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadTrustRecord(reader);
    }

    public List<(Citizen citizen, TrustRecord record)> GetByNeighborhoodId(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = BuildGetByNeighborhoodQuery();
        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        var results = new List<(Citizen, TrustRecord)>();
        while (reader.Read())
            results.Add((ReadCitizen(reader), ReadTrustRecord(reader)));
        return results;
    }

    private string BuildGetByCitizenQuery() => @"
        SELECT
            (SELECT COUNT(*) FROM events e
             WHERE e.organizer_id = @citizenId
             AND e.neighborhood_id = @neighborhoodId
             AND e.status = 'Finished') AS organized,
            (SELECT COUNT(*) FROM event_volunteers ev
             JOIN events e ON e.id = ev.event_id
             WHERE ev.citizen_id = @citizenId
             AND e.neighborhood_id = @neighborhoodId
             AND ev.attended = true) AS volunteered,
            nm.joined_at
        FROM neighborhood_memberships nm
        WHERE nm.citizen_id = @citizenId
        AND nm.neighborhood_id = @neighborhoodId";

    private string BuildGetByNeighborhoodQuery() => @"
        SELECT
            u.id, u.username, u.password, u.name, u.surname, u.birthday, u.role,
            c.address,
            (SELECT COUNT(*) FROM events e
             WHERE e.organizer_id = u.id
             AND e.neighborhood_id = @neighborhoodId
             AND e.status = 'Finished') AS organized,
            (SELECT COUNT(*) FROM event_volunteers ev
             JOIN events e ON e.id = ev.event_id
             WHERE ev.citizen_id = u.id
             AND e.neighborhood_id = @neighborhoodId
             AND ev.attended = true) AS volunteered,
            nm.joined_at
        FROM neighborhood_memberships nm
        JOIN users u ON u.id = nm.citizen_id
        JOIN citizens c ON c.user_id = u.id
        WHERE nm.neighborhood_id = @neighborhoodId";

    private TrustRecord ReadTrustRecord(IDataReader reader)
    {
        int organized = Convert.ToInt32(reader["organized"]);
        int volunteered = Convert.ToInt32(reader["volunteered"]);
        DateTime joinedAt = Convert.ToDateTime(reader["joined_at"]);
        return new TrustRecord(organized, volunteered, joinedAt);
    }

    private Citizen ReadCitizen(IDataReader reader)
    {
        User user = new User(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetDateTime(5),
            UserRole.Citizen
        );
        return new Citizen(user, reader.GetString(7));
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}