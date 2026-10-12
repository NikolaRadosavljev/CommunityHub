using CommunityHub.Ui.Commands;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views;
using CommunityHub.Ui.Views.CitizenViews;
using System.Windows;
using System.Windows.Input;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class CitizenHomeViewModel : ViewModelBase
    {
        private readonly long _userId;

        public ICommand NavigateMeetingsCommand { get; }
        public ICommand NavigateEventsCommand { get; }
        public ICommand NavigateNeighborhoodsCommand { get; }
        public ICommand NavigateProfileCommand { get; }
        public ICommand LogoutCommand { get; }

        public CitizenHomeViewModel(long userId)
        {
            _userId = userId;

            NavigateMeetingsCommand = new RelayCommand(o => NavigateTo(new MeetingsWindow(_userId)));
            NavigateEventsCommand = new RelayCommand(o => NavigateTo(new EventsWindow(_userId)));
            NavigateNeighborhoodsCommand = new RelayCommand(o => NavigateTo(new NeighborhoodsWindow(_userId)));
            NavigateProfileCommand = new RelayCommand(o => NavigateTo(new ProfileWindow(_userId)));
            LogoutCommand = new RelayCommand(o => ExecuteLogout());
        }

        private void NavigateTo(Window targetWindow)
        {
            targetWindow.Show();
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window != targetWindow && window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void ExecuteLogout()
        {
            UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window != loginForm && window.IsVisible)
                {
                    window.Close();
                }
            }
        }
    }
}