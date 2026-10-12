using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class NeighborhoodService
{
    private readonly INeighborhoodRepository _neighborhoodRepository;
    private readonly IStreetRepository _streetRepository;

    public NeighborhoodService(INeighborhoodRepository neighborhoodRepository,
                                IStreetRepository streetRepository)
    {
        _neighborhoodRepository = neighborhoodRepository;
        _streetRepository = streetRepository;
    }

    public long Create(long coordinatorId, string name, string description, City city, List<string> images, List<Street> streets)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Neighborhood name is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Neighborhood description is required.");

        if (city == null)
            throw new ArgumentException("City is required.");

        Neighborhood neighborhood = new Neighborhood(0, coordinatorId, name, description);
        neighborhood.City = city;

        foreach (string image in images)
            neighborhood.AddImage(image);

        foreach (Street street in streets)
            neighborhood.AddStreet(street);

        long neighborhoodId = _neighborhoodRepository.Create(neighborhood);

        foreach (Street street in streets)
            _streetRepository.Create(street, neighborhoodId);

        return neighborhoodId;
    }

    public NeighborhoodService() : this(
    new NeighborhoodDbRepository(),
    new StreetDbRepository())
    { }

    public List<Neighborhood> GetByCoordinatorId(long coordinatorId)
    {
        return _neighborhoodRepository.Search(null, null, null, null)
            .Where(n => n.CoordinatorId == coordinatorId)
            .ToList();
    }

    public List<Neighborhood> Search(string? name, string? city, string? street, string? description)
    {
        return _neighborhoodRepository.Search(name, city, street, description);
    }
}