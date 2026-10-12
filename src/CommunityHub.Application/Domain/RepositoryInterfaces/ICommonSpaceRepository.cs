using System.Collections.Generic;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface ICommonSpaceRepository
    {
        List<CommonSpace> GetCommonSpacesForBuilding(int buildingId);
        CommonSpace Create(int buildingId, string name, string description, int floorNumber, CommonSpaceRentalType rentalType);
        bool Delete(int commonSpaceId);
    }
}
