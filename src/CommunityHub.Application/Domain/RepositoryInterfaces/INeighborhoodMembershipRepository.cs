using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface INeighborhoodMembershipRepository
{
    bool HasMembership(long userId);
    void Create(long neighborhoodId, long citizenId);

    long? GetNeighborhoodIdByUserId(long userId);
    List<NeighborhoodMembership> GetByCoordinator(long coordinatorId);
}