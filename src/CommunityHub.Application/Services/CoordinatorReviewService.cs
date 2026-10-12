using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class CoordinatorReviewService
{
    private readonly ICoordinatorReviewRepository _reviewRepository;

    public CoordinatorReviewService(ICoordinatorReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public CoordinatorReviewService() : this(
        new Database.Repositories.CoordinatorReviewDbRepository())
    { }

    public List<CoordinatorReview> GetReviews(long coordinatorId)
    {
        return _reviewRepository.GetByCoordinatorId(coordinatorId);
    }

    public double GetAverageRatingLastYear(long coordinatorId)
    {
        return _reviewRepository.GetAverageRatingLastYear(coordinatorId);
    }
}