using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class NoticeBoardWindow : Window
    {
        private readonly long _tenantId;
        private long _buildingId;
        private readonly IHelpAdService _helpAdService;
        private readonly IUserRepository _userRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IBuildingMembershipRepository _membershipRepository;
        private readonly Dictionary<long, string> _tenantNameCache;
        private List<Building> _userBuildings;

        public NoticeBoardWindow(
            long tenantId,
            long buildingId = 0,
            IHelpAdService? helpAdService = null,
            IUserRepository? userRepository = null,
            IBuildingRepository? buildingRepository = null,
            IBuildingMembershipRepository? membershipRepository = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;

            // Koristi prosleđene servise ili kreiraj default implementacije preko Factory
            _helpAdService = helpAdService ?? DIConfiguration.Factory.CreateHelpAdService();
            _userRepository = userRepository ?? DIConfiguration.Factory.CreateUserRepository();
            _buildingRepository = buildingRepository ?? DIConfiguration.Factory.CreateBuildingRepository();
            _membershipRepository = membershipRepository ?? DIConfiguration.Factory.CreateBuildingMembershipRepository();
            _tenantNameCache = new Dictionary<long, string>();
            _userBuildings = new List<Building>();

            Loaded += NoticeBoardWindow_Loaded;
        }

        private void NoticeBoardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserBuildings();

            // If building is pre-selected (from MyBuildings), show ads directly
            if (_buildingId > 0)
            {
                ShowAdsPanel();
                LoadActiveAds();
            }
            else
            {
                // Otherwise show building selection with statistics
                ShowBuildingSelectionPanel();
            }
        }

        private void ShowBuildingSelectionPanel()
        {
            BuildingSelectionPanel.Visibility = Visibility.Visible;
            AdsContentPanel.Visibility = Visibility.Collapsed;
            ToolbarBorder.Visibility = Visibility.Collapsed;
            LoadBuildingStatistics();
        }

        private void ShowAdsPanel()
        {
            BuildingSelectionPanel.Visibility = Visibility.Collapsed;
            AdsContentPanel.Visibility = Visibility.Visible;
            ToolbarBorder.Visibility = Visibility.Visible;
            BuildingSelector.ItemsSource = _userBuildings;
            if (_userBuildings.Count > 0)
            {
                var selectedBuilding = _userBuildings.FirstOrDefault(b => b.Id == _buildingId);
                BuildingSelector.SelectedItem = selectedBuilding ?? _userBuildings[0];
            }
        }

        private async void LoadBuildingStatistics()
        {
            try
            {
                var stats = new List<BuildingStatistic>();
                int maxAds = 0;

                foreach (var building in _userBuildings)
                {
                    var ads = await _helpAdService.GetHelpAdsForBuildingAsync(building.Id);
                    int adCount = ads?.Count ?? 0;
                    if (adCount > maxAds) maxAds = adCount;

                    stats.Add(new BuildingStatistic
                    {
                        BuildingAddress = building.Address,
                        BuildingId = building.Id,
                        AdCount = adCount
                    });
                }

                // Calculate bar widths (normalize to 350px max)
                foreach (var stat in stats)
                {
                    stat.BarWidth = maxAds > 0 ? (stat.AdCount * 350.0 / maxAds) : 0;
                }

                StatsChart.ItemsSource = stats;
                StatsChartLabels.ItemsSource = stats;
                BuildingSelectionCombo.ItemsSource = _userBuildings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildingSelectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BuildingSelectionCombo.SelectedItem is Building selectedBuilding)
            {
                _buildingId = (long)selectedBuilding.Id;
                ShowAdsPanel();
                LoadActiveAds();
            }
        }

        private void LoadUserBuildings()
        {
            try
            {
                var allBuildings = _buildingRepository.Search(null, null, null, null).ToList();
                _userBuildings = new List<Building>();

                foreach (var building in allBuildings)
                {
                    if (HasTenantMembership(_tenantId, (long)building.Id))
                    {
                        _userBuildings.Add(building);
                    }
                }

                // Bind buildings to ComboBox if it exists
                if (BuildingSelector != null)
                {
                    BuildingSelector.ItemsSource = _userBuildings;
                    if (_userBuildings.Count > 0)
                    {
                        BuildingSelector.SelectedIndex = 0;
                        _buildingId = (long)_userBuildings[0].Id;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading buildings: {ex.Message}");
            }
        }

        private bool HasTenantMembership(long tenantId, long buildingId)
        {
            IDbConnection? connection = null;
            try
            {
                connection = PostgresConnection.CreateConnection();
                IDbCommand command = connection.CreateCommand();
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

                long count = Convert.ToInt64(command.ExecuteScalar());
                return count > 0;
            }
            finally
            {
                connection?.Close();
                connection?.Dispose();
            }
        }

        private async void LoadActiveAds()
        {
            try
            {
                if (_helpAdService == null)
                {
                    ShowError("Service not initialized. Please try again.");
                    return;
                }

                // Check if XAML controls are initialized
                if (RbFilterOffers == null || RbFilterRequests == null || RbFilterAll == null ||
                    CbFilterCategory == null || CbFromDay == null || CbFromMonth == null || CbFromYear == null ||
                    CbToDay == null || CbToMonth == null || CbToYear == null ||
                    HelpAdsListControl == null || EmptyStateMessage == null)
                {
                    return;
                }

                var ads = await _helpAdService.GetHelpAdsForBuildingAsync(_buildingId);

                if (ads == null)
                {
                    ShowError("Unable to load ads. Please check your database connection.");
                    return;
                }

                // Filter only active ads (status = "active")
                var activeAds = ads.Where(a => a.Status.Equals("active", StringComparison.OrdinalIgnoreCase) 
                    && a.EndDate.Date >= DateTime.Now.Date).ToList();

                // Apply type filter (offers/requests/all). Default is "All" so every tenant's
                // active ads (both offering and seeking) are visible on the notice board.
                bool isOffers = RbFilterOffers.IsChecked ?? false;
                bool isRequests = RbFilterRequests.IsChecked ?? false;

                if (isOffers)
                {
                    activeAds = activeAds.Where(a => a.Type.Equals("offering", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else if (isRequests)
                {
                    activeAds = activeAds.Where(a => a.Type.Equals("seeking", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                // If "All" is selected (default), no additional filtering — ads from every tenant in the building are shown.

                // Apply category filter
                var selectedCategory = (CbFilterCategory.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "All";
                if (selectedCategory != "All")
                {
                    activeAds = activeAds.Where(a => a.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Apply date range filter - find ads that overlap with selected date range
                var fromDate = GetDateFromFilters(CbFromDay, CbFromMonth, CbFromYear);
                var toDate = GetDateFromFilters(CbToDay, CbToMonth, CbToYear);

                if (fromDate.HasValue && toDate.HasValue)
                {
                    // Find ads where: ad.StartDate <= toDate AND ad.EndDate >= fromDate (overlap logic)
                    activeAds = activeAds.Where(a =>
                        a.StartDate.Date <= toDate.Value.Date &&
                        a.EndDate.Date >= fromDate.Value.Date
                    ).ToList();
                }
                else if (fromDate.HasValue)
                {
                    // Only from date specified - find ads that are active on or after fromDate
                    activeAds = activeAds.Where(a => a.EndDate.Date >= fromDate.Value.Date).ToList();
                }
                else if (toDate.HasValue)
                {
                    // Only to date specified - find ads that start on or before toDate
                    activeAds = activeAds.Where(a => a.StartDate.Date <= toDate.Value.Date).ToList();
                }

                // Load slots for all active ads to get the actual working hours
                var adIds = activeAds.Select(a => a.Id).ToList();
                var slotsByAdId = new Dictionary<long, List<HelpAdSlotDTO>>();
                foreach (var adId in adIds)
                {
                    try
                    {
                        var slots = await _helpAdService.GetAvailableSlotsForAdAsync(adId);
                        slotsByAdId[adId] = slots;
                    }
                    catch
                    {
                        slotsByAdId[adId] = new List<HelpAdSlotDTO>();
                    }
                }

                // Map to display model
                var displayAds = activeAds.Select(ad => new HelpAdDisplayModel
                {
                    Id = ad.Id,
                    TenantName = GetTenantName(ad.TenantId),
                    OfferingType = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "Offering:" : "Seeking:",
                    TypeBadge = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "offers" : "request",
                    Category = ad.Category,
                    Description = ad.Description,
                    StartDate = ad.StartDate,
                    EndDate = ad.EndDate,
                    Status = ad.Status,
                    FreeAppointments = BuildFreeAppointmentsSummary(slotsByAdId.ContainsKey(ad.Id) ? slotsByAdId[ad.Id] : new List<HelpAdSlotDTO>())
                }).OrderByDescending(a => a.StartDate).ToList();

                if (displayAds.Count == 0)
                {
                    HelpAdsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    HelpAdsListControl.ItemsSource = displayAds;
                    HelpAdsListControl.Visibility = Visibility.Visible;
                    EmptyStateMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading ads: {ex.Message}");
            }
        }

        private void BuildingSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BuildingSelector.SelectedItem is Building selectedBuilding)
            {
                _buildingId = (long)selectedBuilding.Id;
                LoadActiveAds();
            }
        }

        private static string BuildFreeAppointmentsSummary(List<HelpAdSlotDTO> slots)
        {
            if (slots == null || slots.Count == 0)
            {
                return "No time slots available";
            }

            // Get unique working hours (min start time and max end time)
            var minStartTime = slots.Min(s => s.StartTime);
            var maxEndTime = slots.Max(s => s.EndTime);

            return $"{minStartTime:hh\\:mm} - {maxEndTime:hh\\:mm}";
        }

        private string GetTenantName(long tenantId)
        {
            if (_tenantNameCache.TryGetValue(tenantId, out var cachedName))
            {
                return cachedName;
            }

            try
            {
                var user = _userRepository.GetById(tenantId);
                var displayName = user != null
                    ? $"{user.Name} {user.Surname}".Trim()
                    : $"Tenant #{tenantId}";

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = $"Tenant #{tenantId}";
                }

                _tenantNameCache[tenantId] = displayName;
                return displayName;
            }
            catch
            {
                return $"Tenant #{tenantId}";
            }
        }

        private DateTime? GetDateFromFilters(System.Windows.Controls.ComboBox cbDay, System.Windows.Controls.ComboBox cbMonth, System.Windows.Controls.ComboBox cbYear)
        {
            try
            {
                var dayStr = (cbDay.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                var monthStr = (cbMonth.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                var yearStr = (cbYear.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

                if (string.IsNullOrEmpty(dayStr) || string.IsNullOrEmpty(monthStr) || string.IsNullOrEmpty(yearStr))
                {
                    return null;
                }

                if (!int.TryParse(dayStr, out int day) || !int.TryParse(yearStr, out int year))
                {
                    return null;
                }

                // Convert month name to number - map English month names
                var monthMap = new Dictionary<string, int>
                {
                    { "January", 1 }, { "February", 2 }, { "March", 3 }, { "April", 4 },
                    { "May", 5 }, { "June", 6 }, { "July", 7 }, { "August", 8 },
                    { "September", 9 }, { "October", 10 }, { "November", 11 }, { "December", 12 }
                };

                if (!monthMap.TryGetValue(monthStr, out int month))
                {
                    return null;
                }

                if (day <= 0 || day > 31 || month <= 0 || year <= 0)
                {
                    return null;
                }

                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private void CreateAdButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateHelpAdWindow(_tenantId, _buildingId, _helpAdService);
            window.Show();
            this.Close();
        }

        private void AdCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is long helpAdId)
            {
                var window = new ScheduleAppointmentWindow(_tenantId, _buildingId, helpAdId, _helpAdService);
                window.Show();
                this.Close();
            }
        }

        private void ShowError(string message)
        {
            // Error handling can be done through message boxes if needed
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RbFilterType_Changed(object sender, RoutedEventArgs e)
        {
            if (_helpAdService != null)
            {
                LoadActiveAds();
            }
        }

        private void CbFilterCategory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_helpAdService != null)
            {
                LoadActiveAds();
            }
        }

        private void OnDateFilterChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_helpAdService != null)
            {
                LoadActiveAds();
            }
        }

        private void YourAdsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new MyAdsWindow(_tenantId, _buildingId, _helpAdService);
            window.Show();
            this.Close();
        }

        public void RefreshAds()
        {
            LoadActiveAds();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            TenantWindow tenantWindow = new TenantWindow(_tenantId);
            tenantWindow.Show();
            this.Close();
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NotificationsWindow(_tenantId, _buildingId, _helpAdService);
            window.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
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
                    case "MyBuilding":
                        var buildingWindow = new MyBuildingsWindow(_tenantId);
                        buildingWindow.Show();
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
    }

    public class HelpAdDisplayModel
    {
        public long Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string OfferingType { get; set; } = string.Empty;
        public string TypeBadge { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FreeAppointments { get; set; } = string.Empty;
    }

    public class BuildingStatistic
    {
        public long BuildingId { get; set; }
        public string BuildingAddress { get; set; }
        public int AdCount { get; set; }
        public double BarWidth { get; set; }
    }
}
