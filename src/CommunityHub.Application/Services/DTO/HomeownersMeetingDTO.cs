using System;
using System.Collections.Generic;

namespace CommunityHub.Application.Services
{
    public class HomeownersMeetingDTO
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalStans { get; set; }
        public int AttendanceCount { get; set; }
        public decimal AttendancePercentage { get; set; }
        public bool UserAttending { get; set; }
        public List<MeetingTopicDTO> Topics { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class MeetingTopicDTO
    {
        public long Id { get; set; }
        public string TopicText { get; set; } = string.Empty;
        public string ProposedByName { get; set; } = string.Empty;
        public long ProposedByTenantId { get; set; }
        public DateTime ProposedDate { get; set; }
    }

    public class CreateMeetingDTO
    {
        public long BuildingId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledTime { get; set; }
    }

    public class ReportAttendanceDTO
    {
        public long MeetingId { get; set; }
        public long TenantId { get; set; }
        public bool Attended { get; set; }
    }

    public class ProposeMeetingTopicDTO
    {
        public long MeetingId { get; set; }
        public long TenantId { get; set; }
        public string TopicText { get; set; } = string.Empty;
    }
}
