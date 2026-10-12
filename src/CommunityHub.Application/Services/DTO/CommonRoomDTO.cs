namespace CommunityHub.Application.Services.DTO;

public class CommonRoomDTO
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Floor { get; set; }
    public string RentalType { get; set; } // "SingleDay" | "MultipleDays"

    public string RentalTypeDisplay => RentalType == "SingleDay" ? "Single Day" : "Multiple Days";
}
