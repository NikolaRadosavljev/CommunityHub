namespace CommunityHub.Application.Domain;
public class Flat
{
    public long Id { get; private set; }
    public int BuildingId { get; private set; }
    public int FloorNumber { get; private set; }
    public int FlatNumber { get; private set; }

    public Flat(long id, int buildingId, int floorNumber, int flatNumber)
    {
        Id = id;
        BuildingId = buildingId;
        FloorNumber = floorNumber;
        FlatNumber = flatNumber;
    }
}