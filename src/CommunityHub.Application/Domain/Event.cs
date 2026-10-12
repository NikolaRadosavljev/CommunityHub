namespace CommunityHub.Application.Domain;

public enum EventStatus
{
    Preparation,
    Scheduled,
    Cancelled,
    Finished
}

public class Event
{
    public long Id { get; private set; }
    public long NeighborhoodId { get; private set; }
    public long OrganizerId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MinVolunteers { get; private set; }
    public EventStatus Status { get; internal set; }
    public List<EventItem>? Items { get; private set; }
    public List<EventVolunteer>? Volunteers { get; private set; }

    public Event(long id, long neighborhoodId, long organizerId, string name, string description,
        DateOnly date, TimeOnly startTime, int durationMinutes, int minVolunteers, EventStatus status)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        OrganizerId = organizerId;
        Name = name;
        Description = description;
        Date = date;
        StartTime = startTime;
        DurationMinutes = durationMinutes;
        MinVolunteers = minVolunteers;
        Status = status;
        Items = null;
        Volunteers = null;
    }

    public void AddItem(EventItem item)
    {
        if (Items == null) Items = new List<EventItem>();
        Items.Add(item);
    }

    public void AddVolunteer(EventVolunteer volunteer)
    {
        if (Volunteers == null) Volunteers = new List<EventVolunteer>();
        Volunteers.Add(volunteer);
    }

    public int CurrentVolunteerCount => Volunteers?.Count ?? 0;
    public bool AllItemsCovered => Items != null && Items.All(i => i.IsCovered);
    public bool IsOrganizer(long userId) => OrganizerId == userId;
    public DateTime EventDateTime => Date.ToDateTime(StartTime);
}