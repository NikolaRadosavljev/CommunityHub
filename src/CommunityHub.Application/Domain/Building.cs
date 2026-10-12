namespace CommunityHub.Application.Domain;

public class Building
{
    public int Id { get; private set; }
    public string Address { get; private set; }
    public string Settlement { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }
    public int FloorNumber { get; private set; } //broj spratova
    public List<int> FlatNumbers { get; private set; } //brojevi stanova u zgradi
    public List<string> Pictures { get; private set; }
    public int ManagerId { get; private set; }

    public Building(int id, string address, string settlement, string city, string country,
                    int floorNumber, List<int> flatNumbers, List<string> pictures, int managerId)
    {
        Id = id;
        Address = address;
        Settlement = settlement;
        City = city;
        Country = country;
        FloorNumber = floorNumber;
        FlatNumbers = flatNumbers;
        Pictures = pictures;
        ManagerId = managerId;
    }
}