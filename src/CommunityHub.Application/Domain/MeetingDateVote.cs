namespace CommunityHub.Application.Domain;

public class MeetingDateVote
{
    public long Id { get; private set; }
    public long MeetingId { get; private set; }
    public long CitizenId { get; private set; }
    public DateOnly VotedDate { get; private set; }

    public MeetingDateVote(long id, long meetingId, long citizenId, DateOnly votedDate)
    {
        Id = id;
        MeetingId = meetingId;
        CitizenId = citizenId;
        VotedDate = votedDate;
    }
}