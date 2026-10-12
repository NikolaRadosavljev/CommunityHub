namespace CommunityHub.Application.Domain;

public class CitizenTrustRecord
{
    public long CitizenId { get; private set; }
    public long NeighborhoodId { get; private set; }
    public string CitizenFullName { get; private set; }
    public int OrganizedEvents { get; private set; }
    public int VolunteeredEvents { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public CitizenTrustRecord(long citizenId, long neighborhoodId, string citizenFullName,
                               int organizedEvents, int volunteeredEvents, DateTime joinedAt)
    {
        CitizenId = citizenId;
        NeighborhoodId = neighborhoodId;
        CitizenFullName = citizenFullName;
        OrganizedEvents = organizedEvents;
        VolunteeredEvents = volunteeredEvents;
        JoinedAt = joinedAt;
    }

    public CitizenTrustLevel GetLevel()
    {
        int totalEvents = OrganizedEvents + VolunteeredEvents;
        bool moreThanOneYear = (DateTime.UtcNow - JoinedAt).TotalDays > 365;

        if (!moreThanOneYear)
            return CitizenTrustLevel.New;
        if (totalEvents < 5)
            return CitizenTrustLevel.Inactive;
        if (totalEvents <= 10)
            return CitizenTrustLevel.Active;
        if (totalEvents <= 15)
            return CitizenTrustLevel.Distinguished;
        return CitizenTrustLevel.Trusted;
    }
}