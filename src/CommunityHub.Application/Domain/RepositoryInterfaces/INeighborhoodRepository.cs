using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface INeighborhoodRepository
{
    long Create(Neighborhood neighborhood);
    List<Neighborhood> Search(string? name, string? address, string? city, string? country);
}