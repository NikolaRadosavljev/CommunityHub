using CommunityHub.Application.Domain;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views.CoordinatorViews;
using System.Windows;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class HomeWindow : Window
{
    private readonly long _userId;

    public HomeWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;

        if (!UserSession.IsAuthenticated)
        {
            RedirectToLogin();
            return;
        }

        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može pristupiti ovoj početnoj stranici.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            RedirectToLogin();
        }
    }

    private void CreateNeighborhoodButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može registrovati kvart.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreateNeighborhoodWindow window = new CreateNeighborhoodWindow(_userId);
        window.Show();
        this.Close();
    }

    private void NeighborhoodRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može upravljati zahtevima.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NeighborhoodMembershipRequestsWindow window = new NeighborhoodMembershipRequestsWindow(_userId);
        window.Show();
        this.Close();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        UserSession.SignOut();
        RedirectToLogin();
    }

    private void RedirectToLogin()
    {
        LogInForm loginForm = new LogInForm();
        loginForm.Show();
        this.Close();
    }

    private void CitizenStatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo koordinator može pristupiti ovoj stranici.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CitizenStatisticsWindow window = new CitizenStatisticsWindow(_userId);
        window.Show();
        this.Close();
    }

    private void ForumsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo koordinator može pristupiti forumima.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ForumsWindow window = new ForumsWindow(_userId);
        window.Show();
        this.Close();
    }

    private void CityObjectsButton_Click(object sender, RoutedEventArgs e)
    {
        CityObjectsWindow window = new CityObjectsWindow(_userId);
        window.Show();
        this.Close();
    }

    private void ReviewsButton_Click(object sender, RoutedEventArgs e)
    {
        CoordinatorReviewsWindow window = new CoordinatorReviewsWindow(_userId);
        window.Show();
        this.Close();
    }

    private void BudgetButton_Click(object sender, RoutedEventArgs e)
    {
        BudgetWindow window = new BudgetWindow(_userId);
        window.Show();
        this.Close();
    }

    private void CityGiftButton_Click(object sender, RoutedEventArgs e)
    {
        CityGiftWindow window = new CityGiftWindow(_userId);
        window.Show();
        this.Close();
    }
}