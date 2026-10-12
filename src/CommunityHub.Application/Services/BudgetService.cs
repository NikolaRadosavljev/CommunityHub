using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class BudgetService
{
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;

    public BudgetService(IBudgetCategoryRepository categoryRepository,
                         IDonationRepository donationRepository,
                         IExpenseRepository expenseRepository,
                         INeighborhoodRepository neighborhoodRepository)
    {
        _categoryRepository = categoryRepository;
        _donationRepository = donationRepository;
        _expenseRepository = expenseRepository;
        _neighborhoodRepository = neighborhoodRepository;
    }

    public BudgetService() : this(
        new Database.Repositories.BudgetCategoryDbRepository(),
        new Database.Repositories.DonationDbRepository(),
        new Database.Repositories.ExpenseDbRepository(),
        new Database.Repositories.NeighborhoodDbRepository())
    { }

    public List<BudgetCategory> GetCategories(long neighborhoodId)
    {
        return _categoryRepository.GetByNeighborhoodId(neighborhoodId);
    }

    public List<Donation> GetDonations(long categoryId)
    {
        return _donationRepository.GetByCategoryId(categoryId);
    }

    public List<Expense> GetExpenses(long categoryId)
    {
        return _expenseRepository.GetByCategoryId(categoryId);
    }

    public void AddExpense(long categoryId, decimal amount, string description, DateOnly expenseDate)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");

        var categories = _categoryRepository.GetByNeighborhoodId(0);
        var category = categories.FirstOrDefault(c => c.Id == categoryId);

        Expense expense = new Expense(0, categoryId, amount, description, expenseDate);
        _expenseRepository.Create(expense);

        if (category != null)
        {
            category.SubtractAmount(amount);
            _categoryRepository.UpdateAmount(categoryId, category.TotalAmount);
        }
    }

    public long CreateCategory(long neighborhoodId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.");

        BudgetCategory category = new BudgetCategory(0, neighborhoodId, name, 0);
        return _categoryRepository.Create(category);
    }
}