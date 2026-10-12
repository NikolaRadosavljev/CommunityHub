using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ICityObjectRepository
{
    List<CityObject> GetAll();
    void ResetVotes(long cityObjectId);
}