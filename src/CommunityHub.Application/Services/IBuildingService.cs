using System.Collections.Generic;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.DTO;

namespace CommunityHub.Application.Services
{
    public interface IBuildingService
    {
        List<Building> Search(string? address, string? settlement, string? city, string? country);
        BuildingRegistrationDTO Register(BuildingRegistrationRequestDTO request);
    }
}
