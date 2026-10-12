using System.Windows;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow(long userId)
        {
            InitializeComponent();

            if (!UserSession.IsAuthenticated)
            {
                RedirectToLogin();
                return;
            }

            this.DataContext = new ProfileViewModel(userId, () => this.Close());
        }

        private void RedirectToLogin()
        {
            LogInForm loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }
    }
}