using System;

namespace CommunityHub.Application.Domain
{
    public class Problem
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; } // 1 = može čekati, 2 = treba rešiti uskoro, 3 = hitno
        public string Status { get; set; } = "Unresolved"; // Unresolved, Potentially_resolved, Resolved
        public DateTime ReportedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }

    public enum ProblemPriority
    {
        CanWait = 1,
        ShouldResolveSoon = 2,
        UrgentlyNeeded = 3
    }

    public enum ProblemStatus
    {
        Unresolved,
        PotentiallyResolved,
        Resolved
    }
}
