using System.Collections.Generic;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface IBuildingRepository
    {
        Building Create(Building building, List<Flat> flats);
        List<Building> GetByManagerId(long managerId);
        Building? GetById(int id);
        int GetOccupancyCount(int buildingId);
        List<Building> Search(string? address, string? settlement, string? city, string? country);
    }
}
