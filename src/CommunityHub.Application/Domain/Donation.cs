namespace CommunityHub.Application.Domain;

public class Donation
{
    public long Id { get; private set; }
    public long CategoryId { get; private set; }
    public long CitizenId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DonatedAt { get; private set; }

    public Donation(long id, long categoryId, long citizenId, decimal amount, DateTime donatedAt)
    {
        Id = id;
        CategoryId = categoryId;
        CitizenId = citizenId;
        Amount = amount;
        DonatedAt = donatedAt;
    }
}