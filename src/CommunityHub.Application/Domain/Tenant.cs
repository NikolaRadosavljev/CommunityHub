using CommunityHub.Application.Domain;

public class Tenant
{
    public User User { get; private set; }
    public int FloorNumber { get; private set; }
    public int FlatNumber { get; private set; }
    public int BuildingId { get; private set; }

    public Tenant(User user, int floorNumber, int flatNumber, int buildingId)
    {
        User = user;
        FloorNumber = floorNumber;
        FlatNumber = flatNumber;
        BuildingId = buildingId;
    }
}