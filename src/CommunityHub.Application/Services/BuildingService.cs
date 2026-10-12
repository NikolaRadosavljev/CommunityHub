using System.Collections.Generic;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.DTO;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Services;

public class BuildingService : IBuildingService
{
    private readonly IBuildingRepository _buildingRepository;

    public BuildingService(IBuildingRepository buildingRepository)
    {
        _buildingRepository = buildingRepository;
    }

    public List<Building> Search(string? address, string? settlement, string? city, string? country)
    {
        return _buildingRepository.Search(address, settlement, city, country);
    }

    public BuildingRegistrationDTO Register(BuildingRegistrationRequestDTO request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string address = request.Address.Trim();
        string settlement = request.Settlement.Trim();
        string city = request.City.Trim();
        string country = request.Country.Trim();

        if (string.IsNullOrWhiteSpace(address))
        {
            return Fail("Address is required.");
        }

        if (string.IsNullOrWhiteSpace(settlement))
        {
            return Fail("Settlement is required.");
        }

        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
        {
            return Fail("City and country are required.");
        }

        if (request.FloorCount <= 0)
        {
            return Fail("Number of floors must be an integer greater than 0.");
        }

        if (request.PicturePaths is null || request.PicturePaths.Count == 0)
        {
            return Fail("At least one picture must be selected.");
        }

        var parseResult = ParseFlatsByFloor(request.FlatsByFloorRaw, request.FloorCount);
        if (!parseResult.IsSuccess)
        {
            return Fail(parseResult.Error!);
        }

        List<Flat> parsedFlats = parseResult.Flats!;
        List<int> flatNumbers = parseResult.FlatNumbers!;

        int managerId;
        try
        {
            managerId = checked((int)request.ManagerId);
        }
        catch (OverflowException)
        {
            return Fail("Manager identifier is not valid.");
        }

        Building building = new Building(
            0,
            address,
            settlement,
            city,
            country,
            request.FloorCount,
            flatNumbers,
            [.. request.PicturePaths],
            managerId);

        Building createdBuilding = _buildingRepository.Create(building, parsedFlats);

        return new BuildingRegistrationDTO
        {
            IsSuccess = true,
            Building = createdBuilding,
            Message = string.Empty
        };
    }

    private static BuildingRegistrationDTO Fail(string message)
    {
        return new BuildingRegistrationDTO
        {
            IsSuccess = false,
            Message = message,
            Building = null
        };
    }

    private static (bool IsSuccess, List<Flat>? Flats, List<int>? FlatNumbers, string? Error) ParseFlatsByFloor(string rawValue, int floorCount)
    {
        var parser = new FlatParser(floorCount);
        return parser.Parse(rawValue);
    }

    private class FlatParser
    {
        private readonly int _floorCount;
        private readonly List<Flat> _flats = [];
        private readonly List<int> _flatNumbers = [];
        private readonly HashSet<int> _usedFloors = [];
        private readonly HashSet<int> _allFlatNumbers = [];

        public FlatParser(int floorCount)
        {
            _floorCount = floorCount;
        }

        public (bool IsSuccess, List<Flat>? Flats, List<int>? FlatNumbers, string? Error) Parse(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return (false, null, null, "You must enter floors and flats for each line.");
            }

            string[] lines = rawValue.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length != _floorCount)
            {
                return (false, null, null, "You must enter exactly one line for each floor.");
            }

            foreach (string line in lines)
            {
                var result = ParseLine(line);
                if (!result.IsSuccess)
                {
                    return (false, null, null, result.Error);
                }
            }

            if (_usedFloors.Count != _floorCount)
            {
                return (false, null, null, "Not all floors were entered.");
            }

            return (true, _flats, _flatNumbers, null);
        }

        private (bool IsSuccess, string? Error) ParseLine(string line)
        {
            string[] parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return (false, $"Invalid line format '{line}'. Use 'floor: flats'.");
            }

            if (!int.TryParse(parts[0], out int floorNumber) || floorNumber <= 0 || floorNumber > _floorCount)
            {
                return (false, $"Floor '{parts[0]}' is not valid. Allowed range is 1-{_floorCount}.");
            }

            if (!_usedFloors.Add(floorNumber))
            {
                return (false, $"Floor {floorNumber} was entered multiple times.");
            }

            string[] apartmentParts = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (apartmentParts.Length == 0)
            {
                return (false, $"Floor {floorNumber} must have at least one flat.");
            }

            return ParseFlats(apartmentParts, floorNumber);
        }

        private (bool IsSuccess, string? Error) ParseFlats(string[] apartmentParts, int floorNumber)
        {
            HashSet<int> floorFlatNumbers = [];
            foreach (string apartment in apartmentParts)
            {
                var flatResult = ParseApartment(apartment, floorNumber, floorFlatNumbers);
                if (!flatResult.IsSuccess)
                {
                    return flatResult;
                }
            }
            return (true, null);
        }

        private (bool IsSuccess, string? Error) ParseApartment(string apartment, int floorNumber, HashSet<int> floorFlatNumbers)
        {
            if (!int.TryParse(apartment, out int flatNumber) || flatNumber <= 0)
            {
                return (false, $"Invalid flat number '{apartment}' on floor {floorNumber}.");
            }

            if (!floorFlatNumbers.Add(flatNumber))
            {
                return (false, $"Flat {flatNumber} was entered multiple times on floor {floorNumber}.");
            }

            if (!_allFlatNumbers.Add(flatNumber))
            {
                return (false, $"Flat {flatNumber} was entered multiple times in the building.");
            }

            _flats.Add(new Flat(0, 0, floorNumber, flatNumber));
            _flatNumbers.Add(flatNumber);
            return (true, null);
        }
    }

    public Building? GetById(int buildingId)
    {
        return _buildingRepository.GetById(buildingId);
    }

    public int GetOccupancyCount(int buildingId)
    {
        return _buildingRepository.GetOccupancyCount(buildingId);
    }
}
