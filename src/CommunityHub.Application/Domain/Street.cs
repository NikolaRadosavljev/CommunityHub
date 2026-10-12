namespace CommunityHub.Application.Domain;

public class Street
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public int StartNumber { get; private set; }
    public int EndNumber { get; private set; }
    public long NeighbourhoodId { get; internal set; }

    public Street(long id, string name, int startNumber, int endNumber, long neighborhoodId)
    {
        Id = id;
        Name = name;
        StartNumber = startNumber;
        EndNumber = endNumber;
        NeighbourhoodId = neighborhoodId;
    }
    public Street(string name, int startNumber, int endNumber, long neighborhoodId)
    {
        Name = name;
        StartNumber = startNumber;
        EndNumber = endNumber;
        NeighbourhoodId = neighborhoodId;
    }
}