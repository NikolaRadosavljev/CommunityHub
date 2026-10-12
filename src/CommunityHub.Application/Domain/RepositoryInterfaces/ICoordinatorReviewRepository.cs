using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface ICoordinatorReviewRepository
{
    List<CoordinatorReview> GetByCoordinatorId(long coordinatorId);
    double GetAverageRatingLastYear(long coordinatorId);
}