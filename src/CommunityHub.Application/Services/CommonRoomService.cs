using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.DTO;

namespace CommunityHub.Application.Services;

public class CommonRoomService
{
    private readonly CommonRoomDbRepository _commonRoomRepository;

    public CommonRoomService()
    {
        _commonRoomRepository = new CommonRoomDbRepository();
    }

    public List<CommonRoomDTO> GetCommonRoomsForBuilding(int buildingId)
    {
        if (buildingId == 0)
        {
            throw new ArgumentException("Building ID cannot be 0.", nameof(buildingId));
        }

        List<CommonRoom> rooms = _commonRoomRepository.GetAllByBuildingId(buildingId);

        return rooms.Select(room => new CommonRoomDTO
        {
            Id = room.Id,
            BuildingId = room.BuildingId,
            Name = room.Name,
            Description = room.Description,
            Floor = room.Floor,
            RentalType = room.RentalType
        }).ToList();
    }

    public CommonRoomDTO? GetCommonRoomById(int commonRoomId)
    {
        if (commonRoomId == 0)
        {
            throw new ArgumentException("Common room ID cannot be 0.", nameof(commonRoomId));
        }

        CommonRoom? room = _commonRoomRepository.GetById(commonRoomId);

        if (room == null)
        {
            return null;
        }

        return new CommonRoomDTO
        {
            Id = room.Id,
            BuildingId = room.BuildingId,
            Name = room.Name,
            Description = room.Description,
            Floor = room.Floor,
            RentalType = room.RentalType
        };
    }
}
