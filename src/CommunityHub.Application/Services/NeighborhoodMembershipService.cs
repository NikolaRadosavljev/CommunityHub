using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

public class NeighborhoodMembershipService
{
    private readonly CitizenDbRepository _citizenRepository;
    private readonly INeighborhoodMembershipRepository _membershipRepository;
    private readonly INeighborhoodMembershipRequestRepository _requestRepository;

    public NeighborhoodMembershipService(
        CitizenDbRepository citizenRepository,
        INeighborhoodMembershipRepository membershipRepository,
        INeighborhoodMembershipRequestRepository requestRepository)
    {
        _citizenRepository = citizenRepository;
        _membershipRepository = membershipRepository;
        _requestRepository = requestRepository;
    }

    public NeighborhoodMembershipService() : this(
    new CitizenDbRepository(),
    new NeighborhoodMembershipDbRepository(),
    new NeighborhoodMembershipRequestDbRepository())
    { }

    public string RequestAccess(long userId, Neighborhood neighborhood)
    {
        if (_membershipRepository.HasMembership(userId))
            return "You are already a member of a neighborhood.";

        if (_requestRepository.HasPendingRequest(userId, neighborhood.Id))
            return "You already have a pending request for this neighborhood.";

        Citizen? citizen = _citizenRepository.GetByUserId(userId);
        if (citizen == null)
            return "Citizen not found.";

        if (AddressMatchesNeighborhood(citizen.Address, neighborhood.Streets))
        {
            _membershipRepository.Create(neighborhood.Id, userId);
            return "You automatically became a member of the neighborhood based on your address.";
        }

        _requestRepository.Create(neighborhood.Id, userId);
        return "Access request has been submitted and is awaiting approval.";
    }

    private bool AddressMatchesNeighborhood(string address, List<Street>? streets)
    {
        if (streets == null) return false;

        var parts = address.Trim().Split(' ');
        if (!int.TryParse(parts.Last(), out int streetNumber)) return false;

        string streetName = string.Join(" ", parts.Take(parts.Length - 1));

        return streets.Any(s =>
            s.Name.Equals(streetName, StringComparison.OrdinalIgnoreCase) &&
            streetNumber >= s.StartNumber &&
            streetNumber <= s.EndNumber);
    }

    public long GetNeighborhoodId(long userId)
    {
        return _membershipRepository.GetNeighborhoodIdByUserId(userId)
            ?? throw new InvalidOperationException("User does not belong to any neighborhood.");
    }
}