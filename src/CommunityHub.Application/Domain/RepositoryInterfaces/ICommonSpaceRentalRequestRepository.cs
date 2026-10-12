using System;
using System.Collections.Generic;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface ICommonSpaceRentalRequestRepository
    {
        List<CommonSpaceRentalRequest> GetAllRequestsForManagerBuildings(List<int> buildingIds);
        List<CommonSpaceRentalRequest> GetPendingRequestsForManagerBuildings(List<int> buildingIds);
        CommonSpaceRentalRequest? GetById(int id);
        CommonSpaceRentalRequest Insert(CommonSpaceRentalRequest request);
        void UpdateStatus(CommonSpaceRentalRequest request);
        List<CommonSpaceRentalRequest> GetAllRequests();
    }
}
