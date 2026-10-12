using System.Collections.Generic;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services
{
    public interface ICommonSpaceService
    {
        List<CommonSpace> GetCommonSpacesForBuilding(int buildingId);
        CommonSpace CreateCommonSpace(int buildingId, string name, string description, int floorNumber, CommonSpaceRentalType rentalType);
    }
}
