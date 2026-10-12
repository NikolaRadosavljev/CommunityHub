namespace CommunityHub.Application.Domain;

public class BudgetCategory
{
    public long Id { get; private set; }
    public long NeighborhoodId { get; private set; }
    public string Name { get; private set; }
    public decimal TotalAmount { get; private set; }

    public BudgetCategory(long id, long neighborhoodId, string name, decimal totalAmount)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        Name = name;
        TotalAmount = totalAmount;
    }

    public void AddAmount(decimal amount)
    {
        TotalAmount += amount;
    }

    public void SubtractAmount(decimal amount)
    {
        TotalAmount -= amount;
    }
}