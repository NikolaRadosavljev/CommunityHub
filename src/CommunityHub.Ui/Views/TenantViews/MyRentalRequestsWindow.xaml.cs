using System.Windows;
using CommunityHub.Application.Services;
using CommunityHub.Ui.ViewModels;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews;

public partial class MyRentalRequestsWindow : Window
{
    private readonly long _tenantId;
    private readonly int _buildingId;
    private RentalRequestsViewModel _viewModel;

    public MyRentalRequestsWindow(long tenantId, int buildingId)
    {
        InitializeComponent();
        _tenantId = tenantId;
        _buildingId = buildingId;
        _viewModel = new RentalRequestsViewModel(tenantId, buildingId);

        Loaded += MyRentalRequestsWindow_Loaded;
    }

    private void MyRentalRequestsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadRequests();
    }

    private void AcceptDatesButton_Click(object sender, RoutedEventArgs e)
    {
        // Use the selected request ID
        if (_selectedRequestId == 0)
        {
            MessageBox.Show("Please select a request first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            _viewModel.AcceptSuggestedDates(_selectedRequestId);

            // Učitaj ponovo iz baze da vidiš novi status
            LoadRequests();

            // Prikaži odgovarajuću poruku na osnovu novog statusa
            var updatedRequest = _viewModel.MyRequests.FirstOrDefault(r => r.Id == _selectedRequestId);
            if (updatedRequest != null)
            {
                if (updatedRequest.Status == "Approved")
                {
                    MessageBox.Show("Great! Your suggested dates have been accepted and approved. You can now use the common space.",
                        "Request Approved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (updatedRequest.Status == "Pending")
                {
                    MessageBox.Show("Your suggested dates were not available. The request is pending manager review. They will suggest alternative dates soon.",
                        "Request Pending", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            _selectedRequestId = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error accepting suggested dates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadRequests()
    {
        try
        {
            _viewModel.LoadMyRequests();
            RequestsListView.ItemsSource = _viewModel.MyRequests;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading requests: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private int _selectedRequestId = 0;
    private System.Windows.Controls.Border? _previouslySelectedBorder = null;

    private void RequestItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Track which request was selected
        if (sender is System.Windows.Controls.Border border && border.Tag is int requestId)
        {
            // If clicking the same item again, deselect it
            if (_selectedRequestId == requestId)
            {
                _selectedRequestId = 0;
                // Reset to original style
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
                border.BorderThickness = new Thickness(1);
                _previouslySelectedBorder = null;
            }
            else
            {
                // Deselect previous item if there was one
                if (_previouslySelectedBorder != null)
                {
                    _previouslySelectedBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
                    _previouslySelectedBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
                    _previouslySelectedBorder.BorderThickness = new Thickness(1);
                }

                // Select new item
                _selectedRequestId = requestId;
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 248, 255));
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 34, 34));
                border.BorderThickness = new Thickness(2);
                _previouslySelectedBorder = border;
            }
        }
    }

    private void CancelRequestButton_Click(object sender, RoutedEventArgs e)
    {
        // Use the selected request ID
        if (_selectedRequestId == 0)
        {
            MessageBox.Show("Please select a request first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            "Are you sure you want to withdraw this request?",
            "Confirm Withdrawal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _viewModel.CancelRequest(_selectedRequestId);
                MessageBox.Show("Request withdrawn successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _selectedRequestId = 0;
                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error withdrawing request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var rentalWindow = new RentalRequestsWindow(_tenantId, _buildingId);
        rentalWindow.Show();
        this.Close();
    }

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string tag)
        {
            switch (tag)
            {
                case "Home":
                    var homeWindow = new TenantWindow(_tenantId);
                    homeWindow.Show();
                    this.Close();
                    break;
                case "MyBuilding":
                    var buildingWindow = new MyBuildingsWindow(_tenantId);
                    buildingWindow.Show();
                    this.Close();
                    break;
                case "NoticeBoard":
                    var noticeBoardWindow = new NoticeBoardWindow(_tenantId);
                    noticeBoardWindow.Show();
                    this.Close();
                    break;
                case "Requests":
                    var requestsWindow = new TenantBuildingRequestsWindow(_tenantId);
                    requestsWindow.Show();
                    this.Close();
                    break;
                case "RentingRooms":
                    var rentalWindow = new RentalRequestsWindow(_tenantId, _buildingId);
                    rentalWindow.Show();
                    this.Close();
                    break;
                case "Homeowners":
                        var homeownersWindow = new HomeownersMeetingWindow(_tenantId);
                        homeownersWindow.Show();
                        this.Close();
                    break;
                    case "Problem":
                        var problemWindow = new ProblemWindow(_tenantId);
                        problemWindow.Show();
                        this.Close();
                    break;
            }
        }
    }

    private void NotificationButton_Click(object sender, RoutedEventArgs e)
    {
        var notificationsWindow = new NotificationsWindow(_tenantId, _buildingId, DIConfiguration.Factory.CreateHelpAdService());
        notificationsWindow.Show();
        this.Close();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        CommunityHub.Ui.Security.UserSession.SignOut();
        var loginForm = new LogInForm();
        loginForm.Show();
        this.Close();
    }
}
