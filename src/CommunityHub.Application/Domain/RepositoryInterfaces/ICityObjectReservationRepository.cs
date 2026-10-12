using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ICityObjectReservationRepository
{
    long Create(CityObjectReservation reservation);
    List<CityObjectReservation> GetByCityObjectId(long cityObjectId);
    List<CityObjectReservation> GetByNeighborhoodId(long neighborhoodId);
}