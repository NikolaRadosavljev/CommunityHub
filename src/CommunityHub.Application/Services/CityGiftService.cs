using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class CityGiftService
{
    private readonly ICityGiftRepository _cityGiftRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;

    public CityGiftService(ICityGiftRepository cityGiftRepository,
                           IBudgetCategoryRepository categoryRepository,
                           INeighborhoodRepository neighborhoodRepository)
    {
        _cityGiftRepository = cityGiftRepository;
        _categoryRepository = categoryRepository;
        _neighborhoodRepository = neighborhoodRepository;
    }

    public CityGiftService() : this(
        new Database.Repositories.CityGiftDbRepository(),
        new Database.Repositories.BudgetCategoryDbRepository(),
        new Database.Repositories.NeighborhoodDbRepository())
    { }

    public List<CityGift> GetAvailable()
    {
        return _cityGiftRepository.GetAvailable();
    }

    public void Apply(long cityGiftId, long neighborhoodId, long coordinatorId)
    {
        if (_cityGiftRepository.HasApplied(cityGiftId, coordinatorId))
            throw new InvalidOperationException("You have already applied for this gift.");

        _cityGiftRepository.Apply(cityGiftId, neighborhoodId);
    }

    public void ProcessExpiredGifts()
    {
        var gifts = _cityGiftRepository.GetAvailable();
        var expiredGifts = gifts.Where(g => g.Deadline < DateOnly.FromDateTime(DateTime.Today)).ToList();

        foreach (var gift in expiredGifts)
        {
            long? neighborhoodId = _cityGiftRepository.GetNeighborhoodWithLowestBudget(gift.Id);
            if (neighborhoodId == null) continue;

            var categories = _categoryRepository.GetByNeighborhoodId(neighborhoodId.Value);
            if (categories.Count == 0) continue;

            decimal amountPerCategory = gift.Amount / categories.Count;
            foreach (var category in categories)
            {
                category.AddAmount(amountPerCategory);
                _categoryRepository.UpdateAmount(category.Id, category.TotalAmount);
            }

            _cityGiftRepository.Assign(gift.Id);
        }
    }
}