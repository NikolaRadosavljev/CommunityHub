using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class MeetingService
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingDateVoteRepository _voteRepository;

    public MeetingService(IMeetingRepository meetingRepository,
                          IMeetingDateVoteRepository voteRepository)
    {
        _meetingRepository = meetingRepository;
        _voteRepository = voteRepository;
    }

    public MeetingService() : this(
    new MeetingDbRepository(),
    new MeetingDateVoteDbRepository())
    { }

    public long CreateMeeting(long neighborhoodId, MeetingTopic topic,
                              TimeOnly time, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Početni datum ne može biti posle krajnjeg datuma.");

        if (startDate < DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Datum ne može biti u prošlosti.");

        Meeting meeting = new Meeting(0, neighborhoodId, topic, time, startDate, endDate);
        return _meetingRepository.Create(meeting);
    }

    public List<Meeting> GetMeetingsForCoordinator(long coordinatorId)
    {
        return _meetingRepository.GetByCoordinatorId(coordinatorId);
    }

    public void ProcessMeetingsBeforeStart()
    {
        List<Meeting> meetings = _meetingRepository.GetInPreparationWithStartDateTomorrow();

        foreach (Meeting meeting in meetings)
        {
            DateOnly? winningDate = _voteRepository.GetWinningDate(meeting.Id);

            if (winningDate == null)
            {
                List<MeetingDateVote> votes = _voteRepository.GetByMeetingId(meeting.Id);
                if (votes.Count == 0)
                {
                    meeting.Cancel();
                    _meetingRepository.UpdateStatus(meeting.Id, meeting.Status, null);
                }
            }
            else
            {
                meeting.Schedule(winningDate.Value);
                _meetingRepository.UpdateStatus(meeting.Id, meeting.Status, meeting.ScheduledDate);
            }
        }
    }

    public void ManuallySchedule(long meetingId, DateOnly chosenDate)
    {
        ProcessManualSchedule(meetingId, chosenDate);
    }

    private void ProcessManualSchedule(long meetingId, DateOnly chosenDate)
    {
        _meetingRepository.UpdateStatus(meetingId, MeetingStatus.Scheduled, chosenDate);
    }

    public List<DateOnly> GetTiedDates(long meetingId)
    {
        List<MeetingDateVote> votes = _voteRepository.GetByMeetingId(meetingId);

        if (votes.Count == 0) return new List<DateOnly>();

        var grouped = votes
            .GroupBy(v => v.VotedDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        int maxCount = grouped.First().Count;
        return grouped
            .Where(x => x.Count == maxCount)
            .Select(x => x.Date)
            .ToList();
    }

    public List<Meeting> GetMeetingsForNeighborhood(long neighborhoodId)
    {
        return _meetingRepository.GetByNeighborhoodId(neighborhoodId);
    }

    public string VoteForDate(long meetingId, long citizenId, DateOnly votedDate, DateOnly startDate)
    {
        if (DateTime.UtcNow >= startDate.ToDateTime(TimeOnly.MinValue).AddHours(-24))
            return "Voting is closed 24h before the start date.";

        if (_voteRepository.HasVote(meetingId, citizenId))
            return "You have already voted for this meeting.";

        _voteRepository.Create(new MeetingDateVote(0, meetingId, citizenId, votedDate));
        return "You have successfully voted.";
    }

    public string ChangeVote(long meetingId, long citizenId, DateOnly newDate, DateOnly startDate)
    {
        if (DateTime.UtcNow >= startDate.ToDateTime(TimeOnly.MinValue).AddHours(-24))
            return "Voting is closed 24h before the start date.";

        if (!_voteRepository.HasVote(meetingId, citizenId))
            return "You have not voted for this meeting.";

        _voteRepository.Update(meetingId, citizenId, newDate);
        return "You have successfully changed your vote.";
    }

    public List<MeetingDateVote> GetVotesForMeeting(long meetingId)
    {
        return _voteRepository.GetByMeetingId(meetingId);
    }
}