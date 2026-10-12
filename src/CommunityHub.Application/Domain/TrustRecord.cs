namespace CommunityHub.Application.Domain;

public enum CitizenLevel
{
    NovUKvartu,
    NeaktivniGradjanin,
    AktivniGradjanin,
    IstaknutiGradjanin,
    GradjaninOdPoverenja
}

public class TrustRecord
{
    public int OrganizedEvents { get; private set; }
    public int VolunteeredEvents { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public CitizenLevel Level { get; private set; }

    public TrustRecord(int organizedEvents, int volunteeredEvents, DateTime joinedAt)
    {
        OrganizedEvents = organizedEvents;
        VolunteeredEvents = volunteeredEvents;
        JoinedAt = joinedAt;
        Level = CalculateLevel();
    }

    public int TotalEvents => OrganizedEvents + VolunteeredEvents;

    public string LevelDisplayName => Level switch
    {
        CitizenLevel.NovUKvartu => "New Citizen",
        CitizenLevel.NeaktivniGradjanin => "Inactive Citizen",
        CitizenLevel.AktivniGradjanin => "Active Citizen",
        CitizenLevel.IstaknutiGradjanin => "Notable Citizen",
        CitizenLevel.GradjaninOdPoverenja => "Trusted Citizen",
        _ => "Unknown"
    };

    private CitizenLevel CalculateLevel()
    {
        bool isNewMember = DateTime.Now - JoinedAt < TimeSpan.FromDays(365);
        if (isNewMember) return CitizenLevel.NovUKvartu;
        if (TotalEvents < 5) return CitizenLevel.NeaktivniGradjanin;
        if (TotalEvents <= 10) return CitizenLevel.AktivniGradjanin;
        if (TotalEvents <= 15) return CitizenLevel.IstaknutiGradjanin;
        return CitizenLevel.GradjaninOdPoverenja;
    }
}