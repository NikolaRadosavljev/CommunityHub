using System.Data;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using Npgsql;
using NpgsqlTypes;

namespace CommunityHub.Application.Database.Repositories;

/// <summary>
/// Implementacija repozitorijuma za pristup podacima o zgradama iz PostgreSQL baze
/// </summary>
public class BuildingDbRepository : IBuildingRepository
{
    public Building Create(Building building, List<Flat> flats)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        try
        {
            IDbCommand buildingCommand = connection.CreateCommand();
            buildingCommand.Transaction = transaction;
            buildingCommand.CommandText = @"
                INSERT INTO buildings (address, settlement, city, country, floor_number, manager_id)
                VALUES (@address, @settlement, @city, @country, @floorNumber, @managerId)
                RETURNING id";

            AddParameter(buildingCommand, "@address", building.Address);
            AddParameter(buildingCommand, "@settlement", building.Settlement);
            AddParameter(buildingCommand, "@city", building.City);
            AddParameter(buildingCommand, "@country", building.Country);
            AddParameter(buildingCommand, "@floorNumber", building.FloorNumber);
            AddParameter(buildingCommand, "@managerId", building.ManagerId);

            int buildingId = Convert.ToInt32(buildingCommand.ExecuteScalar());

            foreach (Flat flat in flats)
            {
                IDbCommand flatCommand = connection.CreateCommand();
                flatCommand.Transaction = transaction;
                flatCommand.CommandText = @"
                    INSERT INTO flats (building_id, floor_number, flat_number)
                    VALUES (@buildingId, @floorNumber, @flatNumber)";

                AddParameter(flatCommand, "@buildingId", buildingId);
                AddParameter(flatCommand, "@floorNumber", flat.FloorNumber);
                AddParameter(flatCommand, "@flatNumber", flat.FlatNumber);

                flatCommand.ExecuteNonQuery();
            }

            foreach (string picturePath in building.Pictures)
            {
                IDbCommand pictureCommand = connection.CreateCommand();
                pictureCommand.Transaction = transaction;
                pictureCommand.CommandText = @"
                    INSERT INTO building_pictures (building_id, picture_path)
                    VALUES (@buildingId, @picturePath)";

                AddParameter(pictureCommand, "@buildingId", buildingId);
                AddParameter(pictureCommand, "@picturePath", picturePath);

                pictureCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return new Building(
                buildingId,
                building.Address,
                building.Settlement,
                building.City,
                building.Country,
                building.FloorNumber,
                building.FlatNumbers,
                building.Pictures,
                building.ManagerId
            );
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public List<Building> GetByManagerId(long managerId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT b.id, b.address, b.settlement, b.city, b.country, b.floor_number, b.manager_id,
                   f.flat_number, bp.picture_path
            FROM buildings b
            LEFT JOIN flats f ON b.id = f.building_id
            LEFT JOIN building_pictures bp ON b.id = bp.building_id
            WHERE b.manager_id = @managerId";

        AddParameter(command, "@managerId", managerId);

        using IDataReader reader = command.ExecuteReader();
        var buildingDict = new Dictionary<int, Building>();

        while (reader.Read())
        {
            int id = reader.GetInt32(reader.GetOrdinal("id"));

            if (!buildingDict.TryGetValue(id, out Building? building))
            {
                building = new Building(
                    id,
                    reader.GetString(reader.GetOrdinal("address")),
                    reader.GetString(reader.GetOrdinal("settlement")),
                    reader.GetString(reader.GetOrdinal("city")),
                    reader.GetString(reader.GetOrdinal("country")),
                    reader.GetInt32(reader.GetOrdinal("floor_number")),
                    new List<int>(),
                    new List<string>(),
                    reader.IsDBNull(reader.GetOrdinal("manager_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("manager_id"))
                );
                buildingDict[id] = building;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("flat_number")))
            {
                int flatNum = reader.GetInt32(reader.GetOrdinal("flat_number"));
                if (!building.FlatNumbers.Contains(flatNum))
                {
                    building.FlatNumbers.Add(flatNum);
                }
            }

            if (!reader.IsDBNull(reader.GetOrdinal("picture_path")))
            {
                string picPath = reader.GetString(reader.GetOrdinal("picture_path"));
                if (!building.Pictures.Contains(picPath))
                {
                    building.Pictures.Add(picPath);
                }
            }
        }

        return [.. buildingDict.Values];
    }

    public Building? GetById(int id)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT b.id, b.address, b.settlement, b.city, b.country, b.floor_number, b.manager_id,
                   f.flat_number, bp.picture_path
            FROM buildings b
            LEFT JOIN flats f ON b.id = f.building_id
            LEFT JOIN building_pictures bp ON b.id = bp.building_id
            WHERE b.id = @id";

        AddParameter(command, "@id", id);

        using IDataReader reader = command.ExecuteReader();
        Building? result = null;

        while (reader.Read())
        {
            if (result == null)
            {
                result = new Building(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("address")),
                    reader.GetString(reader.GetOrdinal("settlement")),
                    reader.GetString(reader.GetOrdinal("city")),
                    reader.GetString(reader.GetOrdinal("country")),
                    reader.GetInt32(reader.GetOrdinal("floor_number")),
                    new List<int>(),
                    new List<string>(),
                    reader.IsDBNull(reader.GetOrdinal("manager_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("manager_id"))
                );
            }

            if (!reader.IsDBNull(reader.GetOrdinal("flat_number")))
            {
                int flatNum = reader.GetInt32(reader.GetOrdinal("flat_number"));
                if (!result.FlatNumbers.Contains(flatNum))
                {
                    result.FlatNumbers.Add(flatNum);
                }
            }

            if (!reader.IsDBNull(reader.GetOrdinal("picture_path")))
            {
                string picPath = reader.GetString(reader.GetOrdinal("picture_path"));
                if (!result.Pictures.Contains(picPath))
                {
                    result.Pictures.Add(picPath);
                }
            }
        }

        return result;
    }

    private static void AddNullableStringParameter(IDbCommand command, string name, string? value)
    {
        var parameter = (Npgsql.NpgsqlParameter)command.CreateParameter();
        parameter.ParameterName = name;
        parameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text;
        parameter.Value = value == null ? DBNull.Value : value;
        command.Parameters.Add(parameter);
    }

    public int GetOccupancyCount(int buildingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(DISTINCT bm.flat_number) as occupancy_count
            FROM building_memberships bm
            WHERE bm.building_id = @buildingId";

        AddParameter(command, "@buildingId", buildingId);

        object? result = command.ExecuteScalar();
        return result != null && int.TryParse(result.ToString(), out int count) ? count : 0;
    }

    public List<Building> Search(string? address, string? settlement, string? city, string? country)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT b.id, b.address, b.settlement, b.city, b.country, b.floor_number, b.manager_id,
                   f.flat_number, bp.picture_path
            FROM buildings b
            LEFT JOIN flats f ON b.id = f.building_id
            LEFT JOIN building_pictures bp ON b.id = bp.building_id
            WHERE (@address IS NULL OR b.address ILIKE '%' || @address || '%')
              AND (@settlement IS NULL OR b.settlement ILIKE '%' || @settlement || '%')
              AND (@city IS NULL OR b.city ILIKE '%' || @city || '%')
              AND (@country IS NULL OR b.country ILIKE '%' || @country || '%')";

        AddNullableStringParameter(command, "@address", string.IsNullOrEmpty(address) ? null : address);
        AddNullableStringParameter(command, "@settlement", string.IsNullOrEmpty(settlement) ? null : settlement);
        AddNullableStringParameter(command, "@city", string.IsNullOrEmpty(city) ? null : city);
        AddNullableStringParameter(command, "@country", string.IsNullOrEmpty(country) ? null : country);

        using IDataReader reader = command.ExecuteReader();
        var buildingDict = new Dictionary<int, Building>();

        while (reader.Read())
        {
            int id = reader.GetInt32(reader.GetOrdinal("id"));

            if (!buildingDict.TryGetValue(id, out Building? building))
            {
                building = new Building(
                    id,
                    reader.GetString(reader.GetOrdinal("address")),
                    reader.GetString(reader.GetOrdinal("settlement")),
                    reader.GetString(reader.GetOrdinal("city")),
                    reader.GetString(reader.GetOrdinal("country")),
                    reader.GetInt32(reader.GetOrdinal("floor_number")),
                    new List<int>(),
                    new List<string>(),
                    reader.IsDBNull(reader.GetOrdinal("manager_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("manager_id"))
                );
                buildingDict[id] = building;
            }

            if (!reader.IsDBNull(reader.GetOrdinal("flat_number")))
            {
                int flatNum = reader.GetInt32(reader.GetOrdinal("flat_number"));
                if (!building.FlatNumbers.Contains(flatNum))
                {
                    building.FlatNumbers.Add(flatNum);
                }
            }

            if (!reader.IsDBNull(reader.GetOrdinal("picture_path")))
            {
                string picPath = reader.GetString(reader.GetOrdinal("picture_path"));
                if (!building.Pictures.Contains(picPath))
                {
                    building.Pictures.Add(picPath);
                }
            }
        }

        return [.. buildingDict.Values];
    }
}
