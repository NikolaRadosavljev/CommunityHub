namespace CommunityHub.Application.Domain;

public class EventVolunteer
{
    public long Id { get; private set; }
    public long EventId { get; private set; }
    public long CitizenId { get; private set; }
    public bool? Attended { get; internal set; }
    public List<EventItem>? Items { get; private set; }

    public EventVolunteer(long id, long eventId, long citizenId, bool? attended)
    {
        Id = id;
        EventId = eventId;
        CitizenId = citizenId;
        Attended = attended;
        Items = null;
    }

    public void AddItem(EventItem item)
    {
        if (Items == null) Items = new List<EventItem>();
        Items.Add(item);
    }
}