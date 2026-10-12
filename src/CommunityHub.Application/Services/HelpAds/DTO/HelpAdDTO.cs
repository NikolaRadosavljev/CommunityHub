using System;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class HelpAdDTO
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = "offering"; // "offering" or "seeking"
    }
}
