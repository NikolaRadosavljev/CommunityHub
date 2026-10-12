using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Database.Repositories;

/// <summary>
/// Implementacija repozitorijuma za pristup podacima o članstvu u zgradama iz PostgreSQL baze
/// </summary>
public class BuildingMembershipDbRepository : CommunityHub.Application.Domain.RepositoryInterfaces.IBuildingMembershipRepository
{
    public BuildingMembership Create(BuildingMembership membership)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbTransaction transaction = connection.BeginTransaction();

        try
        {
            BuildingMembership createdMembership = Create(connection, transaction, membership);
            transaction.Commit();
            return createdMembership;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public BuildingMembership Create(IDbConnection connection, IDbTransaction transaction, BuildingMembership membership)
    {
        IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO building_memberships (building_id, user_id, floor_number, flat_number, joined_at)
            VALUES (@buildingId, @userId, @floorNumber, @flatNumber, @joinedAt)
            RETURNING id";

        AddParameter(command, "@buildingId", membership.BuildingId);
        AddParameter(command, "@userId", membership.User.Id);
        AddParameter(command, "@floorNumber", membership.FloorNumber);
        AddParameter(command, "@flatNumber", membership.FlatNumber);
        AddParameter(command, "@joinedAt", membership.Date);

        long id = Convert.ToInt64(command.ExecuteScalar());
        return new BuildingMembership(checked((int)id), (int)membership.BuildingId, membership.User, membership.FloorNumber, membership.FlatNumber, membership.Date);
    }

    public bool IsFlatOccupied(long buildingId, int flatNumber)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*)
            FROM building_memberships
            WHERE building_id = @buildingId AND flat_number = @flatNumber";

        AddParameter(command, "@buildingId", buildingId);
        AddParameter(command, "@flatNumber", flatNumber);

        long count = Convert.ToInt64(command.ExecuteScalar());
        return count > 0;
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public List<BuildingMembershipDTO> GetMembershipsForManager(long managerId)
    {
        var memberships = new List<BuildingMembershipDTO>();
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"
            SELECT bm.id, bm.floor_number, bm.flat_number, bm.joined_at,
                   u.name, u.surname,
                   b.address, b.settlement, b.city, b.country
            FROM building_memberships bm
            JOIN users u ON bm.user_id = u.id
            JOIN buildings b ON bm.building_id = b.id
            WHERE b.manager_id  = @managerId
            ORDER BY bm.joined_at DESC";

        AddParameter(command, "@managerId", managerId);

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            long id = Convert.ToInt64(reader["id"]);
            int floorNumber = Convert.ToInt32(reader["floor_number"]);
            int flatNumber = Convert.ToInt32(reader["flat_number"]);
            DateTime joinedAt = Convert.ToDateTime(reader["joined_at"]);
            string name = Convert.ToString(reader["name"]) ?? string.Empty;
            string surname = Convert.ToString(reader["surname"]) ?? string.Empty;
            string address = Convert.ToString(reader["address"]) ?? string.Empty;
            string settlement = Convert.ToString(reader["settlement"]) ?? string.Empty;
            string city = Convert.ToString(reader["city"]) ?? string.Empty;
            string country = Convert.ToString(reader["country"]) ?? string.Empty;

            memberships.Add(new BuildingMembershipDTO
            {
                Id = id,
                TenantName = $"{name} {surname}",
                BuildingAddress = $"{address}, {settlement}, {city}, {country}",
                FloorNumber = floorNumber,
                FlatNumber = flatNumber,
                JoinedAt = joinedAt
            });
        }

        return memberships;
    }

    public List<BuildingMembershipDTO> GetMembershipsForBuilding(int buildingId)
    {
        var memberships = new List<BuildingMembershipDTO>();
        using IDbConnection connection = PostgresConnection.CreateConnection();
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = @"
            SELECT bm.id, bm.floor_number, bm.flat_number, bm.joined_at,
                   u.name, u.surname,
                   b.address, b.settlement, b.city, b.country
            FROM building_memberships bm
            JOIN users u ON bm.user_id = u.id
            JOIN buildings b ON bm.building_id = b.id
            WHERE bm.building_id = @buildingId
            ORDER BY bm.joined_at DESC";

        AddParameter(command, "@buildingId", buildingId);

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            long id = Convert.ToInt64(reader["id"]);
            int floorNumber = Convert.ToInt32(reader["floor_number"]);
            int flatNumber = Convert.ToInt32(reader["flat_number"]);
            DateTime joinedAt = Convert.ToDateTime(reader["joined_at"]);
            string name = Convert.ToString(reader["name"]) ?? string.Empty;
            string surname = Convert.ToString(reader["surname"]) ?? string.Empty;
            string address = Convert.ToString(reader["address"]) ?? string.Empty;
            string settlement = Convert.ToString(reader["settlement"]) ?? string.Empty;
            string city = Convert.ToString(reader["city"]) ?? string.Empty;
            string country = Convert.ToString(reader["country"]) ?? string.Empty;

            memberships.Add(new BuildingMembershipDTO
            {
                Id = id,
                TenantName = $"{name} {surname}",
                BuildingAddress = $"{address}, {settlement}, {city}, {country}",
                FloorNumber = floorNumber,
                FlatNumber = flatNumber,
                JoinedAt = joinedAt
            });
        }

        return memberships;
    }
}
