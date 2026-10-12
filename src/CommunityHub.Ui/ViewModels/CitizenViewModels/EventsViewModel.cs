using CommunityHub.Application;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Ui.Views.CitizenViews;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class EventDisplayWrapper : ViewModelBase
    {
        private readonly long _currentUserId;
        public Event Event { get; }
        public bool IsFinished => Event.Status == EventStatus.Finished;
        public bool IsPreparation => Event.Status == EventStatus.Preparation;
        public bool CanRegisterVolunteer => Event.Status == EventStatus.Preparation;
        public bool IsOrganizerCurrentUser => Event.IsOrganizer(_currentUserId);
        public bool IsAlreadyRegistered => Event.Volunteers?.Any(v => v.CitizenId == _currentUserId) ?? false;
        public bool CanRegisterVolunteerForUser => CanRegisterVolunteer && !IsAlreadyRegistered;
        public bool ShowAttendanceSection => IsFinished && IsOrganizerCurrentUser;
        public List<VolunteerDisplay> VolunteerDisplays { get; } = new List<VolunteerDisplay>();

        public EventDisplayWrapper(Event evt, long currentUserId)
        {
            Event = evt;
            _currentUserId = currentUserId;

            if (Event.Volunteers != null && Event.Volunteers.Count > 0)
            {
                var userRepo = new UserDbRepository();
                foreach (var v in Event.Volunteers)
                {
                    if (v.CitizenId == Event.OrganizerId) continue;
                    string name = userRepo.GetFullNameById(v.CitizenId);
                    VolunteerDisplays.Add(new VolunteerDisplay(v, name));
                }
            }
        }
    }

    public class VolunteerDisplay
    {
        public EventVolunteer Volunteer { get; }
        public string FullName { get; }
        public VolunteerDisplay(EventVolunteer volunteer, string fullName)
        {
            Volunteer = volunteer;
            FullName = fullName;
        }
    }

    public class EventsViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly long _neighborhoodId;
        private readonly EventService _eventService;
        private readonly NeighborhoodMembershipService _membershipService;

        public ObservableCollection<EventDisplayWrapper> Events { get; set; }

        public ICommand BackCommand { get; }
        public ICommand CreateEventCommand { get; }
        public ICommand RegisterVolunteerCommand { get; }
        public ICommand RecordAttendedCommand { get; }
        public ICommand RecordNotAttendedCommand { get; }

        public EventsViewModel(long userId)
        {
            _userId = userId;
            _eventService = Injector.CreateInstance<EventService>();
            _membershipService = new NeighborhoodMembershipService();
            _neighborhoodId = _membershipService.GetNeighborhoodId(userId);
            Events = new ObservableCollection<EventDisplayWrapper>();

            BackCommand = new RelayCommand(o => ExecuteBack());
            CreateEventCommand = new RelayCommand(o => ExecuteCreateEvent());
            RegisterVolunteerCommand = new RelayCommand(o => ExecuteRegisterVolunteer(o));
            RecordAttendedCommand = new RelayCommand(o => ExecuteRecordAttendance(o, true));
            RecordNotAttendedCommand = new RelayCommand(o => ExecuteRecordAttendance(o, false));

            LoadEvents();
        }

        public void LoadEvents()
        {
            _eventService.UpdateAllStatuses();
            var events = _eventService.GetEventsByNeighborhood(_neighborhoodId);
            Events.Clear();
            foreach (var e in events)
            {
                Events.Add(new EventDisplayWrapper(e, _userId));
            }
        }

        private void ExecuteBack()
        {
            var home = new CitizenHomeWindow(_userId);
            home.Show();
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window != home && window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void ExecuteCreateEvent()
        {
            var target = new CreateEventWindow(_userId, _neighborhoodId);
            target.ShowDialog();
            LoadEvents();
        }

        private void ExecuteRegisterVolunteer(object param)
        {
            if (param is Event ev)
            {
                if (_eventService.IsAlreadyRegistered(ev.Id, _userId))
                {
                    MessageBox.Show("You are already registered for this event.");
                    return;
                }
                var target = new RegisterVolunteerWindow(_userId, ev);
                target.ShowDialog();
                LoadEvents();
            }
        }

        private void ExecuteRecordAttendance(object param, bool attended)
        {
            if (param is EventVolunteer volunteer)
            {
                var ev = _eventService.GetEventsByNeighborhood(_neighborhoodId).FirstOrDefault(e => e.Id == volunteer.EventId);
                if (ev == null || !ev.IsOrganizer(_userId)) return;

                string result = _eventService.RecordAttendance(volunteer, attended);
                MessageBox.Show(result);
                LoadEvents();
            }
        }
    }
}