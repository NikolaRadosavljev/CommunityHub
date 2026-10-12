using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;

namespace CommunityHub.Application.Database.Repositories;

public class BuildingRequestDbRepository
{
    private readonly BuildingMembershipDbRepository _membershipRepository;

    public BuildingRequestDbRepository()
    {
        _membershipRepository = new BuildingMembershipDbRepository();
    }

    public List<BuildingRequestDTO> GetForManager(long managerId, RequestStatus? statusFilter, bool ascending)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT br.id,
                   u.name,
                   u.surname,
                   b.address,
                   b.settlement,
                   b.city,
                   b.country,
                   br.flat_number,
                   br.created_at,
                   br.status,
                   br.rejection_explanation,
                   br.building_id
            FROM building_requests br
            INNER JOIN users u ON br.user_id = u.id
            INNER JOIN buildings b ON br.building_id = b.id
            WHERE b.manager_id = @managerId
            {(statusFilter.HasValue ? "AND br.status = @status" : string.Empty)}
            ORDER BY br.created_at {(ascending ? "ASC" : "DESC")}";

        AddParameter(command, "@managerId", managerId);
        if (statusFilter.HasValue)
        {
            AddParameter(command, "@status", statusFilter.Value.ToString());
        }

        using IDataReader reader = command.ExecuteReader();
        List<BuildingRequestDTO> requests = [];

        while (reader.Read())
        {
            requests.Add(new BuildingRequestDTO
            {
                Id = reader.GetInt64(0),
                TenantName = $"{reader.GetString(1)} {reader.GetString(2)}",
                Address = reader.GetString(3),
                Settlement = reader.GetString(4),
                City = reader.GetString(5),
                Country = reader.GetString(6),
                FlatNumber = reader.GetInt32(7),
                CreatedAt = reader.GetDateTime(8),
                Status = Enum.Parse<RequestStatus>(reader.GetString(9), true),
                RejectionExplanation = reader.IsDBNull(10) ? null : reader.GetString(10),
                BuildingId = reader.GetInt32(11)
            });
        }

