using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using Npgsql;

namespace CommunityHub.Application.Database.Repositories;

public class CommonRoomDbRepository
{
    public List<CommonRoom> GetAllByBuildingId(int buildingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, building_id, name, description, floor_number, rental_type
            FROM common_spaces
            WHERE building_id = @buildingId
            ORDER BY floor_number, name";

        AddParameter(command, "@buildingId", buildingId);

        var rooms = new List<CommonRoom>();
        using IDataReader reader = command.ExecuteReader();
        {
            while (reader.Read())
            {
                rooms.Add(new CommonRoom(
                    id: Convert.ToInt32(reader["id"]),
                    buildingId: Convert.ToInt32(reader["building_id"]),
                    name: reader["name"].ToString(),
                    description: reader["description"].ToString(),
                    floor: Convert.ToInt32(reader["floor_number"]),
                    rentalType: reader["rental_type"].ToString()
                ));
            }
        }
        return rooms;
    }

    public CommonRoom GetById(int id)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, building_id, name, description, floor_number, rental_type
            FROM common_spaces
            WHERE id = @id";

        AddParameter(command, "@id", id);

        using IDataReader reader = command.ExecuteReader();
        {
            if (reader.Read())
            {
                return new CommonRoom(
                    id: Convert.ToInt32(reader["id"]),
                    buildingId: Convert.ToInt32(reader["building_id"]),
                    name: reader["name"].ToString(),
                    description: reader["description"].ToString(),
                    floor: Convert.ToInt32(reader["floor_number"]),
                    rentalType: reader["rental_type"].ToString()
                );
            }
        }
        return null;
    }

    private static void AddParameter(IDbCommand command, string parameterName, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
