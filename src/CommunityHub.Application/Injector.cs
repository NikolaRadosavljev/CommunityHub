using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.Interfaces;
using CommunityHub.Application.Services;

namespace CommunityHub.Application;

public class Injector
{
    private static readonly Dictionary<Type, object> _implementations = new()
    {
        // Repositories
        { typeof(INeighborhoodRepository), new NeighborhoodDbRepository() },
        { typeof(INeighborhoodMembershipRepository), new NeighborhoodMembershipDbRepository() },
        { typeof(INeighborhoodMembershipRequestRepository), new NeighborhoodMembershipRequestDbRepository() },
        { typeof(IMeetingRepository), new MeetingDbRepository() },
        { typeof(IMeetingDateVoteRepository), new MeetingDateVoteDbRepository() },
        { typeof(IForumRepository), new ForumDbRepository() },
        { typeof(IForumCommentRepository), new ForumCommentDbRepository() },
        { typeof(IForumReactionRepository), new ForumReactionDbRepository() },
        { typeof(ICitizenTrustRecordRepository), new CitizenTrustRecordDbRepository() },
        { typeof(IStreetRepository), new StreetDbRepository() },
        { typeof(IEventRepository), new EventDbRepository() },
        { typeof(ITrustRecordRepository), new TrustRecordDbRepository() },

        // Services
        { typeof(NeighborhoodService), new NeighborhoodService(
            new NeighborhoodDbRepository(),
            new StreetDbRepository()) },

        { typeof(NeighborhoodMembershipService), new NeighborhoodMembershipService(
            new CitizenDbRepository(),
            new NeighborhoodMembershipDbRepository(),
            new NeighborhoodMembershipRequestDbRepository()) },

        { typeof(NeighborhoodMembershipRequestService), new NeighborhoodMembershipRequestService(
            new NeighborhoodMembershipRequestDbRepository(),
            new NeighborhoodMembershipDbRepository()) },

        { typeof(MeetingService), new MeetingService(
            new MeetingDbRepository(),
            new MeetingDateVoteDbRepository()) },

        { typeof(TrustRecordService), new TrustRecordService(
            new TrustRecordDbRepository(),
            new NeighborhoodMembershipDbRepository()) },

        { typeof(EventService), new EventService(
            new EventDbRepository()) },

        { typeof(CitizenTrustService), new CitizenTrustService(
            new CitizenTrustRecordDbRepository(),
            new NeighborhoodDbRepository()) },

        { typeof(ForumService), new ForumService(
            new ForumDbRepository(),
            new ForumCommentDbRepository(),
            new ForumReactionDbRepository(),
            new NeighborhoodDbRepository()) },

        { typeof(CityObjectService), new CityObjectService(
    new CityObjectDbRepository(),
    new CityObjectReservationDbRepository()) },

{ typeof(CoordinatorReviewService), new CoordinatorReviewService(
    new CoordinatorReviewDbRepository()) },

{ typeof(BudgetService), new BudgetService(
    new BudgetCategoryDbRepository(),
    new DonationDbRepository(),
    new ExpenseDbRepository(),
    new NeighborhoodDbRepository()) },

{ typeof(CityGiftService), new CityGiftService(
    new CityGiftDbRepository(),
    new BudgetCategoryDbRepository(),
    new NeighborhoodDbRepository()) },
    };

    public static T CreateInstance<T>()
    {
        Type type = typeof(T);
        if (_implementations.ContainsKey(type))
            return (T)_implementations[type];

        throw new ArgumentException($"No implementation found for type {type}");
    }
}