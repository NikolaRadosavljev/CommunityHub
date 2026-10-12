namespace CommunityHub.Application.Domain;

public class Post
{
    public long Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public User? User { get; internal set; }

    public Post(long id, string title, string content, DateTime createdAt)
    {
        Id = id;
        Title = title;
        Content = content;
        CreatedAt = createdAt;
        User = null;
    }

    public Post(string title, string content)
    {
        Id = 0;
        Title = title;
        Content = content;
        CreatedAt = DateTime.Now;
        User = null;
    }
}
