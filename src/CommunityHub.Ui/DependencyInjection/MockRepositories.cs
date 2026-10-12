using System;
using System.Collections.Generic;
using System.Data;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.DependencyInjection;

/// <summary>
/// Mock repozitorijumi za testiranje - vraćaju fake podatke umesto iz baze
/// </summary>

// ==================== MOCK USER REPOSITORY ====================
/// <summary>
/// Mock implementacija IUserRepository - vraća fake korisnike za testiranje
/// </summary>
public class MockUserRepository : CommunityHub.Application.Domain.RepositoryInterfaces.IUserRepository
{
    public User? GetByCredentials(string username, string password)
    {
        // Za testiranje - ako je korisnik "test" i lozinka "test"
        if (username == "test" && password == "test")
        {
            return new User(
                id: 1,
                username: "test",
                password: "test",
                name: "Test",
                surname: "User",
                birthDay: DateTime.Now.AddYears(-30),
                role: UserRole.Tenant
            );
        }
        return null;
    }

    public long? GetIdByCredentials(string username, string password)
    {
        var user = GetByCredentials(username, password);
        return user?.Id;
    }

    public User? GetById(long userId)
    {
        return new User(
            id: userId,
            username: $"user{userId}",
            password: "password",
            name: "Test",
            surname: $"User{userId}",
            birthDay: DateTime.Now.AddYears(-30),
            role: UserRole.Tenant
        );
    }

    public User? GetWithPosts(long userId)
    {
        return GetById(userId);
    }

    public string GetFullNameById(long userId)
    {
        return $"Test User{userId}";
    }
}

// ==================== MOCK BUILDING REPOSITORY ====================
/// <summary>
/// Mock implementacija IBuildingRepository - vraća fake zgrade za testiranje
/// </summary>
public class MockBuildingRepository : CommunityHub.Application.Domain.RepositoryInterfaces.IBuildingRepository
{
    public Building Create(Building building, List<Flat> flats)
    {
        // Building je immutable - vraćam novog sa novim ID-om
        return new Building(
            id: new Random().Next(1, 1000),
            address: building.Address,
            settlement: building.Settlement,
            city: building.City,
            country: building.Country,
            floorNumber: building.FloorNumber,
            flatNumbers: building.FlatNumbers,
            pictures: building.Pictures,
            managerId: building.ManagerId
        );
    }

    public List<Building> GetByManagerId(long managerId)
    {
        return new List<Building>
        {
            new Building(
                id: 1,
                address: "Test Street 123",
                settlement: "Test City",
                city: "Test City",
                country: "Test Country",
                floorNumber: 5,
                flatNumbers: new List<int> { 101, 102, 103, 104, 105 },
                pictures: new List<string>(),
                managerId: (int)managerId
            ),
            new Building(
                id: 2,
                address: "Test Avenue 456",
                settlement: "Test City",
                city: "Test City",
                country: "Test Country",
                floorNumber: 3,
                flatNumbers: new List<int> { 201, 202, 203 },
                pictures: new List<string>(),
                managerId: (int)managerId
            )
        };
    }

    public Building? GetById(int id)
    {
        return new Building(
            id: id,
            address: $"Test Street {id}",
            settlement: "Test City",
            city: "Test City",
            country: "Test Country",
            floorNumber: 4,
            flatNumbers: new List<int> { 101, 102, 103, 104 },
            pictures: new List<string>(),
            managerId: 1
        );
    }

    public int GetOccupancyCount(int buildingId)
    {
        // Vraća fake broj zauzimanja
        return new Random().Next(5, 20);
    }

    public List<Building> Search(string? address, string? settlement, string? city, string? country)
    {
        return GetByManagerId(1);
    }
}

// ==================== MOCK BUILDING MEMBERSHIP REPOSITORY ====================
/// <summary>
/// Mock implementacija IBuildingMembershipRepository
/// </summary>
public class MockBuildingMembershipRepository : CommunityHub.Application.Domain.RepositoryInterfaces.IBuildingMembershipRepository
{
    public BuildingMembership Create(BuildingMembership membership)
    {
        // BuildingMembership je immutable - vraćam novog sa novim ID-om
        return new BuildingMembership(
            id: new Random().Next(1, 1000),
            buildingId: membership.BuildingId,
            user: membership.User,
            floorNumber: membership.FloorNumber,
            flatNumber: membership.FlatNumber,
            date: membership.Date
        );
    }

    public BuildingMembership Create(IDbConnection connection, IDbTransaction transaction, BuildingMembership membership)
    {
        return Create(membership);
    }

    public bool IsFlatOccupied(long buildingId, int flatNumber)
    {
        return flatNumber % 2 == 0;  // Fake logika - parni brojevi su zauzeti
    }

    public List<BuildingMembershipDTO> GetMembershipsForManager(long managerId)
    {
        return new List<BuildingMembershipDTO>
        {
            new()
            {
                Id = 1,
                TenantName = "Test Tenant 1",
                BuildingAddress = "Test Street 123",
                FloorNumber = 1,
                FlatNumber = 101,
                JoinedAt = DateTime.Now.AddMonths(-3)
            },
            new()
            {
                Id = 2,
                TenantName = "Test Tenant 2",
                BuildingAddress = "Test Street 123",
                FloorNumber = 1,
                FlatNumber = 102,
                JoinedAt = DateTime.Now.AddMonths(-6)
            }
        };
    }

    public List<BuildingMembershipDTO> GetMembershipsForBuilding(int buildingId)
    {
        return GetMembershipsForManager(1);
    }
}
