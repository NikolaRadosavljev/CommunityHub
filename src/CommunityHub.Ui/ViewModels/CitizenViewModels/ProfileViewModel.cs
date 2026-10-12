using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Commands;
using CommunityHub.Ui.Views;
using CommunityHub.Ui.Views.CitizenViews;
using CommunityHub.Ui.Views.TenantViews;
using System;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Ui.Views.CoordinatorViews;
using CommunityHub.Application;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly UserDbRepository _userRepository;
        private readonly CitizenDbRepository _citizenRepository;
        private readonly TrustRecordService _trustRecordService;
        private readonly Action _closeAction;

        private User _user;
        private Citizen? _citizen;

        private string _fullName = "Loading User...";
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _address = "No Address Provided";
        public string Address { get => _address; set { _address = value; OnPropertyChanged(); } }

        private string _trustLevelName = "Not a member of any neighborhood.";
        public string TrustLevelName { get => _trustLevelName; set { _trustLevelName = value; OnPropertyChanged(); } }

        private string _organizedCount = "0";
        public string OrganizedCount { get => _organizedCount; set { _organizedCount = value; OnPropertyChanged(); } }

        private string _volunteeredCount = "0";
        public string VolunteeredCount { get => _volunteeredCount; set { _volunteeredCount = value; OnPropertyChanged(); } }

        private string _joinedDate = "--.--.----.";
        public string JoinedDate { get => _joinedDate; set { _joinedDate = value; OnPropertyChanged(); } }

        public ICommand HomeCommand { get; }
        public ICommand SeeMembersCommand { get; }
        public ICommand MyEventsCommand { get; }

        public ProfileViewModel(long userId, Action closeAction)
        {
            _userId = userId;
            _closeAction = closeAction;
            _userRepository = new UserDbRepository();
            _citizenRepository = new CitizenDbRepository();
            _trustRecordService = Injector.CreateInstance<TrustRecordService>();

            HomeCommand = new RelayCommand(o => ExecuteHome());
            SeeMembersCommand = new RelayCommand(o => ExecuteSeeMembers());
            MyEventsCommand = new RelayCommand(o => ExecuteMyEvents());

            LoadProfileData();
        }

        private void LoadProfileData()
        {
            User? user = _userRepository.GetWithPosts(_userId);
            if (user == null)
            {
                MessageBox.Show("User data could not be recovered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _closeAction?.Invoke();
                return;
            }

            _user = user;
            FullName = $"{_user.Name} {_user.Surname}";

            _citizen = _citizenRepository.GetByUserId(_userId);
            if (_citizen != null)
            {
                Address = _citizen.Address;
            }

            var record = _trustRecordService.GetForCurrentUser(_userId);
            if (record != null)
            {
                TrustLevelName = record.LevelDisplayName;
                OrganizedCount = record.OrganizedEvents.ToString();
                VolunteeredCount = record.VolunteeredEvents.ToString();
                JoinedDate = record.JoinedAt.ToString("dd. MM. yyyy.");
            }
        }

        private void ExecuteSeeMembers()
        {
            try
            {
                var membershipService = new NeighborhoodMembershipService();
                long neighborhoodId = membershipService.GetNeighborhoodId(_userId);

                var trustRecordsWindow = new NeighborhoodTrustRecordsWindow(neighborhoodId);
                trustRecordsWindow.ShowDialog();
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("You are not registered as a member of any neighborhood.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching window view: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteMyEvents()
        {
            var myEventsWindow = new EventsWindow(_userId);
            myEventsWindow.Show();
            _closeAction?.Invoke();
        }

        private void ExecuteHome()
        {
            Window startWindow = _user.Role switch
            {
                UserRole.Cordinator => new HomeWindow(_user.Id),
                UserRole.Manager => new ManagerMainWindow(_user.Id),
                UserRole.Citizen => new CitizenHomeWindow(_user.Id),
                UserRole.Tenant => new TenantWindow(_user.Id),
                _ => new CitizenHomeWindow(_user.Id)
            };

            startWindow.Show();
            _closeAction?.Invoke();
        }
    }
}