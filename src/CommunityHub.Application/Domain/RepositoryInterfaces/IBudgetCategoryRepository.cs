using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IBudgetCategoryRepository
{
    List<BudgetCategory> GetByNeighborhoodId(long neighborhoodId);
    void UpdateAmount(long categoryId, decimal newAmount);
    long Create(BudgetCategory category);
}