using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CommonSpaceRentalRequestDbRepository : ICommonSpaceRentalRequestRepository
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

    public List<CommonSpaceRentalRequest> GetPendingRequestsForManagerBuildings(List<int> managedBuildingIds)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        var idsStr = string.Join(",", managedBuildingIds);
        if (string.IsNullOrEmpty(idsStr)) return new List<CommonSpaceRentalRequest>();

        command.CommandText = $@"SELECT r.*, cs.building_id FROM common_space_rental_requests r JOIN common_spaces cs ON r.common_space_id = cs.id WHERE cs.building_id IN ({idsStr}) AND r.status IN ('Pending', 'PendingChangeOfDate')";

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

    public List<CommonSpaceRentalRequest> GetAllRequestsForManagerBuildings(List<int> managedBuildingIds)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        var idsStr = string.Join(",", managedBuildingIds);
        if (string.IsNullOrEmpty(idsStr)) return new List<CommonSpaceRentalRequest>();

        command.CommandText = $@"SELECT r.*, cs.building_id FROM common_space_rental_requests r JOIN common_spaces cs ON r.common_space_id = cs.id WHERE cs.building_id IN ({idsStr}) ORDER BY r.id DESC";

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

    public List<CommonSpaceRentalRequest> GetAllRequests()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"SELECT r.* FROM common_space_rental_requests r ORDER BY r.id DESC";

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

    public CommonSpaceRentalRequest? GetById(int id)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = "SELECT * FROM common_space_rental_requests WHERE id = @id";

        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = id;
        command.Parameters.Add(idParam);

        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new CommonSpaceRentalRequest(
            Convert.ToInt32(reader["id"]),
            Convert.ToInt32(reader["common_space_id"]),
            Convert.ToInt32(reader["tenant_id"]),
            ReadDate(reader, "requested_start_date"),
            ReadDate(reader, "requested_end_date"),
            Enum.Parse<RentalRequestStatus>(reader["status"]?.ToString() ?? string.Empty),
            ReadNullableDate(reader, "approved_date")
        );
    }

    public CommonSpaceRentalRequest Insert(CommonSpaceRentalRequest request)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO common_space_rental_requests
                (common_space_id, tenant_id, requested_start_date, requested_end_date, status, approved_date)
            VALUES
                (@common_space_id, @tenant_id, @start_date, @end_date, @status, @approved_date)
            RETURNING id";

        var p1 = command.CreateParameter(); p1.ParameterName = "@common_space_id"; p1.Value = request.CommonSpaceId; command.Parameters.Add(p1);
        var p2 = command.CreateParameter(); p2.ParameterName = "@tenant_id"; p2.Value = request.TenantId; command.Parameters.Add(p2);
        var p3 = command.CreateParameter(); p3.ParameterName = "@start_date"; p3.Value = request.RequestedStartDate; command.Parameters.Add(p3);
        var p4 = command.CreateParameter(); p4.ParameterName = "@end_date"; p4.Value = request.RequestedEndDate; command.Parameters.Add(p4);
        var p5 = command.CreateParameter(); p5.ParameterName = "@status"; p5.Value = request.Status.ToString(); command.Parameters.Add(p5);
        var p6 = command.CreateParameter(); p6.ParameterName = "@approved_date"; p6.Value = request.ApprovedDate.HasValue ? (object)request.ApprovedDate.Value : DBNull.Value; command.Parameters.Add(p6);

        var idObj = command.ExecuteScalar();
        int id = idObj != null ? Convert.ToInt32(idObj) : 0;
        return new CommonSpaceRentalRequest(id, request.CommonSpaceId, request.TenantId, request.RequestedStartDate, request.RequestedEndDate, request.Status, request.ApprovedDate);
    }

    public void UpdateStatus(CommonSpaceRentalRequest request)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"UPDATE common_space_rental_requests SET status = @status, approved_date = @approved_date, requested_start_date = @start_date, requested_end_date = @end_date WHERE id = @id";

        var idParam = command.CreateParameter(); idParam.ParameterName = "@id"; idParam.Value = request.Id; command.Parameters.Add(idParam);
        var statusParam = command.CreateParameter(); statusParam.ParameterName = "@status"; statusParam.Value = request.Status.ToString(); command.Parameters.Add(statusParam);
        var appDateParam = command.CreateParameter(); appDateParam.ParameterName = "@approved_date"; appDateParam.Value = request.ApprovedDate.HasValue ? request.ApprovedDate.Value : DBNull.Value; command.Parameters.Add(appDateParam);
        var startParam = command.CreateParameter(); startParam.ParameterName = "@start_date"; startParam.Value = request.RequestedStartDate; command.Parameters.Add(startParam);
        var endParam = command.CreateParameter(); endParam.ParameterName = "@end_date"; endParam.Value = request.RequestedEndDate; command.Parameters.Add(endParam);

        command.ExecuteNonQuery();
    }
}
