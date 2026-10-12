using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public class BuildingRegistrationDTO
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public Building? Building { get; init; }
}
