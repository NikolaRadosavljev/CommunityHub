using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public class BuildingRequestService
{
    private readonly BuildingRequestDbRepository _requestRepository;

    public BuildingRequestService()
    {
        _requestRepository = new BuildingRequestDbRepository();
    }

    public List<BuildingRequestDTO> GetManagerRequests(long managerId, RequestStatus? statusFilter, bool ascending)
    {
        return _requestRepository.GetForManager(managerId, statusFilter, ascending);
    }

    public List<BuildingRequestDTO> GetTenantRequests(long userId, RequestStatus? statusFilter, bool ascending)
    {
        return _requestRepository.GetForTenant(userId, statusFilter, ascending);
    }

    public void CreateRequest(long userId, long buildingId, int flatNumber)
    {
        _requestRepository.CreateRequest(userId, buildingId, flatNumber);
    }

    public void CancelRequest(long requestId, long userId)
    {
        _requestRepository.CancelRequest(requestId, userId);
    }

    public bool IsFlatOccupied(long buildingId, int flatNumber)
    {
        BuildingMembershipDbRepository membershipRepo = new BuildingMembershipDbRepository();
        return membershipRepo.IsFlatOccupied(buildingId, flatNumber);
    }

    public void ApproveRequest(long managerId, long requestId)
    {
        _requestRepository.Approve(requestId, managerId);
    }

    public void RejectRequest(long managerId, long requestId, string? rejectionExplanation)
    {
        _requestRepository.Reject(requestId, managerId, rejectionExplanation);
    }
}
