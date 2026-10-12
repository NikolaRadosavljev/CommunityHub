using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IMeetingDateVoteRepository
{
    void Create(MeetingDateVote vote);
    void Update(long meetingId, long citizenId, DateOnly newDate);
    List<MeetingDateVote> GetByMeetingId(long meetingId);
    DateOnly? GetWinningDate(long meetingId);
    bool HasVote(long meetingId, long citizenId);
}