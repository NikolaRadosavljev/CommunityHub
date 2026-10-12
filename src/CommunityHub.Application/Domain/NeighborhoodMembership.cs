namespace CommunityHub.Application.Domain;

public class NeighborhoodMembership
{
    public long Id { get; private set; }
    public long NeighborhoodId { get; private set; }
    public long CitizenId { get; private set; }
    public Citizen? Citizen { get; internal set; }
    public DateTime JoinedAt { get; private set; }

    public NeighborhoodMembership(long id, long neighborhoodId, long citizenId)   //konstruktor za novo clanstvo
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        CitizenId = citizenId;
        JoinedAt = DateTime.UtcNow;
        Citizen = null;
    }

    public NeighborhoodMembership(long id, long neighborhoodId, long citizenId, DateTime joinedAt)   //drugi konstruktor za citanje iz baze
    {
        Id = id;
        NeighborhoodId = neighborhoodId;
        CitizenId = citizenId;
        JoinedAt = joinedAt;
        Citizen = null;
    }
}