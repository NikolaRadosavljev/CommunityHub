using System;

namespace CommunityHub.Application.Services
{
    public class CreateProblemDTO
    {
        public long BuildingId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; } // 1, 2, 3
    }

    public class ProblemDTO
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }

    public class UpdateProblemStatusDTO
    {
        public long ProblemId { get; set; }
        public string NewStatus { get; set; } = string.Empty; // Potentially_resolved, Resolved
    }
}
