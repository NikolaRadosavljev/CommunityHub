using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels;
using CommunityHub.Ui.ViewModels.CitizenViewModels;
using System.Windows;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class CitizenHomeWindow : Window
    {
        public CitizenHomeWindow(long userId)
        {
            InitializeComponent();
            if (!UserSession.IsAuthenticated)
            {
                var loginForm = new LogInForm();
                loginForm.Show();
                this.Close();
                return;
            }
            DataContext = new CitizenHomeViewModel(userId);
        }
    }
}