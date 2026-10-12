using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public class BuildingRequestDTO
{
    public long Id { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Settlement { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int FlatNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public RequestStatus Status { get; init; }
    public string? RejectionExplanation { get; init; }
    public int BuildingId { get; init; }
}
