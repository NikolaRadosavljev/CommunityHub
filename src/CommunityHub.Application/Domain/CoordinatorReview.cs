namespace CommunityHub.Application.Domain;

public class CoordinatorReview
{
    public long Id { get; private set; }
    public long CoordinatorId { get; private set; }
    public long CitizenId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRemoved { get; private set; }

    public CoordinatorReview(long id, long coordinatorId, long citizenId,
        int rating, string? comment, DateTime createdAt, bool isRemoved)
    {
        Id = id;
        CoordinatorId = coordinatorId;
        CitizenId = citizenId;
        Rating = rating;
        Comment = comment;
        CreatedAt = createdAt;
        IsRemoved = isRemoved;
    }
}