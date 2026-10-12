namespace CommunityHub.Application.Domain;

public class CommonRoom
{
    public int Id { get; private set; }
    public int BuildingId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Floor { get; private set; } // sprat
    public string RentalType { get; private set; } // "SingleDay" | "MultipleDays"

    public CommonRoom(int id, int buildingId, string name, string description, int floor, string rentalType)
    {
        Id = id;
        BuildingId = buildingId;
        Name = name;
        Description = description;
        Floor = floor;
        RentalType = rentalType;
    }
}
