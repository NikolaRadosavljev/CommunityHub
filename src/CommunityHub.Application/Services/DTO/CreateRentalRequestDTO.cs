namespace CommunityHub.Application.Services.DTO;

public class CreateRentalRequestDTO
{
    public int CommonRoomId { get; set; }
    public long TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
