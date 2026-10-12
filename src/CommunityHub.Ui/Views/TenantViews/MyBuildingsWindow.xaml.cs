using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Database;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class MyBuildingsWindow : Window
    {
        private readonly long _tenantId;
        private readonly BuildingService _buildingService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IBuildingMembershipRepository _membershipRepository;
        private readonly IHelpAdService _helpAdService;
        private long _selectedBuildingId = -1;
        private Border? _previouslySelectedBorder;

        public MyBuildingsWindow(
            long tenantId,
            IBuildingRepository? buildingRepository = null,
            IBuildingMembershipRepository? membershipRepository = null,
            IHelpAdService? helpAdService = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingRepository = buildingRepository ?? DIConfiguration.Factory.CreateBuildingRepository();
            _buildingService = new BuildingService(_buildingRepository);
            _membershipRepository = membershipRepository ?? DIConfiguration.Factory.CreateBuildingMembershipRepository();
            _helpAdService = helpAdService ?? DIConfiguration.Factory.CreateHelpAdService();

            Loaded += MyBuildingsWindow_Loaded;
        }

        private void MyBuildingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMyBuildings();
        }

        private void LoadMyBuildings()
        {
            try
            {
                var allBuildings = _buildingRepository.Search(null, null, null, null).ToList();
                var myBuildings = new List<BuildingDTO>();

                foreach (var building in allBuildings)
                {
                    if (HasTenantMembership(_tenantId, building.Id))
                    {
                        string flatsRange = "-";
                        if (building.FlatNumbers != null && building.FlatNumbers.Any())
                        {
                            int minFlat = building.FlatNumbers.Min();
                            int maxFlat = building.FlatNumbers.Max();
                            flatsRange = minFlat == maxFlat ? minFlat.ToString() : $"{minFlat}-{maxFlat}";
                        }

                        // Get occupancy count
                        int occupancyCount = _buildingService.GetOccupancyCount(building.Id);
                        int totalFlats = building.FlatNumbers?.Count ?? 0;

                        // Get flat number for this tenant in this building
                        int flatNumberForTenant = GetFlatNumberForTenant(_tenantId, building.Id);

                        // Generate pie chart path data
                        string pieChartData = GeneratePieChartPathData(occupancyCount, totalFlats);

                        myBuildings.Add(new BuildingDTO
                        {
                            Id = building.Id,
                            Address = building.Address ?? string.Empty,
                            Settlement = building.Settlement ?? string.Empty,
                            City = building.City ?? string.Empty,
                            Country = building.Country ?? string.Empty,
                            FloorNumber = building.FloorNumber,
                            TotalFlats = totalFlats,
                            FlatsRange = flatsRange,
                            MainPicture = building.Pictures?.FirstOrDefault() ?? string.Empty,
                            FlatNumbers = building.FlatNumbers ?? new System.Collections.Generic.List<int>(),
                            OccupancyCount = occupancyCount,
                            FlatNumberForTenant = flatNumberForTenant,
                            PieChartPathData = pieChartData
                        });
                    }
                }

                if (myBuildings.Count == 0)
                {
                    MyBuildingsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    MyBuildingsListControl.ItemsSource = myBuildings;
                    MyBuildingsListControl.Visibility = Visibility.Visible;
                    EmptyStateMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading buildings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetFlatNumberForTenant(long tenantId, long buildingId)
        {
            IDbConnection connection = null;
            try
            {
                connection = PostgresConnection.CreateConnection();
                IDbCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT flat_number
                    FROM building_memberships
                    WHERE building_id = @buildingId AND user_id = @userId
                    LIMIT 1";

                var buildingIdParam = command.CreateParameter();
                buildingIdParam.ParameterName = "@buildingId";
                buildingIdParam.Value = buildingId;
                command.Parameters.Add(buildingIdParam);

                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@userId";
                userIdParam.Value = tenantId;
                command.Parameters.Add(userIdParam);

                var result = command.ExecuteScalar();
                return result != null ? System.Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
                connection?.Close();
                connection?.Dispose();
            }
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

        private bool HasTenantMembership(long tenantId, long buildingId)
        {
            IDbConnection connection = null;
            try
            {
                connection = PostgresConnection.CreateConnection();
                System.Data.IDbCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM building_memberships
                    WHERE building_id = @buildingId AND user_id = @userId";

                var buildingIdParam = command.CreateParameter();
                buildingIdParam.ParameterName = "@buildingId";
                buildingIdParam.Value = buildingId;
                command.Parameters.Add(buildingIdParam);

                var userIdParam = command.CreateParameter();
                userIdParam.ParameterName = "@userId";
                userIdParam.Value = tenantId;
                command.Parameters.Add(userIdParam);

                long count = System.Convert.ToInt64(command.ExecuteScalar());
                return count > 0;
            }
            finally
            {
                connection?.Close();
                connection?.Dispose();
            }
        }

        private void ViewAdsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is long buildingId)
            {
                var window = new NoticeBoardWindow(_tenantId, buildingId, _helpAdService);
                window.Show();
                this.Close();
            }
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is long buildingId)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to leave this building?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        MessageBox.Show("You have left the building.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadMyBuildings();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, 0, _helpAdService);
            notificationsWindow.Show();
            this.Close();
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "Home":
                        var homeWindow = new TenantWindow(_tenantId);
                        homeWindow.Show();
                        this.Close();
                        break;
                    case "NoticeBoard":
                        var buildingRepo = DIConfiguration.Factory.CreateBuildingRepository();
                        var helpBoardService = new CommunityHub.Application.Database.Repositories.HelpBoardRepository();
                        var userBuilding = buildingRepo.Search(null, null, null, null)
                            .OrderBy(b => b.Id)
                            .FirstOrDefault(b => helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id));

                        if (userBuilding == null)
                        {
                            MessageBox.Show("You must be a member of a building to access the notice board.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var noticeBoardWindow = new NoticeBoardWindow(_tenantId, userBuilding.Id, _helpAdService);
                        noticeBoardWindow.Show();
                        this.Close();
                        break;
                    case "Requests":
                        var requestsWindow = new TenantBuildingRequestsWindow(_tenantId);
                        requestsWindow.Show();
                        this.Close();
                        break;
                    case "RentingRooms":
                        var rentalWindow = new RentalRequestsWindow(_tenantId);
                        rentalWindow.Show();
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

        private void BuildingCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is long buildingId)
            {
                // If clicking the same building, deselect it
                if (_selectedBuildingId == buildingId)
                {
                    if (_previouslySelectedBorder != null)
                    {
                        _previouslySelectedBorder.BorderThickness = new Thickness(1);
                        _previouslySelectedBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDD, 0xDD, 0xDD));
                    }
                    _selectedBuildingId = -1;
                    _previouslySelectedBorder = null;
                }
                else
                {
                    // Deselect previous selection
                    if (_previouslySelectedBorder != null)
                    {
                        _previouslySelectedBorder.BorderThickness = new Thickness(1);
                        _previouslySelectedBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDD, 0xDD, 0xDD));
                    }

                    // Select new building
                    _selectedBuildingId = buildingId;
                    _previouslySelectedBorder = border;
                    border.BorderThickness = new Thickness(3);
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }
    }

    public class BuildingDTO
    {
        public long Id { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Settlement { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int TotalFlats { get; set; }
        public int OccupancyCount { get; set; }
        public string FlatsRange { get; set; } = string.Empty;
        public string MainPicture { get; set; } = string.Empty;
        public List<int> FlatNumbers { get; set; } = new List<int>();
        public int FlatNumberForTenant { get; set; }
        public string PieChartPathData { get; set; } = string.Empty;

        public int OccupancyPercentage => TotalFlats > 0 ? (OccupancyCount * 100) / TotalFlats : 0;
        public string OccupancyDisplayText => $"{OccupancyCount}/{TotalFlats} flats occupied";
    }
}
