using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class CityObjectService
{
    private readonly ICityObjectRepository _cityObjectRepository;
    private readonly ICityObjectReservationRepository _reservationRepository;

    public CityObjectService(ICityObjectRepository cityObjectRepository,
                              ICityObjectReservationRepository reservationRepository)
    {
        _cityObjectRepository = cityObjectRepository;
        _reservationRepository = reservationRepository;
    }

    public CityObjectService() : this(
        new Database.Repositories.CityObjectDbRepository(),
        new Database.Repositories.CityObjectReservationDbRepository())
    { }

    public List<CityObject> GetAll()
    {
        return _cityObjectRepository.GetAll();
    }

    public List<DateTime> FindFreeSlots(long cityObjectId, int durationMinutes,
        DateTime rangeStart, DateTime rangeEnd)
    {
        var reservations = _reservationRepository.GetByCityObjectId(cityObjectId);
        var freeSlots = new List<DateTime>();

        DateTime current = rangeStart;
        while (current.AddMinutes(durationMinutes) <= rangeEnd)
        {
            DateTime slotEnd = current.AddMinutes(durationMinutes);
            bool overlaps = reservations.Any(r =>
                r.StartDatetime < slotEnd && r.EndDatetime > current);

            if (!overlaps)
                freeSlots.Add(current);

            current = current.AddHours(1);
        }

        return freeSlots;
    }

    public List<DateTime> FindFreeSlotsOutsideRange(long cityObjectId, int durationMinutes,
        DateTime rangeStart, DateTime rangeEnd)
    {
        var reservations = _reservationRepository.GetByCityObjectId(cityObjectId);
        var freeSlots = new List<DateTime>();

        DateTime searchStart = DateTime.Today;
        DateTime searchEnd = rangeStart.AddDays(-1);

        DateTime current = searchStart;
        while (current.AddMinutes(durationMinutes) <= searchEnd && freeSlots.Count < 10)
        {
            DateTime slotEnd = current.AddMinutes(durationMinutes);
            bool overlaps = reservations.Any(r =>
                r.StartDatetime < slotEnd && r.EndDatetime > current);

            if (!overlaps)
                freeSlots.Add(current);

            current = current.AddHours(1);
        }

        current = rangeEnd.AddDays(1);
        while (freeSlots.Count < 10)
        {
            DateTime slotEnd = current.AddMinutes(durationMinutes);
            bool overlaps = reservations.Any(r =>
                r.StartDatetime < slotEnd && r.EndDatetime > current);

            if (!overlaps)
                freeSlots.Add(current);

            current = current.AddHours(1);
        }

        return freeSlots;
    }

    public void Reserve(long cityObjectId, long neighborhoodId,
        DateTime startDatetime, int durationMinutes)
    {
        DateTime endDatetime = startDatetime.AddMinutes(durationMinutes);

        CityObjectReservation reservation = new CityObjectReservation(
            0, cityObjectId, neighborhoodId, startDatetime, endDatetime, durationMinutes);

        _reservationRepository.Create(reservation);
        _cityObjectRepository.ResetVotes(cityObjectId);
    }

    public List<CityObjectReservation> GetReservationsByNeighborhood(long neighborhoodId)
    {
        return _reservationRepository.GetByNeighborhoodId(neighborhoodId);
    }
}