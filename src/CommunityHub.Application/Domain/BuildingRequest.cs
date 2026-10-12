namespace CommunityHub.Application.Domain;

public enum RequestStatus
{
    Pending,
    Accepted,
    Denied
}

public class BuildingRequest
{
    public int Id { get; private set; }
    public User User { get; private set; }
    public int BuildingId { get; private set; }
    public int FlatNumber { get; private set; }
    public DateTime Date { get; private set; }
    public RequestStatus Status { get; private set; }
    public string? RejectionExplanation { get; private set; }

    public BuildingRequest(int id, User user, int buildingId, int flatNumber,
                           DateTime date, RequestStatus status, string? explanation)
    {
        Id = id;
        User = user;
        BuildingId = buildingId;
        FlatNumber = flatNumber;
        Date = date;
        Status = status;
        RejectionExplanation = explanation;
    }

    public void Approve()
    {
        Status = RequestStatus.Accepted;
        RejectionExplanation = null;
    }

    public void Reject(string explanation)
    {
        Status = RequestStatus.Denied;
        RejectionExplanation = explanation;
    }
}