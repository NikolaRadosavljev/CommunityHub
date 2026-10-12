using System;

namespace CommunityHub.Application.Domain
{
    public class MeetingAttendance
    {
        public long Id { get; set; }
        public long MeetingId { get; set; }
        public long TenantId { get; set; }
        public bool Attended { get; set; }
        public DateTime ReportedDate { get; set; }
    }
}
