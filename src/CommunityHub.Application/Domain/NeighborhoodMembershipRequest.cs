namespace CommunityHub.Application.Domain;

public class NeighborhoodMembershipRequest
{
    public long Id { get;  set; }
    public long NeighborhoodId { get;  set; }
    public long CitizenId { get;  set; }

    public Neighborhood? Neighborhood { get;  set; }
    public Citizen? Citizen { get; set; }

    public DateTime CreatedAt { get; set; }
    public NeighborhoodMembershipRequestStatus Status { get;  set; }
    public string? RejectionReason { get;  set; }

    public NeighborhoodMembershipRequest(long id, long neighborhoodId, long citizenId)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        CitizenId = citizenId;
        CreatedAt = DateTime.UtcNow;
        Neighborhood = null;
        Citizen = null;
        Status = NeighborhoodMembershipRequestStatus.Pending;
        RejectionReason = null;
    }

    public NeighborhoodMembershipRequest(long id, long neighborhoodId, long citizenId, DateTime createdAt,NeighborhoodMembershipRequestStatus status,
    string? rejectionReason)
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        CitizenId = citizenId;
        CreatedAt = createdAt;
        Status = status;
        RejectionReason = rejectionReason;
        Neighborhood = null;
        Citizen = null;
    }

    public NeighborhoodMembershipRequest(long id)
    {
        Id = id;
        
    }

    public void Approve()
    {
        Status = NeighborhoodMembershipRequestStatus.Approved;
        RejectionReason = null;
    }

    public void Reject(string? rejectionReason)
    {
        Status = NeighborhoodMembershipRequestStatus.Rejected;
        RejectionReason = rejectionReason;
    }

    public bool IsPending => Status == NeighborhoodMembershipRequestStatus.Pending;

    public bool IsRejected => Status == NeighborhoodMembershipRequestStatus.Rejected;
}