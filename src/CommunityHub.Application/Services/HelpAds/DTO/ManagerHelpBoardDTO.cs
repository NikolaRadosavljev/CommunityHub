using System;
using System.Collections.Generic;

namespace CommunityHub.Application.Services.HelpAds.DTO
{
    public class ManagerHelpAdOverviewDTO
    {
        public long Id { get; set; }
        public long TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
    }

    public class ManagerCategorySplitCountDTO
    {
        public string Category { get; set; } = string.Empty;
        public int OfferingCount { get; set; }
        public int SeekingCount { get; set; }
    }

    public class ManagerCategoryActiveCountDTO
    {
        public string Category { get; set; } = string.Empty;
        public int ActiveCount { get; set; }
    }

    public class ManagerMostHelpfulTenantDTO
    {
        public string TenantName { get; set; } = string.Empty;
        public int BookedSlotsCount { get; set; }
    }

    public class ManagerHelpBoardStatisticsDTO
    {
        public int OfferingCreatedCount { get; set; }
        public int SeekingCreatedCount { get; set; }
        public int ActiveCount { get; set; }
        public int ArchivedCount { get; set; }
        public List<ManagerCategorySplitCountDTO> CategorySplitCounts { get; set; } = new();
        public List<ManagerCategoryActiveCountDTO> ActiveByCategory { get; set; } = new();
        public ManagerMostHelpfulTenantDTO? MostHelpfulTenant { get; set; }
    }
}
