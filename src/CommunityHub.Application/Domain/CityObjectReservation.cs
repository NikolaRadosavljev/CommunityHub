namespace CommunityHub.Application.Domain;

public class CityObjectReservation
{
    public long Id { get; private set; }
    public long CityObjectId { get; private set; }
    public long NeighborhoodId { get; private set; }
    public DateTime StartDatetime { get; private set; }
    public DateTime EndDatetime { get; private set; }
    public int DurationMinutes { get; private set; }

    public CityObjectReservation(long id, long cityObjectId, long neighborhoodId,
        DateTime startDatetime, DateTime endDatetime, int durationMinutes)
    {
        Id = id;
        CityObjectId = cityObjectId;
        NeighborhoodId = neighborhoodId;
        StartDatetime = startDatetime;
        EndDatetime = endDatetime;
        DurationMinutes = durationMinutes;
    }
}