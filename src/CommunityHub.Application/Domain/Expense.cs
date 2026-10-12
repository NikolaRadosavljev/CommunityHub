namespace CommunityHub.Application.Domain;

public class Expense
{
    public long Id { get; private set; }
    public long CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public DateOnly ExpenseDate { get; private set; }

    public Expense(long id, long categoryId, decimal amount, string description, DateOnly expenseDate)
    {
        Id = id;
        CategoryId = categoryId;
        Amount = amount;
        Description = description;
        ExpenseDate = expenseDate;
    }
}