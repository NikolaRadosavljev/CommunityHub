namespace CommunityHub.Application.Domain;

public class BuildingMembership
{
    public int Id { get; private set; }
    public int BuildingId { get; private set; }
    public User User { get; private set; }
    public int FloorNumber { get; private set; }
    public int FlatNumber { get; private set; }
    public DateTime Date { get; private set; }

    public BuildingMembership(int id, int buildingId, User user,
                      int floorNumber, int flatNumber, DateTime date)
    {
        Id = id;
        BuildingId = buildingId;
        User = user;
        FloorNumber = floorNumber;
        FlatNumber = flatNumber;
        Date = date;
    }
}