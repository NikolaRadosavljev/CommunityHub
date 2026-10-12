using System;

namespace CommunityHub.Application.Domain
{
    public class HomeownersMeeting
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string Status { get; set; } = "scheduled"; // scheduled, confirmed, cancelled
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
