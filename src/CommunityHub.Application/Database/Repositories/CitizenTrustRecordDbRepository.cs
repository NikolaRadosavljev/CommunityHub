using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;


namespace CommunityHub.Application.Database.Repositories;

public class CitizenTrustRecordDbRepository : ICitizenTrustRecordRepository
{
    public List<CitizenTrustRecord> GetByNeighborhoodId(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                m.citizen_id,
                m.neighborhood_id,
                m.joined_at,
                u.name,
                u.surname
            FROM neighborhood_memberships m
            JOIN users u ON u.id = m.citizen_id
            WHERE m.neighborhood_id = @neighborhoodId";

        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        var records = new List<CitizenTrustRecord>();

        while (reader.Read())
        {
            records.Add(new CitizenTrustRecord(
                Convert.ToInt64(reader["citizen_id"]),
                Convert.ToInt64(reader["neighborhood_id"]),
                $"{reader["name"]} {reader["surname"]}",
                0, // OrganizedEvents - biće dopunjeno kad kolega implementira događaje
                0, // VolunteeredEvents - isto
                Convert.ToDateTime(reader["joined_at"])
            ));
        }

        return records;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}