using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class NeighborhoodMembershipDbRepository : INeighborhoodMembershipRepository
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

    public bool HasMembership(long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM neighborhood_memberships
            WHERE citizen_id = @userId";

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    public void Create(long neighborhoodId, long citizenId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO neighborhood_memberships (neighborhood_id, citizen_id, joined_at)
            VALUES (@neighborhoodId, @citizenId, @joinedAt)";

        IDbDataParameter neighborhoodIdParam = command.CreateParameter();
        neighborhoodIdParam.ParameterName = "@neighborhoodId";
        neighborhoodIdParam.Value = neighborhoodId;
        command.Parameters.Add(neighborhoodIdParam);

        IDbDataParameter citizenIdParam = command.CreateParameter();
        citizenIdParam.ParameterName = "@citizenId";
        citizenIdParam.Value = citizenId;
        command.Parameters.Add(citizenIdParam);

        IDbDataParameter joinedAtParam = command.CreateParameter();
        joinedAtParam.ParameterName = "@joinedAt";
        joinedAtParam.Value = DateTime.UtcNow;
        command.Parameters.Add(joinedAtParam);

        command.ExecuteNonQuery();
    }

    public long? GetNeighborhoodIdByUserId(long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
        SELECT neighborhood_id FROM neighborhood_memberships
        WHERE citizen_id = @userId";

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        object? result = command.ExecuteScalar();
        return result == null ? null : Convert.ToInt64(result);
    }

    public List<NeighborhoodMembership> GetByCoordinator(long coordinatorId)
    {
        List<NeighborhoodMembership> memberships = new List<NeighborhoodMembership>();

        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                m.id AS membership_id,
                m.neighborhood_id,
                m.citizen_id,
                m.joined_at,

                u.id AS user_id,
                u.username,
                u.password,
                u.name AS citizen_name,
                u.surname,
                u.birthday,
                u.role,
                
                c.address

            FROM neighborhood_memberships m
            INNER JOIN neighborhoods n ON m.neighborhood_id = n.id
            INNER JOIN users u ON m.citizen_id = u.id
            INNER JOIN citizens c ON c.user_id = u.id
            WHERE n.coordinator_id = @coordinatorId
            ORDER BY m.joined_at DESC";

        IDbDataParameter coordinatorIdParam = command.CreateParameter();
        coordinatorIdParam.ParameterName = "@coordinatorId";
        coordinatorIdParam.Value = coordinatorId;
        command.Parameters.Add(coordinatorIdParam);

        using IDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            long membershipId = Convert.ToInt64(reader["membership_id"]);
            long neighborhoodId = Convert.ToInt64(reader["neighborhood_id"]);
            long citizenId = Convert.ToInt64(reader["citizen_id"]);
            DateTime joinedAt = ReadDateTime(reader["joined_at"]);

            NeighborhoodMembership membership = new NeighborhoodMembership(
                membershipId,
                neighborhoodId,
                citizenId,
                joinedAt
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

            membership.Citizen = citizen;

            memberships.Add(membership);
        }

        return memberships;
    }
}