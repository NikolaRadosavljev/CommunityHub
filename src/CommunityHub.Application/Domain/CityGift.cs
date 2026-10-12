namespace CommunityHub.Application.Domain;

public class CityGift
{
    public long Id { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Deadline { get; private set; }
    public bool IsAssigned { get; private set; }

    public CityGift(long id, decimal amount, DateOnly deadline, bool isAssigned)
    {
        Id = id;
        Amount = amount;
        Deadline = deadline;
        IsAssigned = isAssigned;
    }

    public void Assign()
    {
        IsAssigned = true;
    }
}