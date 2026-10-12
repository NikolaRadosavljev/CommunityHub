using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface INeighborhoodMembershipRequestRepository
{
    bool HasPendingRequest(long userId, long neighborhoodId);
    void Create(long neighborhoodId, long citizenId);
    List<NeighborhoodMembershipRequest> GetByUserId(long userId);
    List<NeighborhoodMembershipRequest> GetByCoordinator(long coordinatorId);
    NeighborhoodMembershipRequest? GetById(long requestId);
    void Approve(long requestId);
    void Reject(long requestId, string? rejectionReason);
    void Delete(long requestId);
}