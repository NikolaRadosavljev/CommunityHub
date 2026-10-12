using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Services;

public interface ICommonSpaceRentalRequestService
{
    List<CommonSpaceRentalRequest> GetAllRequests(List<int> buildingIds);
    List<CommonSpaceRentalRequest> GetPendingRequests(List<int> buildingIds);
    CommonSpaceRentalRequest? GetRequestById(int requestId);
    CommonSpaceRentalRequest CreateRequest(int commonSpaceId, int tenantId, DateTime startDate, DateTime endDate);
    void ProcessNewRequest(CommonSpaceRentalRequest request);
    void AcceptDateChange(int requestId);
    void ApproveRequest(CommonSpaceRentalRequest request);
    void RejectRequest(CommonSpaceRentalRequest request);
    void SuggestDatesForMultipleDaysRental(CommonSpaceRentalRequest request, DateTime start, DateTime end);
    void SelectDayForSingleDayRental(CommonSpaceRentalRequest request, DateTime selectedDay);
    List<(DateTime Start, DateTime End)> GetFreeRangesForRequest(int requestId);
    List<DateTime> GetFreeDaysForRequest(int requestId);
}
