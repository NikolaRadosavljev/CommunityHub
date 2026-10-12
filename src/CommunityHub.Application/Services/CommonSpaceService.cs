using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Database.Repositories;

namespace CommunityHub.Application.Services;

public class CommonSpaceService : ICommonSpaceService
{
    private readonly ICommonSpaceRepository _repository;
    private readonly IBuildingRepository _buildingRepository;

    public CommonSpaceService(ICommonSpaceRepository repository, IBuildingRepository buildingRepository)
    {
        _repository = repository;
        _buildingRepository = buildingRepository;
    }

    public CommonSpace CreateCommonSpace(int buildingId, string name, string description, int floorNumber, CommonSpaceRentalType rentalType)
    {
        var building = _buildingRepository.GetById(buildingId);
        if (building == null)
        {
            throw new Exception("Building not found.");
        }

        if (floorNumber > building.FloorNumber || floorNumber < 0)
        {
            throw new Exception($"Invalid floor number. The building has {building.FloorNumber} floors.");
        }

        return _repository.Create(buildingId, name, description, floorNumber, rentalType);
    }

    public List<CommonSpace> GetCommonSpacesForBuilding(int buildingId)
    {
        return _repository.GetCommonSpacesForBuilding(buildingId);
    }

    // Vraca odobrene zahteve za datu prostoriju (koristi se za kalendar)
    public List<CommonSpaceRentalRequest> GetApprovedRequestsForCommonSpace(int commonSpaceId)
    {
        var repo = new CommonSpaceDbRepository();
        return repo.GetApprovedRequestsByCommonSpaceId(commonSpaceId);
    }
}
