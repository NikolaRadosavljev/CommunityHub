using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class NeighborhoodMembershipRequestDbRepository : INeighborhoodMembershipRequestRepository
{
    private static DateTime ReadDateTime(object value)
    {
        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            _ => throw new InvalidCastException($"Unsupported date type: {value.GetType()}")
        };
    }

    public bool HasPendingRequest(long userId, long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM neighborhood_membership_requests
            WHERE citizen_id = @userId AND neighborhood_id = @neighborhoodId
            AND status = 'Pending'";

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        IDbDataParameter neighborhoodIdParam = command.CreateParameter();
        neighborhoodIdParam.ParameterName = "@neighborhoodId";
        neighborhoodIdParam.Value = neighborhoodId;
        command.Parameters.Add(neighborhoodIdParam);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    public void Create(long neighborhoodId, long citizenId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO neighborhood_membership_requests 
            (neighborhood_id, citizen_id, created_at, status, rejection_reason)
            VALUES (@neighborhoodId, @citizenId, @createdAt, 'Pending', NULL)";

        IDbDataParameter neighborhoodIdParam = command.CreateParameter();
        neighborhoodIdParam.ParameterName = "@neighborhoodId";
        neighborhoodIdParam.Value = neighborhoodId;
        command.Parameters.Add(neighborhoodIdParam);

        IDbDataParameter citizenIdParam = command.CreateParameter();
        citizenIdParam.ParameterName = "@citizenId";
        citizenIdParam.Value = citizenId;
        command.Parameters.Add(citizenIdParam);

        IDbDataParameter createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = "@createdAt";
        createdAtParam.Value = DateTime.UtcNow;
        command.Parameters.Add(createdAtParam);

        command.ExecuteNonQuery();
    }

    public List<NeighborhoodMembershipRequest> GetByUserId(long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = command.CommandText = @"
            SELECT r.id, r.created_at, r.status, r.rejection_reason,
            n.id, n.name
            FROM neighborhood_membership_requests r
            JOIN neighborhoods n ON n.id = r.neighborhood_id
            WHERE r.citizen_id = @userId";

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        using IDataReader reader = command.ExecuteReader();
        var requests = new List<NeighborhoodMembershipRequest>();

        while (reader.Read())
        {
            var request = new NeighborhoodMembershipRequest(reader.GetInt64(0));
            request.CreatedAt = reader.GetDateTime(1);
            request.Status = ParseStatus(reader.GetString(2));
            request.RejectionReason = reader.IsDBNull(3) ? null : reader.GetString(3);
            request.Neighborhood = new Neighborhood(reader.GetInt64(4), 0, reader.GetString(5), "");
            requests.Add(request);
        }

        return requests;
    }

    public void Delete(long requestId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM neighborhood_membership_requests
            WHERE id = @requestId AND status = 'Pending'";

        IDbDataParameter requestIdParam = command.CreateParameter();
        requestIdParam.ParameterName = "@requestId";
        requestIdParam.Value = requestId;
        command.Parameters.Add(requestIdParam);

        command.ExecuteNonQuery();
    }

    private static NeighborhoodMembershipRequestStatus ParseStatus(string status)
    {
        return Enum.Parse<NeighborhoodMembershipRequestStatus>(status);
    }

    public List<NeighborhoodMembershipRequest> GetByCoordinator(long coordinatorId)
    {
        List<NeighborhoodMembershipRequest> requests = new List<NeighborhoodMembershipRequest>();

        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                r.id AS request_id,
                r.neighborhood_id,
                r.citizen_id,
                r.created_at,
                r.status,
                r.rejection_reason,

                n.name AS neighborhood_name,
                n.description AS neighborhood_description,
                n.coordinator_id,

                u.id AS user_id,
                u.username,
                u.password,
                u.name AS citizen_name,
                u.surname,
                u.birthday,
                u.role,

                c.address
    
            FROM neighborhood_membership_requests r
            INNER JOIN neighborhoods n ON r.neighborhood_id = n.id
            INNER JOIN users u ON r.citizen_id = u.id
            INNER JOIN citizens c ON c.user_id = u.id
            WHERE n.coordinator_id = @coordinatorId
            ORDER BY r.created_at DESC";

        IDbDataParameter coordinatorIdParam = command.CreateParameter();
        coordinatorIdParam.ParameterName = "@coordinatorId";
        coordinatorIdParam.Value = coordinatorId;
        command.Parameters.Add(coordinatorIdParam);

        using IDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            long requestId = Convert.ToInt64(reader["request_id"]);
            long neighborhoodId = Convert.ToInt64(reader["neighborhood_id"]);
            long citizenId = Convert.ToInt64(reader["citizen_id"]);
            DateTime createdAt = ReadDateTime(reader["created_at"]);
            string statusString = reader["status"].ToString()!;
            string? rejectionReason = reader["rejection_reason"] == DBNull.Value
                ? null
                : reader["rejection_reason"].ToString();

            NeighborhoodMembershipRequestStatus status =
                Enum.Parse<NeighborhoodMembershipRequestStatus>(statusString);

            NeighborhoodMembershipRequest request = new NeighborhoodMembershipRequest(
                requestId,
                neighborhoodId,
                citizenId,
                createdAt,
                status,
                rejectionReason
            );

            User user = new User(
                Convert.ToInt64(reader["user_id"]),
                reader["username"].ToString()!,
                reader["password"].ToString()!,
                reader["citizen_name"].ToString()!,
                reader["surname"].ToString()!,
                ReadDateTime(reader["birthday"]),
                Enum.Parse<UserRole>(reader["role"].ToString()!)
            );

            Citizen citizen = new Citizen(
                user,
                reader["address"] == DBNull.Value ? "" : reader["address"].ToString()!
            );

            Neighborhood neighborhood = new Neighborhood(
                neighborhoodId,
                Convert.ToInt64(reader["coordinator_id"]),
                reader["neighborhood_name"].ToString()!,
                reader["neighborhood_description"].ToString()!
            );

            request.Citizen = citizen;
            request.Neighborhood = neighborhood;

            requests.Add(request);
        }

        return requests;
    }

    public NeighborhoodMembershipRequest? GetById(long requestId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id,
                   neighborhood_id,
                   citizen_id,
                   created_at,
                   status,
                   rejection_reason
            FROM neighborhood_membership_requests
            WHERE id = @id";

        IDbDataParameter idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = requestId;
        command.Parameters.Add(idParam);

        using IDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            long id = Convert.ToInt64(reader["id"]);
            long neighborhoodId = Convert.ToInt64(reader["neighborhood_id"]);
            long citizenId = Convert.ToInt64(reader["citizen_id"]);
            DateTime createdAt = ReadDateTime(reader["created_at"]);
            string statusString = reader["status"].ToString()!;
            string? rejectionReason = reader["rejection_reason"] == DBNull.Value
                ? null
                : reader["rejection_reason"].ToString();

            NeighborhoodMembershipRequestStatus status =
                Enum.Parse<NeighborhoodMembershipRequestStatus>(statusString);

            return new NeighborhoodMembershipRequest(
                id,
                neighborhoodId,
                citizenId,
                createdAt,
                status,
                rejectionReason
            );
        }

        return null;
    }

    public void Approve(long requestId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE neighborhood_membership_requests
            SET status = @status,
                rejection_reason = NULL
            WHERE id = @id";

        IDbDataParameter statusParam = command.CreateParameter();
        statusParam.ParameterName = "@status";
        statusParam.Value = NeighborhoodMembershipRequestStatus.Approved.ToString();
        command.Parameters.Add(statusParam);

        IDbDataParameter idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = requestId;
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();
    }

    public void Reject(long requestId, string? rejectionReason)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE neighborhood_membership_requests
            SET status = @status,
                rejection_reason = @rejectionReason
            WHERE id = @id";

        IDbDataParameter statusParam = command.CreateParameter();
        statusParam.ParameterName = "@status";
        statusParam.Value = NeighborhoodMembershipRequestStatus.Rejected.ToString();
        command.Parameters.Add(statusParam);

        IDbDataParameter rejectionReasonParam = command.CreateParameter();
        rejectionReasonParam.ParameterName = "@rejectionReason";
        rejectionReasonParam.Value = rejectionReason ?? (object)DBNull.Value;
        command.Parameters.Add(rejectionReasonParam);

        IDbDataParameter idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = requestId;
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();
    }
}