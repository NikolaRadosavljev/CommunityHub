using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;
using CommunityHub.Ui.ViewModels;
using CommunityHub.Ui.DependencyInjection;
using CommunityHub.Application.Database.Repositories;

namespace CommunityHub.Ui.Views.TenantViews;

public partial class RentalRequestsWindow : Window
{
    private readonly long _tenantId;
    private int _selectedBuildingId;
    private RentalRequestsViewModel? _viewModel;
    private readonly HelpBoardRepository _helpBoardService;
    private readonly IBuildingRepository _buildingRepository;

    public RentalRequestsWindow(
        long tenantId,
        int buildingId = 0,
        IBuildingRepository? buildingRepository = null)
    {
        InitializeComponent();
        _tenantId = tenantId;
        _selectedBuildingId = buildingId;
        _buildingRepository = buildingRepository ?? DIConfiguration.Factory.CreateBuildingRepository();
        _helpBoardService = new HelpBoardRepository();

        Loaded += RentalRequestsWindow_Loaded;
    }

    private void RentalRequestsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadUserBuildings();

        // If building is pre-selected, auto-load common rooms
        if (_selectedBuildingId != 0)
        {
            LoadCommonRooms(_selectedBuildingId);
        }
    }

    private void LoadUserBuildings()
    {
        try
        {
            var allBuildings = _buildingRepository.Search(null, null, null, null);
            var userBuildings = allBuildings.Where(b => _helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id)).ToList();

            var buildingDTOs = userBuildings.Select(b => new BuildingDTO
            {
                Id = b.Id,
                Address = b.Address,
                Settlement = b.Settlement,
                City = b.City,
                Country = b.Country,
                FloorNumber = b.FloorNumber
            }).ToList();

            BuildingComboBox.ItemsSource = buildingDTOs;

            // Pre-select building if provided
            if (_selectedBuildingId != 0)
            {
                BuildingComboBox.SelectedValue = _selectedBuildingId;
            }
            // Auto-select first building if none is pre-selected
            else if (buildingDTOs.Count > 0)
            {
                BuildingComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading buildings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (BuildingComboBox.SelectedValue is long selectedId)
            {
                _selectedBuildingId = (int)selectedId;
                LoadCommonRooms(_selectedBuildingId);
            }
            else if (BuildingComboBox.SelectedValue is int selectedIntId)
            {
                _selectedBuildingId = selectedIntId;
                LoadCommonRooms(_selectedBuildingId);
            }
            else if (BuildingComboBox.SelectedValue != null && long.TryParse(BuildingComboBox.SelectedValue.ToString(), out long buildingId))
            {
                _selectedBuildingId = (int)buildingId;
                LoadCommonRooms(_selectedBuildingId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error selecting building: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCommonRooms(int buildingId)
    {
        try
        {
            _viewModel = new RentalRequestsViewModel(_tenantId, buildingId);
            _viewModel!.LoadCommonRooms();

            if (_viewModel!.AvailableRooms.Count == 0)
            {
                MessageBox.Show("No common rooms available for this building.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CommonRoomsListView.ItemsSource = _viewModel.AvailableRooms;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading common rooms: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SendRequestButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int commonRoomId)
        {
            // Find the parent Border which contains the entire item
            var container = FindParent<Border>(btn);
            if (container != null)
            {
                // Get the room data to check rental type
                var room = _viewModel?.AvailableRooms.FirstOrDefault(r => r.Id == commonRoomId);
                if (room == null)
                {
                    MessageBox.Show("Room not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Choose correct date pickers based on rental type
                var startPicker = FindChild<DatePicker>(container, "StartDatePicker");
                var endPicker = FindChild<DatePicker>(container, "EndDatePicker");

                // For single day, use start date for both
                if (room.RentalType == "SingleDay")
                {
                    endPicker = startPicker;
                }

                if (startPicker?.SelectedDate == null)
                {
                    MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (room.RentalType == "MultipleDays" && endPicker?.SelectedDate == null)
                {
                    MessageBox.Show("Please select end date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = startPicker.SelectedDate.Value;
                DateTime endDate = endPicker?.SelectedDate ?? startDate;

                if (startDate > endDate)
                {
                    MessageBox.Show("Start date must be before end date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (startDate.Date < DateTime.Now.Date)
                {
                    MessageBox.Show("Start date cannot be in the past.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var requestId = _viewModel!.CreateRentalRequest(commonRoomId, startDate, endDate);
                    MessageBox.Show($"✓ Request sent successfully!\n\nRoom rental request has been created.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Clear date pickers
                    startPicker.SelectedDate = null;
                    if (endPicker != startPicker)
                        endPicker.SelectedDate = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void SeeRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        // If _selectedBuildingId is not set but ComboBox has selection, set it
        if (_selectedBuildingId == 0 && BuildingComboBox.SelectedValue is long selectedId)
        {
            _selectedBuildingId = (int)selectedId;
        }
        else if (_selectedBuildingId == 0 && BuildingComboBox.SelectedValue is int selectedIntId)
        {
            _selectedBuildingId = selectedIntId;
        }
        else if (_selectedBuildingId == 0 && BuildingComboBox.SelectedValue != null)
        {
            if (long.TryParse(BuildingComboBox.SelectedValue.ToString(), out long buildingId))
            {
                _selectedBuildingId = (int)buildingId;
            }
        }

        if (BuildingComboBox.SelectedIndex < 0)
        {
            MessageBox.Show("Please select a building first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Open requests window
        var requestsWindow = new MyRentalRequestsWindow(_tenantId, _selectedBuildingId);
        requestsWindow.Show();
        this.Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Go back to TenantWindow
        var tenantWindow = new TenantWindow(_tenantId);
        tenantWindow.Show();
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
                    // Stay on this page - do nothing
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
        var notificationsWindow = new NotificationsWindow(_tenantId, 0, DIConfiguration.Factory.CreateHelpAdService());
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

    // Helper methods
    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parent == null) return null;
        if (parent is T t) return t;
        return FindParent<T>(parent);
    }

    private T? FindChild<T>(DependencyObject? parent, string childName) where T : DependencyObject
    {
        if (parent == null) return null;
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t && (child as FrameworkElement)?.Name == childName)
                return t;
            var result = FindChild<T>(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}
