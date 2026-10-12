using System.Collections.Generic;
using CommunityHub.Application.Domain;
using System.Data;
using CommunityHub.Application.Services;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface IBuildingMembershipRepository
    {
        BuildingMembership Create(BuildingMembership membership);
        BuildingMembership Create(IDbConnection connection, IDbTransaction transaction, BuildingMembership membership);
        bool IsFlatOccupied(long buildingId, int flatNumber);
        List<BuildingMembershipDTO> GetMembershipsForManager(long managerId);
        List<BuildingMembershipDTO> GetMembershipsForBuilding(int buildingId);
    }
}
