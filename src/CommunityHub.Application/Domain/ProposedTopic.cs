using System;

namespace CommunityHub.Application.Domain
{
    public class ProposedTopic
    {
        public long Id { get; set; }
        public long MeetingId { get; set; }
        public string TopicText { get; set; } = string.Empty;
        public long ProposedByTenantId { get; set; }
        public DateTime ProposedDate { get; set; }
        public bool IsAccepted { get; set; }
    }
}
