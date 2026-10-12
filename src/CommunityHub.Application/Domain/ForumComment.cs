namespace CommunityHub.Application.Domain;

public class ForumComment
{
    public long Id { get; private set; }
    public long ForumId { get; private set; }
    public long CoordinatorId { get; private set; }
    public string CoordinatorFullName { get; private set; }
    public string NeighborhoodName { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int Likes { get; private set; }
    public int Dislikes { get; private set; }

    public ForumComment(long id, long forumId, long coordinatorId, string coordinatorFullName,
                        string neighborhoodName, string content, DateTime createdAt,
                        int likes = 0, int dislikes = 0)
    {
        Id = id;
        ForumId = forumId;
        CoordinatorId = coordinatorId;
        CoordinatorFullName = coordinatorFullName;
        NeighborhoodName = neighborhoodName;
        Content = content;
        CreatedAt = createdAt;
        Likes = likes;
        Dislikes = dislikes;
    }
}