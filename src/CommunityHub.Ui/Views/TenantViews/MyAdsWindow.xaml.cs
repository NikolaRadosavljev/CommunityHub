using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class MyAdsWindow : Window
    {
        private readonly long _tenantId;
        private readonly long _buildingId;
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdBookingRepository _bookingRepository;
        private List<HelpAdDTO> _allAds;
        private HashSet<long> _selectedAdIds = new HashSet<long>();

        public MyAdsWindow(
            long tenantId,
            long buildingId,
            IHelpAdService helpAdService,
            HelpAdBookingRepository bookingRepository = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;
            _helpAdService = helpAdService;
            _bookingRepository = bookingRepository ?? DIConfiguration.Factory.CreateHelpAdBookingRepository();
            _allAds = new List<HelpAdDTO>();

            Loaded += MyAdsWindow_Loaded;
        }

        private void MyAdsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMyAds();
        }

        private async void LoadMyAds()
        {
            try
            {
                // Get all ads for the building
                var allAds = await _helpAdService.GetHelpAdsForBuildingAsync(_buildingId);

                // Filter only ads created by this tenant
                _allAds = allAds?.Where(a => a.TenantId == _tenantId).ToList() ?? new List<HelpAdDTO>();

                // Apply sorting filters
                if (_allAds != null && _allAds.Count > 0)
                {
                    await ApplyFiltersAsync();
                }
                else
                {
                    MyAdsListControl.ItemsSource = null;
                    MyAdsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                _allAds = new List<HelpAdDTO>();
                ShowError($"Error loading your ads: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ApplyFiltersAsync()
        {
            try
            {
                if (MyAdsListControl == null || EmptyStateMessage == null)
                {
                    return;
                }

                if (_allAds == null || _allAds.Count == 0)
                {
                    MyAdsListControl.ItemsSource = null;
                    MyAdsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                    return;
                }

                var filtered = _allAds.AsEnumerable();

                // Filter by type
                if (RbFilterOffers?.IsChecked == true)
                {
                    filtered = filtered.Where(a => a.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));
                }
                else if (RbFilterRequests?.IsChecked == true)
                {
                    filtered = filtered.Where(a => a.Type.Equals("seeking", StringComparison.OrdinalIgnoreCase));
                }

                // Filter by category
                var selectedCategory = (CbFilterCategory?.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All")
                {
                    filtered = filtered.Where(a => a.Category == selectedCategory);
                }

                // Map to display model with slots
                var displayAds = new List<MyAdDisplayModel>();
                foreach (var ad in filtered.OrderByDescending(a => a.Id))
                {
                    // Load slots for this ad to get working hours
                    var slots = await GetSlotsForAd(ad.Id);
                    var workingHours = "N/A";

                    if (slots.Count > 0)
                    {
                        var minTime = slots.Min(s => s.StartTime);
                        var maxTime = slots.Max(s => s.EndTime);
                        workingHours = $"od {minTime:hh\\:mm} do {maxTime:hh\\:mm}";
                    }

                    displayAds.Add(new MyAdDisplayModel
                    {
                        Id = ad.Id,
                        TypeBadge = ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase) ? "Offers" : "Requests",
                        Category = ad.Category,
                        Description = ad.Description,
                        StartDate = ad.StartDate,
                        EndDate = ad.EndDate,
                        Status = ad.Status ?? "active",
                        WorkingHours = workingHours,
                        Slots = slots
                    });
                }

                if (displayAds.Count == 0)
                {
                    MyAdsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    MyAdsListControl.ItemsSource = displayAds;
                    MyAdsListControl.Visibility = Visibility.Visible;
                    EmptyStateMessage.Visibility = Visibility.Collapsed;

                    // Apply status colors after a short delay to allow binding to complete
                    this.Dispatcher.BeginInvoke(new Action(() => ApplyStatusColors()),
                        System.Windows.Threading.DispatcherPriority.Render);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error filtering ads: {ex.Message}");
            }
        }

        private async void RbFilterType_Changed(object sender, RoutedEventArgs e)
        {
            await ApplyFiltersAsync();
        }

        private async void CbFilterCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ApplyFiltersAsync();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, _buildingId, _helpAdService);
            notificationsWindow.Show();
            this.Close();
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, _buildingId, _helpAdService);
            notificationsWindow.Show();
            this.Close();
        }

        private void CreateAdButton_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateHelpAdWindow(_tenantId, _buildingId, _helpAdService);
            createWindow.Show();
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
                        var noticeBoardWindow = new NoticeBoardWindow(_tenantId, _buildingId, _helpAdService);
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }

        private async System.Threading.Tasks.Task<List<HelpAdSlotDTO>> GetSlotsForAd(long adId)
        {
            try
            {
                var slots = await _helpAdService.GetAvailableSlotsForAdAsync(adId);
                return slots ?? new List<HelpAdSlotDTO>();
            }
            catch
            {
                return new List<HelpAdSlotDTO>();
            }
        }

        private void ApplyStatusColors()
        {
            try
            {
                for (int i = 0; i < MyAdsListControl.Items.Count; i++)
                {
                    var container = MyAdsListControl.ItemContainerGenerator.ContainerFromIndex(i);
                    if (container != null)
                    {
                        var statusTextBlock = FindStatusTextBlock(container);
                        if (statusTextBlock != null && statusTextBlock.Text != null)
                        {
                            if (statusTextBlock.Text.Equals("archived", StringComparison.OrdinalIgnoreCase))
                            {
                                statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)); // Red
                            }
                            else
                            {
                                statusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // Green
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently ignore if we can't find the elements
            }
        }

        private TextBlock FindStatusTextBlock(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TextBlock tb && tb.Name == "StatusText")
                {
                    return tb;
                }

                var result = FindStatusTextBlock(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void AdItem_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is long adId)
            {
                if (_selectedAdIds.Contains(adId))
                {
                    _selectedAdIds.Remove(adId);
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC));
                }
                else
                {
                    _selectedAdIds.Add(adId);
                    border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33));
                }
            }
        }

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedAdIds.Count == 0)
                {
                    MessageBox.Show("Please select at least one ad to archive.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to archive {_selectedAdIds.Count} ad(s)? They will no longer appear in suggestions, but existing bookings will remain visible.",
                    "Archive Ads",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    foreach (var adId in _selectedAdIds)
                    {
                        await _helpAdService.ArchiveHelpAdAsync(adId);
                    }
                    MessageBox.Show("Ads archived successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _selectedAdIds.Clear();
                    LoadMyAds();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error archiving ads: {ex.Message}");
            }
        }
    }

    public class MyAdDisplayModel
    {
        public long Id { get; set; }
        public string TypeBadge { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string WorkingHours { get; set; }
        public List<HelpAdSlotDTO> Slots { get; set; } = new List<HelpAdSlotDTO>();
    }
}
