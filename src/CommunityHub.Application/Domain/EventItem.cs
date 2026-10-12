namespace CommunityHub.Application.Domain;

public class EventItem
{
    public long Id { get; private set; }
    public long EventId { get; private set; }
    public string Name { get; private set; }
    public bool IsCovered { get; internal set; }

    public EventItem(long id, long eventId, string name, bool isCovered)
    {
        Id = id;
        EventId = eventId;
        Name = name;
        IsCovered = isCovered;
    }
}