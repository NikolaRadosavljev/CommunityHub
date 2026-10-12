namespace CommunityHub.Application.Services.DTO;

public class RentalRequestDTO
{
    public int Id { get; set; }
    public int CommonRoomId { get; set; }
    public string CommonRoomName { get; set; }
    public long TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string RentalType { get; set; }
    public DateTime? SuggestedStartDate { get; set; }
    public DateTime? SuggestedEndDate { get; set; }
    public int? Floor { get; set; }
}
