using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class TrustRecordService
{
    private readonly ITrustRecordRepository _trustRecordRepository;
    private readonly INeighborhoodMembershipRepository _membershipRepository;

    public TrustRecordService(ITrustRecordRepository trustRecordRepository, INeighborhoodMembershipRepository membershipRepository)
    {
        _trustRecordRepository = trustRecordRepository;
        _membershipRepository = membershipRepository;
    }

    public TrustRecord? GetForCurrentUser(long userId)
    {
        var neighborhoodId = _membershipRepository.GetNeighborhoodIdByUserId(userId);
        if (neighborhoodId == null) return null;

        return _trustRecordRepository.GetByCitizenId(userId, neighborhoodId.Value);
    }

    public List<(Citizen citizen, TrustRecord record)> GetForNeighborhood(long neighborhoodId)
    {
        return _trustRecordRepository.GetByNeighborhoodId(neighborhoodId);
    }
}