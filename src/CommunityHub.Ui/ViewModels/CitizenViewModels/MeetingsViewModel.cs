using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Ui.Views.CitizenViews;
using CommunityHub.Application;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class MeetingItemViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly MeetingService _meetingService;
        private readonly Action _onRefreshRequired;
        private DateTime? _uiSelectedDate;

        public Meeting Meeting { get; }
        public bool HasVoted { get; }
        public DateOnly? MyVote { get; }

        public string Topic => Meeting.Topic.ToString();
        public string Time => Meeting.Time.ToString("HH:mm");
        public string Status => Meeting.Status.ToString();

        public bool IsInPreparation => Meeting.Status == MeetingStatus.InPreparation;
        public bool IsScheduled => Meeting.Status == MeetingStatus.Scheduled;

        public bool CanVote => IsInPreparation &&
            DateTime.UtcNow < Meeting.StartDate.ToDateTime(TimeOnly.MinValue).AddHours(-24);

        public bool CannotVote => !CanVote;
        public bool HasNotVoted => !HasVoted;

        public string VotedDateDisplay => MyVote.HasValue
            ? MyVote.Value.ToString("dd. MM. yyyy.")
            : "Not voted yet";

        public DateTime? UISelectedDate
        {
            get => _uiSelectedDate;
            set
            {
                _uiSelectedDate = value;
                OnPropertyChanged();
            }
        }

        public ICommand VoteCommand { get; }
        public ICommand ChangeVoteCommand { get; }

        public MeetingItemViewModel(Meeting meeting, bool hasVoted, DateOnly? myVote, long userId, MeetingService meetingService, Action onRefreshRequired)
        {
            Meeting = meeting;
            HasVoted = hasVoted;
            MyVote = myVote;
            _userId = userId;
            _meetingService = meetingService;
            _onRefreshRequired = onRefreshRequired;

            if (myVote.HasValue)
            {
                _uiSelectedDate = myVote.Value.ToDateTime(TimeOnly.MinValue);
            }

            VoteCommand = new RelayCommand(o => ExecuteVote());
            ChangeVoteCommand = new RelayCommand(o => ExecuteChangeVote());
        }

        private void ExecuteVote()
        {
            if (!UISelectedDate.HasValue)
            {
                MessageBox.Show("Please select a date first.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly selectedDateOnly = DateOnly.FromDateTime(UISelectedDate.Value);
            string result = _meetingService.VoteForDate(Meeting.Id, _userId, selectedDateOnly, Meeting.StartDate);
            MessageBox.Show(result, "Voting Status", MessageBoxButton.OK, MessageBoxImage.Information);

            _onRefreshRequired?.Invoke();
        }

        private void ExecuteChangeVote()
        {
            if (!UISelectedDate.HasValue)
            {
                MessageBox.Show("Please select a new date first.", "Notification", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateOnly selectedDateOnly = DateOnly.FromDateTime(UISelectedDate.Value);
            string result = _meetingService.ChangeVote(Meeting.Id, _userId, selectedDateOnly, Meeting.StartDate);
            MessageBox.Show(result, "Voting Status", MessageBoxButton.OK, MessageBoxImage.Information);

            _onRefreshRequired?.Invoke();
        }
    }

    public class MeetingsViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly long _neighborhoodId;
        private readonly MeetingService _meetingService;
        private readonly NeighborhoodMembershipService _membershipService;
        private readonly Action _closeAction;

        public ObservableCollection<MeetingItemViewModel> Meetings { get; }
        public ICommand BackCommand { get; }

        public MeetingsViewModel(long userId, Action closeAction)
        {
            _userId = userId;
            _closeAction = closeAction;

            _membershipService = Injector.CreateInstance<NeighborhoodMembershipService>();
            _neighborhoodId = _membershipService.GetNeighborhoodId(_userId);
            _meetingService = Injector.CreateInstance<MeetingService>();

            Meetings = new ObservableCollection<MeetingItemViewModel>();
            BackCommand = new RelayCommand(o => ExecuteBack());

            LoadMeetings();
        }

        public void LoadMeetings()
        {
            var meetingsData = _meetingService.GetMeetingsForNeighborhood(_neighborhoodId);
            Meetings.Clear();

            foreach (var meeting in meetingsData)
            {
                var votes = _meetingService.GetVotesForMeeting(meeting.Id);
                var myVote = votes.FirstOrDefault(v => v.CitizenId == _userId);

                Meetings.Add(new MeetingItemViewModel(
                    meeting,
                    myVote != null,
                    myVote?.VotedDate,
                    _userId,
                    _meetingService,
                    LoadMeetings
                ));
            }
        }

        private void ExecuteBack()
        {
            CitizenHomeWindow home = new CitizenHomeWindow(_userId);
            home.Show();
            _closeAction?.Invoke();
        }
    }
}