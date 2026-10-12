namespace CommunityHub.Application.Domain;

public class CommonSpace
{
    public int Id { get; private set; }
    public int BuildingId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int FloorNumber { get; private set; }
    public CommonSpaceRentalType RentalType { get; private set; }

    public CommonSpace(int id, int buildingId, string name, string description, int floorNumber, CommonSpaceRentalType rentalType)
    {
        Id = id;
        BuildingId = buildingId;
        Name = name;
        Description = description;
        FloorNumber = floorNumber;
        RentalType = rentalType;
    }

    public string GetRentalTypeLabel()
    {
        return RentalType == CommonSpaceRentalType.SingleDay
            ? "Single Day"
            : "Multiple Days";
    }
}
