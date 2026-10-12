namespace CommunityHub.Application.Domain;

public enum RentalRequestStatus
{
    Pending,
    PendingChangeOfDate,
    Approved,
    Rejected
}

public class CommonSpaceRentalRequest
{
    public int Id { get; private set; }
    public int CommonSpaceId { get; private set; }
    public int TenantId { get; private set; }
    public DateTime RequestedStartDate { get; private set; }
    public DateTime RequestedEndDate { get; private set; }
    public RentalRequestStatus Status { get; private set; }
    public DateTime? ApprovedDate { get; private set; }

    public CommonSpaceRentalRequest(int id, int commonSpaceId, int tenantId, DateTime requestedStartDate, DateTime requestedEndDate, RentalRequestStatus status, DateTime? approvedDate = null)
    {
        Id = id;
        CommonSpaceId = commonSpaceId;
        TenantId = tenantId;
        RequestedStartDate = requestedStartDate;
        RequestedEndDate = requestedEndDate;
        Status = status;
        ApprovedDate = approvedDate;
    }

    public void ChangeStatus(RentalRequestStatus newStatus)
    {
        Status = newStatus;
    }

    public void SetApprovedDate(DateTime date)
    {
        ApprovedDate = date;
        Status = RentalRequestStatus.Approved;
    }

    public void UpdateRequestedDates(DateTime start, DateTime end)
    {
        RequestedStartDate = start;
        RequestedEndDate = end;
        Status = RentalRequestStatus.PendingChangeOfDate;
    }

    // Vraca sve zauzete datume za ovu rezervaciju.
    // Za SingleDay prostore koristi ApprovedDate, za MultipleDays ceo opseg.
    public List<DateTime> GetOccupiedDates()
    {
        if (Status != RentalRequestStatus.Approved)
            return new List<DateTime>();

        if (ApprovedDate.HasValue)
            return new List<DateTime> { ApprovedDate.Value.Date };

        var dates = new List<DateTime>();
        for (var date = RequestedStartDate.Date; date <= RequestedEndDate.Date; date = date.AddDays(1))
        {
            dates.Add(date);
        }
        return dates;
    }
}