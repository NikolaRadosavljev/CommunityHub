using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views.CitizenViews;
using CommunityHub.Ui.Views.TenantViews;
using CommunityHub.Ui.Views.CoordinatorViews;

namespace CommunityHub.Ui.Views;

public partial class LogInForm : Window
{
    private readonly UserDbRepository _userRepository;

    public LogInForm()
    {
        InitializeComponent();
        _userRepository = new UserDbRepository();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordBox.Password;

        try
        {
            User? user = _userRepository.GetByCredentials(username, password);

            if (user != null)
            {
                UserSession.SignIn(user);
                OpenStartWindowForRole(user);
                this.Close();
            }
            else
            {
                ErrorMessageTextBlock.Text = "Neispravno korisničko ime ili lozinka.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessageTextBlock.Text = $"Greška pri povezivanju s bazom:\n{ex.Message}";
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }
    }

    private static void OpenStartWindowForRole(User user)
    {
        Window startWindow = user.Role switch
        {
            UserRole.Cordinator => new HomeWindow(user.Id),
            UserRole.Manager => new ManagerMainWindow(user.Id),
            UserRole.Citizen => new CitizenHomeWindow(user.Id),
            UserRole.Tenant => new TenantWindow(user.Id),
            _ => throw new System.InvalidOperationException($"Nepoznata uloga: {user.Role}")
        };

        System.Windows.Application.Current.MainWindow = startWindow;
        startWindow.Show();
    }
}