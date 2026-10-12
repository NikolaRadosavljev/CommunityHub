using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CityObjectDbRepository : ICityObjectRepository
{
    public List<CityObject> GetAll()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, description, vote_count, last_visit_date
            FROM city_objects
            ORDER BY name";

        using IDataReader reader = command.ExecuteReader();
        var result = new List<CityObject>();

        while (reader.Read())
        {
            result.Add(new CityObject(
                Convert.ToInt64(reader["id"]),
                reader["name"].ToString()!,
                reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                Convert.ToInt32(reader["vote_count"]),
               reader["last_visit_date"] == DBNull.Value ? null :
    reader["last_visit_date"] is DateOnly d ? d :
    DateOnly.FromDateTime(Convert.ToDateTime(reader["last_visit_date"]))
            ));
        }

        return result;
    }

    public void ResetVotes(long cityObjectId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE city_objects SET vote_count = 0 WHERE id = @id";

        AddParameter(command, "@id", cityObjectId);
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