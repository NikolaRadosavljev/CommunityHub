using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class DonationDbRepository : IDonationRepository
{
    public List<Donation> GetByCategoryId(long categoryId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, category_id, citizen_id, amount, donated_at
            FROM donations
            WHERE category_id = @categoryId
            ORDER BY donated_at DESC";

        AddParameter(command, "@categoryId", categoryId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<Donation>();

        while (reader.Read())
        {
            result.Add(new Donation(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["category_id"]),
                Convert.ToInt64(reader["citizen_id"]),
                Convert.ToDecimal(reader["amount"]),
                Convert.ToDateTime(reader["donated_at"])
            ));
        }

        return result;
    }

    public void Create(Donation donation)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO donations (category_id, citizen_id, amount, donated_at)
            VALUES (@categoryId, @citizenId, @amount, @donatedAt)";

        AddParameter(command, "@categoryId", donation.CategoryId);
        AddParameter(command, "@citizenId", donation.CitizenId);
        AddParameter(command, "@amount", donation.Amount);
        AddParameter(command, "@donatedAt", donation.DonatedAt);

        command.ExecuteNonQuery();
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}