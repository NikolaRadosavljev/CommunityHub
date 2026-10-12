using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class CitizenTrustService
{
    private readonly ICitizenTrustRecordRepository _trustRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;

    public CitizenTrustService(ICitizenTrustRecordRepository trustRepository,
                                INeighborhoodRepository neighborhoodRepository)
    {
        _trustRepository = trustRepository;
        _neighborhoodRepository = neighborhoodRepository;
    }

    public Dictionary<CitizenTrustLevel, int> GetLevelDistribution(long neighborhoodId)
    {
        List<CitizenTrustRecord> records = _trustRepository.GetByNeighborhoodId(neighborhoodId);

        var distribution = new Dictionary<CitizenTrustLevel, int>
        {
            { CitizenTrustLevel.New, 0 },
            { CitizenTrustLevel.Inactive, 0 },
            { CitizenTrustLevel.Active, 0 },
            { CitizenTrustLevel.Distinguished, 0 },
            { CitizenTrustLevel.Trusted, 0 }
        };

        foreach (CitizenTrustRecord record in records)
        {
            CitizenTrustLevel level = record.GetLevel();
            distribution[level]++;
        }

        return distribution;
    }

    public CitizenTrustLevel GetDominantLevel(long neighborhoodId)
    {
        Dictionary<CitizenTrustLevel, int> distribution = GetLevelDistribution(neighborhoodId);
        return distribution.OrderByDescending(x => x.Value).First().Key;
    }

    public List<CitizenTrustRecord> GetAllRecords(long neighborhoodId)
    {
        return _trustRepository.GetByNeighborhoodId(neighborhoodId);
    }

    public List<long> GetNeighborhoodIdsByCoordinator(long coordinatorId)
    {
        List<Neighborhood> neighborhoods = _neighborhoodRepository.Search(null, null, null, null);
        return neighborhoods
            .Where(n => n.CoordinatorId == coordinatorId)
            .Select(n => n.Id)
            .ToList();
    }
}