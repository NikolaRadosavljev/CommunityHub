using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Services;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface IHomeownersMeetingRepository
    {
        Task<long> CreateMeetingAsync(CreateMeetingDTO meeting);
        Task<HomeownersMeeting> GetMeetingByIdAsync(long meetingId);
        Task<List<HomeownersMeeting>> GetMeetingsByBuildingAsync(long buildingId);
        Task UpdateMeetingStatusAsync(long meetingId, string newStatus);
        Task<int> GetAttendanceCountAsync(long meetingId);
        Task<int> GetBuildingStanCount(long buildingId);
        Task ReportAttendanceAsync(long meetingId, long tenantId, bool attended);
        Task<bool> HasUserReportedAttendanceAsync(long meetingId, long tenantId);
        Task<List<MeetingAttendance>> GetAllAttendanceForMeetingAsync(long meetingId);
        Task ProposeMeetingTopicAsync(long meetingId, long tenantId, string topicText);
        Task AddOfficialTopicAsync(long meetingId, string topicText, long managerId);
        Task AcceptProposedTopicAsync(long topicId);
        Task<List<ProposedTopic>> GetMeetingTopicsAsync(long meetingId);
        Task<List<ProposedTopic>> GetMeetingTopicsWithStatusAsync(long meetingId);
        Task<List<HomeownersMeeting>> GetUpcomingMeetingsAsync(long buildingId);
        Task<List<HomeownersMeeting>> GetMeetingsNearingDeadlineAsync();
    }
}
