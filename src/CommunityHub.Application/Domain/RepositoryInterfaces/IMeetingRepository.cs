using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IMeetingRepository
{
    long Create(Meeting meeting);
    void UpdateStatus(long meetingId, MeetingStatus status, DateOnly? scheduledDate);
    List<Meeting> GetByNeighborhoodId(long neighborhoodId);
    List<Meeting> GetByCoordinatorId(long coordinatorId);
    List<Meeting> GetInPreparationWithStartDateTomorrow();
}