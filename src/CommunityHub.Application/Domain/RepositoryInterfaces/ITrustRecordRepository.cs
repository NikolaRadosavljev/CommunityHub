using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ITrustRecordRepository
{
    TrustRecord? GetByCitizenId(long citizenId, long neighborhoodId);
    List<(Citizen citizen, TrustRecord record)> GetByNeighborhoodId(long neighborhoodId);
}