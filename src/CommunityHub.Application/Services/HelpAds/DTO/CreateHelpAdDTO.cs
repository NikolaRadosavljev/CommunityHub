using System;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class CreateHelpAdDTO
    {
        public long BuildingId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = "offering"; // "offering" or "seeking"
        public TimeSpan StartTime { get; set; } = new TimeSpan(13, 0, 0); // Default: 13:00
        public TimeSpan EndTime { get; set; } = new TimeSpan(20, 0, 0); // Default: 20:00
    }
}
