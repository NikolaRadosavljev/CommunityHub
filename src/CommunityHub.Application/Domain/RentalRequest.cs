namespace CommunityHub.Application.Domain;

public class RentalRequest
{
    public int Id { get; private set; }
    public int CommonRoomId { get; private set; }
    public long TenantId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string Status { get; private set; } // "Pending" | "Pending - Term Change" | "Approved" | "Rejected"
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedDate { get; private set; }
    public DateTime? SuggestedStartDate { get; private set; }
    public DateTime? SuggestedEndDate { get; private set; }

    public RentalRequest(int id, int commonRoomId, long tenantId, DateTime startDate, DateTime endDate,
                         string status, DateTime createdAt, DateTime? approvedDate = null,
                         DateTime? suggestedStartDate = null, DateTime? suggestedEndDate = null)
    {
        Id = id;
        CommonRoomId = commonRoomId;
        TenantId = tenantId;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        CreatedAt = createdAt;
        ApprovedDate = approvedDate;
        SuggestedStartDate = suggestedStartDate;
        SuggestedEndDate = suggestedEndDate;
    }
}
