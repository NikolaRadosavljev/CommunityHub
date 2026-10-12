using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ICitizenTrustRecordRepository
{
    List<CitizenTrustRecord> GetByNeighborhoodId(long neighborhoodId);
}