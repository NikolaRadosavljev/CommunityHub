namespace CommunityHub.Application.Services;

public class BuildingRegistrationRequestDTO
{
    public long ManagerId { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Settlement { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int FloorCount { get; init; }
    public string FlatsByFloorRaw { get; init; } = string.Empty;
    public List<string> PicturePaths { get; init; } = [];
}
