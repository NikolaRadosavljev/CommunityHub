namespace CommunityHub.Application.Domain;

public class Meeting
{
    public long Id { get; private set; }
    public long NeighborhoodId { get; private set; }
    public MeetingTopic Topic { get; private set; }
    public TimeOnly Time { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public MeetingStatus Status { get; private set; }
    public DateOnly? ScheduledDate { get; private set; }

    public Meeting(long id, long neighborhoodId, MeetingTopic topic,
                   TimeOnly time, DateOnly startDate, DateOnly endDate)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        Topic = topic;
        Time = time;
        StartDate = startDate;
        EndDate = endDate;
        Status = MeetingStatus.InPreparation;
        ScheduledDate = null;
    }

    public Meeting(long id, long neighborhoodId, MeetingTopic topic,
                   TimeOnly time, DateOnly startDate, DateOnly endDate,
                   MeetingStatus status, DateOnly? scheduledDate)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        Topic = topic;
        Time = time;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        ScheduledDate = scheduledDate;
    }

    public void Schedule(DateOnly date)
    {
        ScheduledDate = date;
        Status = MeetingStatus.Scheduled;
    }

    public void Cancel()
    {
        Status = MeetingStatus.Cancelled;
    }
}