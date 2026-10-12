using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CommonSpaceDbRepository : ICommonSpaceRepository
{
    // Npgsql vraca DateOnly za DATE kolone, pa koristimo ovaj helper
    private static DateTime ReadDate(IDataReader reader, string column)
    {
        var val = reader[column];
        if (val is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
        return Convert.ToDateTime(val);
    }

    private static DateTime? ReadNullableDate(IDataReader reader, string column)
    {
        var val = reader[column];
        if (val == DBNull.Value || val == null) return null;
        if (val is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);
        return Convert.ToDateTime(val);
    }

    public CommonSpace Create(CommonSpace commonSpace)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO common_spaces (building_id, name, description, floor_number, rental_type)
            VALUES (@building_id, @name, @description, @floor_number, @rental_type)
            RETURNING id;
        ";

        var buildingIdParam = command.CreateParameter();
        buildingIdParam.ParameterName = "@building_id";
        buildingIdParam.Value = commonSpace.BuildingId;
        command.Parameters.Add(buildingIdParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = commonSpace.Name;
        command.Parameters.Add(nameParam);

        var descriptionParam = command.CreateParameter();
        descriptionParam.ParameterName = "@description";
        descriptionParam.Value = commonSpace.Description ?? (object)DBNull.Value;
        command.Parameters.Add(descriptionParam);

        var floorNumberParam = command.CreateParameter();
        floorNumberParam.ParameterName = "@floor_number";
        floorNumberParam.Value = commonSpace.FloorNumber;
        command.Parameters.Add(floorNumberParam);

        var rentalTypeParam = command.CreateParameter();
        rentalTypeParam.ParameterName = "@rental_type";
        rentalTypeParam.Value = commonSpace.RentalType.ToString();
        command.Parameters.Add(rentalTypeParam);

        var id = Convert.ToInt32(command.ExecuteScalar());

        return new CommonSpace(id, commonSpace.BuildingId, commonSpace.Name, commonSpace.Description ?? string.Empty, commonSpace.FloorNumber, commonSpace.RentalType);
    }

    public CommonSpace Create(int buildingId, string name, string description, int floorNumber, CommonSpaceRentalType rentalType)
    {
        var commonSpace = new CommonSpace(
            id: 0,
            buildingId: buildingId,
            name: name,
            description: description ?? string.Empty,
            floorNumber: floorNumber,
            rentalType: rentalType
        );

        return Create(commonSpace);
    }

    public CommonSpace? GetById(int id)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM common_spaces WHERE id = @id;";

        var pId = command.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = id;
        command.Parameters.Add(pId);

        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        string name = reader["name"] == DBNull.Value ? string.Empty : (reader["name"]?.ToString() ?? string.Empty);
        string? description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString();
        string rentalTypeStr = reader["rental_type"] == DBNull.Value ? string.Empty : (reader["rental_type"]?.ToString() ?? string.Empty);

        return new CommonSpace(
            Convert.ToInt32(reader["id"]),
            Convert.ToInt32(reader["building_id"]),
            name,
            description ?? string.Empty,
            Convert.ToInt32(reader["floor_number"]),
            Enum.Parse<CommonSpaceRentalType>(rentalTypeStr)
        );
    }

    public List<CommonSpace> GetByBuildingId(int buildingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM common_spaces WHERE building_id = @building_id;";

        var pId = command.CreateParameter();
        pId.ParameterName = "@building_id";
        pId.Value = buildingId;
        command.Parameters.Add(pId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<CommonSpace>();

        while (reader.Read())
        {
            string name = reader["name"] == DBNull.Value ? string.Empty : (reader["name"]?.ToString() ?? string.Empty);
            string? description = reader["description"] == DBNull.Value ? null : reader["description"]?.ToString();
            string rentalTypeStr = reader["rental_type"] == DBNull.Value ? string.Empty : (reader["rental_type"]?.ToString() ?? string.Empty);

            result.Add(new CommonSpace(
                Convert.ToInt32(reader["id"]),
                Convert.ToInt32(reader["building_id"]),
                name,
                description ?? string.Empty,
                Convert.ToInt32(reader["floor_number"]),
                Enum.Parse<CommonSpaceRentalType>(rentalTypeStr)
            ));
        }

        return result;
    }

    public List<CommonSpaceRentalRequest> GetApprovedRequestsByCommonSpaceId(int commonSpaceId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"
            SELECT * FROM common_space_rental_requests
            WHERE common_space_id = @common_space_id
            AND status = 'Approved';";

        var pId = command.CreateParameter();
        pId.ParameterName = "@common_space_id";
        pId.Value = commonSpaceId;
        command.Parameters.Add(pId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<CommonSpaceRentalRequest>();

        while (reader.Read())
        {
            result.Add(new CommonSpaceRentalRequest(
                Convert.ToInt32(reader["id"]),
                Convert.ToInt32(reader["common_space_id"]),
                Convert.ToInt32(reader["tenant_id"]),
                ReadDate(reader, "requested_start_date"),
                ReadDate(reader, "requested_end_date"),
                Enum.Parse<RentalRequestStatus>(reader["status"]?.ToString() ?? string.Empty),
                ReadNullableDate(reader, "approved_date")
            ));
        }

        return result;
    }

    public List<CommonSpace> GetCommonSpacesForBuilding(int buildingId)
    {
        return GetByBuildingId(buildingId);
    }

    public bool Delete(int commonSpaceId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = "DELETE FROM common_spaces WHERE id = @id;";

        var pId = command.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = commonSpaceId;
        command.Parameters.Add(pId);

        int result = command.ExecuteNonQuery();
        return result > 0;
    }
}
