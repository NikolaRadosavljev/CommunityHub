using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CoordinatorReviewDbRepository : ICoordinatorReviewRepository
{
    public List<CoordinatorReview> GetByCoordinatorId(long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, coordinator_id, citizen_id, rating, comment, created_at, is_removed
            FROM coordinator_reviews
            WHERE coordinator_id = @coordinatorId AND is_removed = FALSE
            ORDER BY created_at DESC";

        AddParameter(command, "@coordinatorId", coordinatorId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<CoordinatorReview>();

        while (reader.Read())
        {
            result.Add(new CoordinatorReview(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["coordinator_id"]),
                Convert.ToInt64(reader["citizen_id"]),
                Convert.ToInt32(reader["rating"]),
                reader["comment"] == DBNull.Value ? null : reader["comment"].ToString(),
                Convert.ToDateTime(reader["created_at"]),
                Convert.ToBoolean(reader["is_removed"])
            ));
        }

        return result;
    }

    public double GetAverageRatingLastYear(long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COALESCE(AVG(rating), 0)
            FROM coordinator_reviews
            WHERE coordinator_id = @coordinatorId
            AND is_removed = FALSE
            AND created_at >= NOW() - INTERVAL '1 year'";

        AddParameter(command, "@coordinatorId", coordinatorId);
        return Convert.ToDouble(command.ExecuteScalar());
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}