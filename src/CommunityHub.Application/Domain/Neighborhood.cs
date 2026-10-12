namespace CommunityHub.Application.Domain;

public class Neighborhood
{
    public long Id { get; private set; }
    public long CoordinatorId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<Street> Streets { get; private set; }
    public City? City { get; internal set; }
    public List<string> Images { get; private set; }
    public decimal Budget { get; private set; }

    public Neighborhood(long id, long coordinatorId, string name, string description)
    {
        Id = id;
        CoordinatorId = coordinatorId;
        Name = name;
        Description = description;
        Budget = 0;
        City = null;
        Streets = new List<Street>();
        Images = new List<string>();
    }

    public void AddStreet(Street street)
    {
        Streets.Add(street);
    }

    public void AddImage(string imagePath)
    {
        Images.Add(imagePath);
    }
}
