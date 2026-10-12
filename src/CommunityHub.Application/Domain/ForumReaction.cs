namespace CommunityHub.Application.Domain;

public class ForumReaction
{
    public long Id { get; private set; }
    public long CommentId { get; private set; }
    public long CoordinatorId { get; private set; }
    public bool IsPositive { get; private set; }

    public ForumReaction(long id, long commentId, long coordinatorId, bool isPositive)
    {
        Id = id;
        CommentId = commentId;
        CoordinatorId = coordinatorId;
        IsPositive = isPositive;
    }
}