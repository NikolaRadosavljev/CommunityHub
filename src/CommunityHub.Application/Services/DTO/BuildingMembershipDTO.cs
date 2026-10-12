using System;

namespace CommunityHub.Application.Services;

public class BuildingMembershipDTO
{
    public long Id { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string BuildingAddress { get; init; } = string.Empty;
    public int FloorNumber { get; init; }
    public int FlatNumber { get; init; }
    public DateTime JoinedAt { get; init; }
}