using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ICityGiftRepository
{
    List<CityGift> GetAvailable();
    void Apply(long cityGiftId, long neighborhoodId);
    bool HasApplied(long cityGiftId, long coordinatorId);
    void Assign(long cityGiftId);
    long? GetNeighborhoodWithLowestBudget(long cityGiftId);
}