        return requests;
    }

    public List<BuildingRequestDTO> GetForTenant(long userId, RequestStatus? statusFilter, bool ascending)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT br.id,
                   u.name,
                   u.surname,
                   b.address,
                   b.settlement,
                   b.city,
                   b.country,
                   br.flat_number,
                   br.created_at,
                   br.status,
                   br.rejection_explanation,
                   br.building_id
            FROM building_requests br
            INNER JOIN users u ON br.user_id = u.id
            INNER JOIN buildings b ON br.building_id = b.id
            WHERE br.user_id = @userId
            {(statusFilter.HasValue ? "AND br.status = @status" : string.Empty)}
            ORDER BY br.created_at {(ascending ? "ASC" : "DESC")}";

        AddParameter(command, "@userId", userId);
        if (statusFilter.HasValue)
        {
            AddParameter(command, "@status", statusFilter.Value.ToString());
        }

        using IDataReader reader = command.ExecuteReader();
        List<BuildingRequestDTO> requests = [];

        while (reader.Read())
        {
            requests.Add(new BuildingRequestDTO
            {
                Id = reader.GetInt64(0),
                TenantName = $"{reader.GetString(1)} {reader.GetString(2)}",
                Address = reader.GetString(3),
                Settlement = reader.GetString(4),
                City = reader.GetString(5),
                Country = reader.GetString(6),
                FlatNumber = reader.GetInt32(7),
                CreatedAt = reader.GetDateTime(8),
                Status = Enum.Parse<RequestStatus>(reader.GetString(9), true),
                RejectionExplanation = reader.IsDBNull(10) ? null : reader.GetString(10),
                BuildingId = reader.GetInt32(11)
            });
        }

        return requests;
    }

    public void CreateRequest(long userId, long buildingId, int flatNumber)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO building_requests (user_id, building_id, flat_number, created_at, status)
            VALUES (@userId, @buildingId, @flatNumber, @createdAt, @status)";

        AddParameter(command, "@userId", userId);
        AddParameter(command, "@buildingId", buildingId);
        AddParameter(command, "@flatNumber", flatNumber);
        AddParameter(command, "@createdAt", DateTime.UtcNow);
        AddParameter(command, "@status", RequestStatus.Pending.ToString());

        command.ExecuteNonQuery();
    }

    public void CancelRequest(long requestId, long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM building_requests
            WHERE id = @requestId AND user_id = @userId AND status = 'Pending'";

        AddParameter(command, "@requestId", requestId);
        AddParameter(command, "@userId", userId);

        command.ExecuteNonQuery();
    }

    public void Approve(long requestId, long managerId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        try
        {
            BuildingRequestDetails request = GetRequestDetails(connection, transaction, requestId, managerId);
            int floorNumber = GetFloorNumber(connection, transaction, request.BuildingId, request.FlatNumber);
            User user = new User(request.UserId, string.Empty, string.Empty, request.UserName, request.UserSurname, request.UserBirthday, request.UserRole);

            UpdateRequestStatus(connection, transaction, requestId, RequestStatus.Accepted.ToString(), null);
            _membershipRepository.Create(connection, transaction, new BuildingMembership(0, (int)request.BuildingId, user, floorNumber, request.FlatNumber, DateTime.UtcNow));

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Reject(long requestId, long managerId, string? rejectionExplanation)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        try
        {
            _ = GetRequestDetails(connection, transaction, requestId, managerId);
            UpdateRequestStatus(connection, transaction, requestId, RequestStatus.Denied.ToString(), string.IsNullOrWhiteSpace(rejectionExplanation) ? null : rejectionExplanation.Trim());
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static BuildingRequestDetails GetRequestDetails(IDbConnection connection, IDbTransaction transaction, long requestId, long managerId)
    {
        IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            SELECT br.id,
                   br.building_id,
                   br.user_id,
                   u.name,
                   u.surname,
                   u.birthday,
                     u.role,
                   br.flat_number
            FROM building_requests br
            INNER JOIN buildings b ON br.building_id = b.id
            INNER JOIN users u ON br.user_id = u.id
            WHERE br.id = @requestId
              AND b.manager_id = @managerId
              AND br.status = 'Pending'";

        AddParameter(command, "@requestId", requestId);
        AddParameter(command, "@managerId", managerId);

        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("Request not found or does not belong to the manager.");
        }

        return new BuildingRequestDetails
        {
            RequestId = reader.GetInt64(0),
            BuildingId = reader.GetInt64(1),
            UserId = reader.GetInt64(2),
            UserName = reader.GetString(3),
            UserSurname = reader.GetString(4),
            UserBirthday = reader.GetDateTime(5),
            UserRole = Enum.Parse<UserRole>(reader.GetString(6), true),
            FlatNumber = reader.GetInt32(7)
        };
    }

    private static int GetFloorNumber(IDbConnection connection, IDbTransaction transaction, long buildingId, int flatNumber)
    {
        IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            SELECT floor_number
            FROM flats
            WHERE building_id = @buildingId AND flat_number = @flatNumber";

        AddParameter(command, "@buildingId", buildingId);
        AddParameter(command, "@flatNumber", flatNumber);

        object? result = command.ExecuteScalar();
        if (result == null)
        {
            throw new InvalidOperationException("Flat not found for the approved request.");
        }

        return Convert.ToInt32(result);
    }

    private static void UpdateRequestStatus(IDbConnection connection, IDbTransaction transaction, long requestId, string status, string? rejectionExplanation)
    {
        IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            UPDATE building_requests
            SET status = @status,
                rejection_explanation = @rejectionExplanation
            WHERE id = @requestId";

        AddParameter(command, "@requestId", requestId);
        AddParameter(command, "@status", status);
        AddParameter(command, "@rejectionExplanation", (object?)rejectionExplanation ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed class BuildingRequestDetails
    {
        public long RequestId { get; set; }
        public long BuildingId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserSurname { get; set; } = string.Empty;
        public DateTime UserBirthday { get; set; }
        public UserRole UserRole { get; set; }
        public int FlatNumber { get; set; }
    }
}
