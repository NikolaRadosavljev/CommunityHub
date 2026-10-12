using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class EventService
{
    private readonly IEventRepository _eventRepository;

    public EventService(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public void UpdateAllStatuses()
    {
        var events = _eventRepository.GetAllPendingStatusUpdate();
        foreach (var ev in events)
            UpdateStatus(ev);
    }

    public bool IsAlreadyRegistered(long eventId, long citizenId)
    {
        if(_eventRepository.IsAlreadyRegistered(eventId, citizenId))
        {
            return true;
        }

        return false;
    }

    public void CreateEvent(Event ev, List<string> itemNames)
    {
        _eventRepository.Create(ev, itemNames);
    }

    public string RegisterVolunteer(Event ev, long citizenId, List<long> selectedItemIds)
    {
        long volunteerId = _eventRepository.CreateVolunteer(ev.Id, citizenId);
        foreach (var itemId in selectedItemIds)
            _eventRepository.AddVolunteerItem(volunteerId, itemId);

        var updatedEvent = _eventRepository.GetByNeighborhood(ev.NeighborhoodId)
            .First(e => e.Id == ev.Id);
        UpdateStatus(updatedEvent);

        return "You registered as a volunteer.";
    }

    public string RecordAttendance(EventVolunteer volunteer, bool attended)
    {
        _eventRepository.RecordAttendance(volunteer.Id, attended);
        return attended ? "Attendance recorded." : "Absence recorded.";
    }

    private void UpdateStatus(Event ev)
    {
        var newStatus = CalculateStatus(ev);
        if (newStatus != ev.Status)
            _eventRepository.UpdateStatus(ev.Id, newStatus);
    }

    private EventStatus CalculateStatus(Event ev)
    {
        if (ev.EventDateTime < DateTime.Now)
            return EventStatus.Finished;

        bool twelveHoursBefore = ev.EventDateTime - DateTime.Now < TimeSpan.FromHours(12);
        if (twelveHoursBefore)
        {
            bool readyToSchedule = ev.CurrentVolunteerCount >= ev.MinVolunteers && ev.AllItemsCovered;
            return readyToSchedule ? EventStatus.Scheduled : EventStatus.Cancelled;
        }

        if (ev.CurrentVolunteerCount >= ev.MinVolunteers && ev.AllItemsCovered)
            return EventStatus.Scheduled;

        return EventStatus.Preparation;
    }

    public List<Event> GetEventsByNeighborhood(long neighborhoodId)
    {
        return _eventRepository.GetByNeighborhood(neighborhoodId);
    }
}