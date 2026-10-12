namespace CommunityHub.Application.Domain;

public class Forum
{
    public long Id { get; private set; }
    public long CoordinatorId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsClosed { get; private set; }

    public Forum(long id, long coordinatorId, string title, string description, bool isClosed = false)
    {
        Id = id;
        CoordinatorId = coordinatorId;
        Title = title;
        Description = description;
        IsClosed = isClosed;
    }

    public void Close()
    {
        IsClosed = true;
    }
}