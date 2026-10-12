namespace CommunityHub.Application.Domain;

public class CityObject
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int VoteCount { get; private set; }
    public DateOnly? LastVisitDate { get; private set; }

    public CityObject(long id, string name, string? description, int voteCount, DateOnly? lastVisitDate)
    {
        Id = id;
        Name = name;
        Description = description;
        VoteCount = voteCount;
        LastVisitDate = lastVisitDate;
    }

    public void ResetVotes()
    {
        VoteCount = 0;
    }
}