using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public class BuildingDTO
{
    public long Id { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Settlement { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Location => $"{City}, {Country}";
    public int FloorNumber { get; init; }
    public int TotalFlats { get; init; }
    public string FlatsRange { get; init; } = string.Empty;
    public string? MainPicture { get; init; }
    public List<int>? FlatNumbers { get; set; }
}
