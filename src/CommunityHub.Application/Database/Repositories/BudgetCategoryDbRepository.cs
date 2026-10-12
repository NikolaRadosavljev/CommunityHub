using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class BudgetCategoryDbRepository : IBudgetCategoryRepository
{
    public List<BudgetCategory> GetByNeighborhoodId(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, neighborhood_id, name, total_amount
            FROM budget_categories
            WHERE neighborhood_id = @neighborhoodId";

        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<BudgetCategory>();

        while (reader.Read())
        {
            result.Add(new BudgetCategory(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["neighborhood_id"]),
                reader["name"].ToString()!,
                Convert.ToDecimal(reader["total_amount"])
            ));
        }

        return result;
    }

    public void UpdateAmount(long categoryId, decimal newAmount)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE budget_categories SET total_amount = @amount WHERE id = @id";

        AddParameter(command, "@amount", newAmount);
        AddParameter(command, "@id", categoryId);
        command.ExecuteNonQuery();
    }

    public long Create(BudgetCategory category)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO budget_categories (neighborhood_id, name, total_amount)
            VALUES (@neighborhoodId, @name, @totalAmount)
            RETURNING id";

        AddParameter(command, "@neighborhoodId", category.NeighborhoodId);
        AddParameter(command, "@name", category.Name);
        AddParameter(command, "@totalAmount", category.TotalAmount);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}