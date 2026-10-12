using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IStreetRepository
{
    Street Create(Street street, long neighborhoodId);
}