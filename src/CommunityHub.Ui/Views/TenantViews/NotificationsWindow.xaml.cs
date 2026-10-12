using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class NotificationsWindow : Window
    {
        private readonly long _tenantId;
        private long _buildingId;
        private readonly IHelpAdService _helpAdService;
        private readonly IUserRepository _userRepository;
        private readonly HelpAdBookingRepository _bookingRepository;
        private readonly HelpAdNotificationRepository _notificationRepository;
        private readonly IBuildingRepository _buildingRepository;
        private List<HelpAdNotification> _allNotifications;
        private long _selectedNotificationId = -1;

        public NotificationsWindow(
            long tenantId,
            long buildingId,
            IHelpAdService helpAdService,
            IUserRepository? userRepository = null,
            HelpAdBookingRepository? bookingRepository = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;
            _helpAdService = helpAdService;
            _userRepository = userRepository ?? DIConfiguration.Factory.CreateUserRepository();
            _bookingRepository = bookingRepository ?? DIConfiguration.Factory.CreateHelpAdBookingRepository();
            _notificationRepository = DIConfiguration.Factory.CreateHelpAdNotificationRepository();
            _buildingRepository = DIConfiguration.Factory.CreateBuildingRepository();

            _allNotifications = new List<HelpAdNotification>();

            Loaded += NotificationsWindow_Loaded;
        }

        private void NotificationsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserBuildings();
            CbSortFilter.SelectedIndex = 0; // Set to "All Notifications" by default

            // Load notifications if building is pre-selected
            if (_buildingId != 0)
            {
                LoadNotifications();
            }
        }

        private void LoadUserBuildings()
        {
            try
            {
                var hepBoardService = new HelpBoardRepository();
                var allBuildings = _buildingRepository.Search(null, null, null, null);
                var userBuildings = allBuildings.Where(b => hepBoardService.IsUserMemberOfBuilding(_tenantId, b.Id)).ToList();

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
                if (_buildingId != 0)
                {
                    BuildingComboBox.SelectedValue = _buildingId;
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
                    _buildingId = selectedId;
                    LoadNotifications();
                }
                else if (BuildingComboBox.SelectedValue is int selectedIntId)
                {
                    _buildingId = selectedIntId;
                    LoadNotifications();
                }
                else if (BuildingComboBox.SelectedValue != null && long.TryParse(BuildingComboBox.SelectedValue.ToString(), out long buildingId))
                {
                    _buildingId = buildingId;
                    LoadNotifications();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting building: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadNotifications()
        {
            try
            {
                // Get all ads for this tenant
                var userAds = await _helpAdService.GetHelpAdsForBuildingAsync(_buildingId);
                var myAds = userAds.Where(a => a.TenantId == _tenantId && !string.Equals(a.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase)).ToList();

                _allNotifications = new List<HelpAdNotification>();

                // For each of my ads, find matching ads
                var matchingService = DIConfiguration.Factory.CreateHelpAdMatchingService();
                foreach (var myAd in myAds)
                {
                    try
                    {
                        var matchingAds = await matchingService.FindMatchingAdsAsync(myAd.Id, _buildingId);

                        foreach (var matchingAd in matchingAds)
                        {
                            var adCreatorUser = _userRepository.GetById(matchingAd.TenantId);
                            var creatorName = adCreatorUser != null
                                ? $"{adCreatorUser.Name} {adCreatorUser.Surname}".Trim()
                                : $"Tenant #{matchingAd.TenantId}";

                            if (string.IsNullOrWhiteSpace(creatorName))
                            {
                                creatorName = $"Tenant #{matchingAd.TenantId}";
                            }

                            _allNotifications.Add(new HelpAdNotification
                            {
                                Id = matchingAd.HelpAdId,
                                HelpAdId = matchingAd.HelpAdId,
                                Category = matchingAd.Category,
                                AdType = matchingAd.Type,
                                BookedByName = creatorName,
                                BookingTime = DateTime.Now, // Use current time for sorting
                                BookingDate = matchingAd.StartDate.Date,
                                NotificationType = "AD_MATCHED"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error finding matching ads for {myAd.Id}: {ex.Message}");
                    }
                }

                // Process SLOT_BOOKED notifications (existing bookings)
                foreach (var ad in myAds)
                {
                    // Get bookings for this ad
                    var bookings = await _bookingRepository.GetBookingsForAdAsync(ad.Id);

                    foreach (var booking in bookings)
                    {
                        // Get the tenant who booked
                        var bookedByUser = _userRepository.GetById(booking.BookingTenantId);
                        var bookedByName = bookedByUser != null
                            ? $"{bookedByUser.Name} {bookedByUser.Surname}".Trim()
                            : $"Tenant #{booking.BookingTenantId}";

                        if (string.IsNullOrWhiteSpace(bookedByName))
                        {
                            bookedByName = $"Tenant #{booking.BookingTenantId}";
                        }

                        _allNotifications.Add(new HelpAdNotification
                        {
                            Id = booking.BookingId,
                            HelpAdId = ad.Id,
                            Category = ad.Category,
                            AdType = ad.Type,
                            BookedByName = bookedByName,
                            BookingTime = booking.SlotDateTime,
                            BookingDate = booking.SlotDateTime.Date,
                            NotificationType = "SLOT_BOOKED"
                        });
                    }
                }

                // Sort by date (newest first)
                _allNotifications = _allNotifications.OrderByDescending(n => n.BookingTime).ToList();

                // Apply filter
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading notifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            var filtered = _allNotifications.ToList();

            // Apply type filter
            var selectedFilter = (CbSortFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

            if (selectedFilter == "Matching Ads")
            {
                filtered = filtered.Where(n => n.NotificationType == "AD_MATCHED").ToList();
            }
            else if (selectedFilter == "Offers")
            {
                filtered = filtered.Where(n => n.AdType.Equals("offering", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (selectedFilter == "Requests")
            {
                filtered = filtered.Where(n => n.AdType.Equals("seeking", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Map to display model
            var displayNotifications = filtered.Select(n =>
            {
                if (n.NotificationType == "AD_MATCHED")
                {
                    return new NotificationDisplayModel
                    {
                        Id = n.Id,
                        NotificationTitle = $"{n.Category} - {(n.AdType == "offering" ? "Offering" : "Seeking")}",
                        NotificationMessage = "✓ A matching ad appeared",
                        BookingTime = n.BookingTime,
                        BookingDate = n.BookingDate,
                        NotificationType = "AD_MATCHED",
                        IsMatchingAd = true
                    };
                }
                else
                {
                    return new NotificationDisplayModel
                    {
                        Id = n.Id,
                        NotificationTitle = $"{n.Category} - {(n.AdType == "offering" ? "Offering" : "Seeking")}",
                        NotificationMessage = $"{n.BookedByName} scheduled an appointment",
                        BookingTime = n.BookingTime,
                        BookingDate = n.BookingDate,
                        NotificationType = "SLOT_BOOKED",
                        IsMatchingAd = false
                    };
                }
            }).ToList();

            if (displayNotifications.Count == 0)
            {
                NotificationsListControl.Visibility = Visibility.Collapsed;
                EmptyStateMessage.Visibility = Visibility.Visible;
            }
            else
            {
                NotificationsListControl.ItemsSource = displayNotifications;
                NotificationsListControl.Visibility = Visibility.Visible;
                EmptyStateMessage.Visibility = Visibility.Collapsed;
            }
        }

        private void CbSortFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var noticeBoardWindow = new NoticeBoardWindow(_tenantId, _buildingId, _helpAdService);
            noticeBoardWindow.Show();
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

        private async void NotificationItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is long notificationId)
            {
                // Find the notification in the list
                var notification = _allNotifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification == null)
                    return;

                // If AD_MATCHED notification, open ScheduleAppointmentWindow
                if (notification.NotificationType == "AD_MATCHED")
                {
                    try
                    {
                        // Open the schedule appointment window for this matching ad
                        var scheduleWindow = new ScheduleAppointmentWindow(
                            _tenantId,
                            _buildingId,
                            notification.HelpAdId,
                            _helpAdService);

                        scheduleWindow.Show();

                        // Mark notification as read
                        await _notificationRepository.MarkAsReadAsync(notification.Id);

                        // Close this window
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening schedule window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // For SLOT_BOOKED notifications
                // If clicking the same notification, toggle details (hide them)
                if (_selectedNotificationId == notificationId)
                {
                    // Hide details
                    DetailsPanel.Visibility = Visibility.Collapsed;
                    EmptyDetailsMessage.Visibility = Visibility.Visible;
                    _selectedNotificationId = -1;
                    return;
                }

                _selectedNotificationId = notificationId;

                // Show details panel
                DetailsPanel.Visibility = Visibility.Visible;
                EmptyDetailsMessage.Visibility = Visibility.Collapsed;

                // Populate details
                DetailTitle.Text = notification.Category + (notification.AdType == "offering" ? " - Offering" : " - Seeking");
                DetailMessage.Text = notification.BookedByName + " scheduled an appointment";
                DetailTime.Text = notification.BookingTime.ToString("HH:mm");
                DetailDate.Text = notification.BookingDate.ToString("dd/MM/yyyy");
                DetailTimeValue.Text = notification.BookingTime.ToString("HH:mm");

                // Load and display the requesting ad
                await LoadRequestingAdAsync(notification.HelpAdId);
            }
        }

        private async Task LoadRequestingAdAsync(long helpAdId)
        {
            try
            {
                // Get the ad to find bookings
                var bookings = await _bookingRepository.GetBookingsForAdAsync(helpAdId);

                if (bookings.Count > 0)
                {
                    // Get the description from the first booking's requesting ad
                    // In a real scenario, you might want to show all descriptions if there are multiple bookings
                    DetailRequestingAd.Text = bookings[0].RequestingAdDescription;
                }
                else
                {
                    DetailRequestingAd.Text = "No requesting ad information available.";
                }
            }
            catch (Exception ex)
            {
                DetailRequestingAd.Text = $"Error loading request information: {ex.Message}";
            }
        }
    }

    // Display model for notifications
    public class NotificationDisplayModel
    {
        public long Id { get; set; }
        public string NotificationTitle { get; set; } = string.Empty;
        public string NotificationMessage { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }
        public DateTime BookingDate { get; set; }
        public string NotificationType { get; set; } = "SLOT_BOOKED";
        public bool IsMatchingAd { get; set; }
    }

    // Internal model for processing
    public class HelpAdNotification
    {
        public long Id { get; set; }
        public long HelpAdId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string AdType { get; set; } = string.Empty;
        public string BookedByName { get; set; } = string.Empty;
        public DateTime BookingTime { get; set; }
        public DateTime BookingDate { get; set; }
        public string NotificationType { get; set; } = "SLOT_BOOKED"; // SLOT_BOOKED or AD_MATCHED
    }

    // Value converter for notification colors
    public class NotificationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMatchingAd && isMatchingAd)
            {
                return new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)); // Green for matching ads
            }
            return new SolidColorBrush(Color.FromArgb(255, 102, 102, 102)); // Gray for others
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
