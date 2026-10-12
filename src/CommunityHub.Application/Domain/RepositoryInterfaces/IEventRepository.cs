using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IEventRepository
{
    List<Event> GetByNeighborhood(long neighborhoodId);
    bool IsAlreadyRegistered(long eventId, long citizenId);
    void Create(Event ev, List<string> itemNames);
    long CreateVolunteer(long eventId, long citizenId);
    void AddVolunteerItem(long volunteerId, long itemId);
    void UpdateStatus(long eventId, EventStatus status);
    void RecordAttendance(long volunteerId, bool attended);
    List<Event> GetAllPendingStatusUpdate();
}