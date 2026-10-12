using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views.TenantViews;
using MyBuildingsWindow = CommunityHub.Ui.Views.TenantViews.MyBuildingsWindow;
using CommunityHub.Ui.Views.CoordinatorViews;

namespace CommunityHub.Ui.Views.TenantViews;

public partial class TenantBuildingRequestsWindow : Window
{
    private readonly long _userId;
    private readonly BuildingRequestService _requestService;
    private readonly BuildingService _buildingService;
    private TenantRequestDisplayViewModel? _selectedRequest;

    public TenantBuildingRequestsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _requestService = new BuildingRequestService();
        _buildingService = new BuildingService(CommunityHub.Ui.DependencyInjection.DIConfiguration.Factory.CreateBuildingRepository());

        if (!UserSession.IsAuthenticated)
        {
            RedirectToLogin();
            return;
        }

        Loaded += (s, e) =>
        {
            // Initialize combo boxes with default values
            if (StatusFilterComboBox.Items.Count > 0)
            {
                StatusFilterComboBox.SelectedIndex = 0;
            }
            if (SortDirectionComboBox.Items.Count > 0)
            {
                SortDirectionComboBox.SelectedIndex = 0;
            }
            LoadRequests();
        };
    }

    private void LoadRequests()
    {
        // Get status filter
        RequestStatus? statusFilter = null;
        if (StatusFilterComboBox != null && StatusFilterComboBox.SelectedItem is ComboBoxItem statusItem)
        {
            var statusText = statusItem.Content?.ToString() ?? "All";
            if (statusText != "All")
            {
                if (Enum.TryParse<RequestStatus>(statusText, ignoreCase: true, out var parsedStatus))
                {
                    statusFilter = parsedStatus;
                }
            }
        }

        // Get sort direction
        bool ascending = false;
        if (SortDirectionComboBox != null && SortDirectionComboBox.SelectedItem is ComboBoxItem sortItem)
        {
            var sortText = sortItem.Content?.ToString() ?? "Newest";
            if (sortText == "Oldest")
            {
                ascending = true;
            }
        }

        var requests = _requestService.GetTenantRequests(_userId, null, ascending);

        // Apply status filter manually if needed
        if (statusFilter.HasValue)
        {
            requests = requests.Where(r => r.Status == statusFilter.Value).ToList();
        }

        var displayRequests = requests.Select(r =>
        {
            var building = _buildingService.GetById(r.BuildingId);
            var occupancyCount = _buildingService.GetOccupancyCount(r.BuildingId);
            var totalFlats = building?.FlatNumbers.Count ?? 0;
            var firstPhoto = building?.Pictures.FirstOrDefault() ?? string.Empty;
            var pieChartData = GeneratePieChartPathData(occupancyCount, totalFlats);

            return new TenantRequestDisplayViewModel
            {
                Id = r.Id,
                Status = r.Status,
                Address = r.Address,
                Settlement = r.Settlement,
                Location = $"{r.City}, {r.Country}",
                FlatNumber = r.FlatNumber,
                CreatedAt = r.CreatedAt,
                BuildingId = r.BuildingId,
                BuildingPhotoUrl = firstPhoto,
                OccupancyCount = occupancyCount,
                TotalFlats = totalFlats,
                PieChartPathData = pieChartData
            };
        }).ToList();

        RequestsListView.ItemsSource = displayRequests;

        // Update status colors after binding
        UpdateStatusColors();
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            LoadRequests();
        }
    }

    private void RequestCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is long requestId)
        {
            var requests = (IEnumerable<TenantRequestDisplayViewModel>)RequestsListView.ItemsSource;
            _selectedRequest = requests?.FirstOrDefault(r => r.Id == requestId);

            // Visual feedback - highlight selected card
            if (sender is Border selectedBorder)
            {
                selectedBorder.BorderThickness = new Thickness(3);
                selectedBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            }
        }
    }

    private void CancelRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not long requestId)
        {
            MessageBox.Show("Invalid request selection.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Find the request
        var requests = (IEnumerable<TenantRequestDisplayViewModel>)RequestsListView.ItemsSource;
        var selectedRequest = requests?.FirstOrDefault(r => r.Id == requestId);

        if (selectedRequest == null)
        {
            MessageBox.Show("Request not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (selectedRequest.Status != RequestStatus.Pending)
        {
            MessageBox.Show("Only pending requests can be withdrawn.", "Invalid Status", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show("Are you sure you want to withdraw this request?", "Withdraw Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _requestService.CancelRequest(selectedRequest.Id, _userId);
                MessageBox.Show("Request withdrawn successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRequests();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error withdrawing request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void WithdrawSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRequest == null)
        {
            MessageBox.Show("Please select a request to withdraw.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_selectedRequest.Status != RequestStatus.Pending)
        {
            MessageBox.Show("Only pending requests can be withdrawn.", "Invalid Status", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Are you sure you want to withdraw this request for {_selectedRequest.Address}?", "Withdraw Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _requestService.CancelRequest(_selectedRequest.Id, _userId);
                MessageBox.Show("Request withdrawn successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _selectedRequest = null;
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
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }

    private void RedirectToLogin()
    {
        LogInForm loginForm = new LogInForm();
        loginForm.Show();
        this.Close();
    }

    private void UpdateStatusColors()
    {
        if (RequestsListView.ItemsSource is not List<TenantRequestDisplayViewModel> requests)
            return;

        // This method updates status colors dynamically
        // Since we're using XAML with multiple borders, we need to handle this in code-behind
        // The visibility of status borders will be controlled when the ItemsControl renders
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        var tenantWindow = new TenantWindow(_userId);
        tenantWindow.Show();
        this.Close();
    }

    private void MyBuildingButton_Click(object sender, RoutedEventArgs e)
    {
        var myBuildingsWindow = new MyBuildingsWindow(_userId);
        myBuildingsWindow.Show();
        this.Close();
    }

    private void NoticeBoardButton_Click(object sender, RoutedEventArgs e)
    {
        var noticeBoardWindow = new NoticeBoardWindow(_userId, 0, null);
        noticeBoardWindow.Show();
        this.Close();
    }

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            switch (tag)
            {
                case "Home":
                    var homeWindow = new TenantWindow(_userId);
                    homeWindow.Show();
                    this.Close();
                    break;
                case "MyBuilding":
                    var buildingWindow = new MyBuildingsWindow(_userId);
                    buildingWindow.Show();
                    this.Close();
                    break;
                case "NoticeBoard":
                    var noticeBoardWindow = new NoticeBoardWindow(_userId, 0, null);
                    noticeBoardWindow.Show();
                    this.Close();
                    break;
                case "RentingRooms":
                    var rentalWindow = new RentalRequestsWindow(_userId);
                    rentalWindow.Show();
                    this.Close();
                    break;
                case "Homeowners":
                        var homeownersWindow = new HomeownersMeetingWindow(_userId);
                        homeownersWindow.Show();
                        this.Close();
                    break;
                    case "Problem":
                        var problemWindow = new ProblemWindow(_userId);
                        problemWindow.Show();
                        this.Close();
                        break;
            }
        }
    }

    private void NotificationButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement notification window
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        CommunityHub.Ui.Security.UserSession.SignOut();
        var loginForm = new LogInForm();
        loginForm.Show();
        this.Close();
    }

    private string GeneratePieChartPathData(int occupancyCount, int totalFlats)
    {
        if (totalFlats <= 0)
            return "";

        double percentage = (occupancyCount * 100.0) / totalFlats;
        double angle = (percentage * 360.0) / 100.0;

        if (angle <= 0)
            return "";

        double angleRad = angle * System.Math.PI / 180.0;

        // Pie chart: center at 50,50, radius 50
        double centerX = 50;
        double centerY = 50;
        double radius = 50;

        // Start point (top of circle)
        double startX = centerX;
        double startY = centerY - radius;

        // End point
        double endX = centerX + radius * System.Math.Sin(angleRad);
        double endY = centerY - radius * System.Math.Cos(angleRad);

        // Large arc flag (1 if angle > 180)
        int largeArcFlag = angle > 180 ? 1 : 0;

        // Path: M (move to center), L (line to start), A (arc), Z (close)
        string pathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "M {0:F1},{1:F1} L {2:F1},{3:F1} A {4:F1},{4:F1} 0 {5} 1 {6:F1},{7:F1} Z",
            centerX, centerY,
            startX, startY,
            radius,
            largeArcFlag,
            endX, endY);

        return pathData;
    }
}

public class TenantRequestDisplayViewModel
{
    public long Id { get; set; }
    public RequestStatus Status { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Settlement { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int FlatNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BuildingId { get; set; }
    public string BuildingPhotoUrl { get; set; } = string.Empty;
    public int OccupancyCount { get; set; }
    public int TotalFlats { get; set; }
    public string PieChartPathData { get; set; } = string.Empty;

    public Visibility AcceptedVisibility => Status == RequestStatus.Accepted ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DeniedVisibility => Status == RequestStatus.Denied ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PendingVisibility => Status == RequestStatus.Pending ? Visibility.Visible : Visibility.Collapsed;

    public int OccupancyPercentage => TotalFlats > 0 ? (OccupancyCount * 100) / TotalFlats : 0;
    public string OccupancyDisplayText => $"{OccupancyCount}/{TotalFlats} flats occupied";
}