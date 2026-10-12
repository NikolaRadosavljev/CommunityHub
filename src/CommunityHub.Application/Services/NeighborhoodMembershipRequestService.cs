using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class NeighborhoodMembershipRequestService
{
    private readonly INeighborhoodMembershipRequestRepository _requestRepository;
    private readonly INeighborhoodMembershipRepository _membershipRepository;

    public NeighborhoodMembershipRequestService(
        INeighborhoodMembershipRequestRepository requestRepository,
        INeighborhoodMembershipRepository membershipRepository)
    {
        _requestRepository = requestRepository;
        _membershipRepository = membershipRepository;
    }

    public List<NeighborhoodMembershipRequest> GetRequestsByCoordinator(long coordinatorId)
    {
        return _requestRepository.GetByCoordinator(coordinatorId);
    }

    public List<NeighborhoodMembership> GetMembershipsByCoordinator(long coordinatorId)
    {
        return _membershipRepository.GetByCoordinator(coordinatorId);
    }

    public List<NeighborhoodMembershipRequest> GetRequestsByUser(long userId)
    {
        return _requestRepository.GetByUserId(userId);
    }

    public void ApproveRequest(long requestId)
    {
        NeighborhoodMembershipRequest? request = _requestRepository.GetById(requestId);

        if (request == null)
            throw new ArgumentException("Request not found.");

        if (request.Status != NeighborhoodMembershipRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved.");

        _requestRepository.Approve(requestId);
        _membershipRepository.Create(request.NeighborhoodId, request.CitizenId);
    }

    public void RejectRequest(long requestId, string? rejectionReason)
    {
        NeighborhoodMembershipRequest? request = _requestRepository.GetById(requestId);

        if (request == null)
            throw new ArgumentException("Request not found.");

        if (request.Status != NeighborhoodMembershipRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        _requestRepository.Reject(requestId, rejectionReason);
    }

    public void DeleteRequest(long requestId)
    {
        NeighborhoodMembershipRequest? request = _requestRepository.GetById(requestId);

        if (request == null)
            throw new ArgumentException("Request not found.");

        if (request.Status != NeighborhoodMembershipRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be deleted.");

        _requestRepository.Delete(requestId);
    }
}