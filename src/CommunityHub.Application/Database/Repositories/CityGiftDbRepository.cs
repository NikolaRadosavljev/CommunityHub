using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CityGiftDbRepository : ICityGiftRepository
{
    public List<CityGift> GetAvailable()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, amount, deadline, is_assigned
            FROM city_gifts
            WHERE is_assigned = FALSE
            ORDER BY deadline";

        using IDataReader reader = command.ExecuteReader();
        var result = new List<CityGift>();

        while (reader.Read())
        {
            var deadline = reader["deadline"] is DateOnly d ? d :
                DateOnly.FromDateTime(Convert.ToDateTime(reader["deadline"]));

            result.Add(new CityGift(
                Convert.ToInt64(reader["id"]),
                Convert.ToDecimal(reader["amount"]),
                deadline,
                Convert.ToBoolean(reader["is_assigned"])
            ));
        }

        return result;
    }

    public void Apply(long cityGiftId, long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO city_gift_applications (city_gift_id, neighborhood_id)
            VALUES (@cityGiftId, @neighborhoodId)
            ON CONFLICT DO NOTHING";

        AddParameter(command, "@cityGiftId", cityGiftId);
        AddParameter(command, "@neighborhoodId", neighborhoodId);
        command.ExecuteNonQuery();
    }

    public bool HasApplied(long cityGiftId, long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM city_gift_applications a
            JOIN neighborhoods n ON n.id = a.neighborhood_id
            WHERE a.city_gift_id = @cityGiftId
            AND n.coordinator_id = @coordinatorId";

        AddParameter(command, "@cityGiftId", cityGiftId);
        AddParameter(command, "@coordinatorId", coordinatorId);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    public void Assign(long cityGiftId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE city_gifts SET is_assigned = TRUE WHERE id = @id";

        AddParameter(command, "@id", cityGiftId);
        command.ExecuteNonQuery();
    }

    public long? GetNeighborhoodWithLowestBudget(long cityGiftId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT n.id FROM neighborhoods n
            JOIN city_gift_applications a ON a.neighborhood_id = n.id
            WHERE a.city_gift_id = @cityGiftId
            ORDER BY n.budget ASC
            LIMIT 1";

        AddParameter(command, "@cityGiftId", cityGiftId);
        var result = command.ExecuteScalar();
        return result == null || result == DBNull.Value ? null : Convert.ToInt64(result);
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}