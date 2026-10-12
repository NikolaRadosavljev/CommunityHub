using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class ScheduleAppointmentWindow : Window
    {
        private static readonly string ConnectionString = CommunityHub.Application.Database.PostgresConnection.GetConnectionString();

        private readonly long _tenantId;
        private readonly long _buildingId;
        private readonly long _helpAdId;
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdBookingFacadeService _bookingFacade;
        private readonly UserDbRepository _userRepository = new();

        private HelpAdDTO? _selectedAd;
        private List<HelpAdSlotDTO> _allSlots = new();
        private List<HelpAdSlotDTO> _slotsForSelectedDate = new();
        private bool _isInitializing = true;

        public ScheduleAppointmentWindow(long tenantId, long buildingId, long helpAdId, IHelpAdService helpAdService)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;
            _helpAdId = helpAdId;
            _helpAdService = helpAdService;

            _bookingFacade = new HelpAdBookingFacadeService(
                _helpAdService,
                new HelpAdBookingRepository(ConnectionString),
                new HelpAdNotificationRepository(ConnectionString),
                ConnectionString);

            Loaded += async (s, e) => await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                _selectedAd = await _helpAdService.GetHelpAdByIdAsync(_helpAdId);
                if (_selectedAd == null)
                {
                    ShowError("This ad is no longer available.");
                    return;
                }

                PopulateAdSummary(_selectedAd);

                // Load all available slots to get the actual working hours
                _allSlots = await _bookingFacade.GetAvailableSlotsAsync(_helpAdId);

                // Display the actual working hours from the slots
                if (_allSlots.Count > 0)
                {
                    var minTime = _allSlots.Min(s => s.StartTime);
                    var maxTime = _allSlots.Max(s => s.EndTime);
                    DisplayFreeAppointmentTimes(minTime, maxTime);
                }

                // Default to the ad's start date (or today if start is in the past).
                var initialDate = _selectedAd.StartDate.Date < DateTime.Now.Date
                    ? DateTime.Now.Date
                    : _selectedAd.StartDate.Date;

                DpAppointmentDate.DisplayDateStart = _selectedAd.StartDate.Date < DateTime.Now.Date
                    ? DateTime.Now.Date
                    : _selectedAd.StartDate.Date;
                DpAppointmentDate.DisplayDateEnd = _selectedAd.EndDate.Date;

                // Setting SelectedDate triggers DpAppointmentDate_SelectedDateChanged,
                // which loads the available slots — no explicit follow-up call needed.
                DpAppointmentDate.SelectedDate = initialDate;
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load ad: {ex.Message}");
                _isInitializing = false;
            }
        }

        private void DisplayFreeAppointmentTimes(TimeSpan startTime, TimeSpan endTime)
        {
            // Create a readable range text, e.g. "13:00 - 20:00"
            var timeRangeText = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";

            // Simply update the named TextBlock
            TxtFreeAppointmentTimes.Text = timeRangeText;
        }

        private void PopulateAdSummary(HelpAdDTO ad)
        {
            TxtTypeBadge.Text = string.Equals(ad.Type, "offering", StringComparison.OrdinalIgnoreCase) ? "offers" : "request";
            TxtCategory.Text = ad.Category;
            TxtStartDate.Text = ad.StartDate.ToString("dd/MM/yyyy");
            TxtEndDate.Text = ad.EndDate.ToString("dd/MM/yyyy");
            TxtDescription.Text = ad.Description;
            TxtTenantName.Text = GetTenantName(ad.TenantId);
        }

        private string GetTenantName(long tenantId)
        {
            try
            {
                var user = _userRepository.GetById(tenantId);
                if (user != null)
                {
                    var fullName = $"{user.Name} {user.Surname}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
                }
            }
            catch
            {
                // fall through to default
            }
            return $"Tenant #{tenantId}";
        }

        private async void DpAppointmentDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // During initialization, we're setting the date which triggers this event.
            // Only load slots if initialization is complete.
            if (!_isInitializing && DpAppointmentDate.SelectedDate is DateTime date)
            {
                await LoadSlotsForDateAsync(date.Date);
            }
        }

        private async Task LoadSlotsForDateAsync(DateTime date)
        {
            SlotsPanel.Children.Clear();
            ErrorMessage.Visibility = Visibility.Collapsed;

            try
            {
                // Use cached slots if available, otherwise fetch them
                var allAvailable = _allSlots.Count > 0 ? _allSlots : await _bookingFacade.GetAvailableSlotsAsync(_helpAdId);

                _slotsForSelectedDate = allAvailable
                    .Where(s => s.SlotDate.Date == date.Date)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (_slotsForSelectedDate.Count == 0)
                {
                    NoSlotsMessage.Visibility = Visibility.Visible;
                    return;
                }

                NoSlotsMessage.Visibility = Visibility.Collapsed;

                foreach (var slot in _slotsForSelectedDate)
                {
                    var cb = new CheckBox
                    {
                        Content = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
                        Tag = slot.Id,
                        Margin = new Thickness(0, 0, 0, 8),
                        FontSize = 13
                    };
                    SlotsPanel.Children.Add(cb);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load free slots: {ex.Message}");
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            if (_selectedAd == null)
            {
                ShowError("Ad is not loaded.");
                return;
            }

            // Get all checked slots
            var selectedSlots = SlotsPanel.Children.OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true && cb.Tag is long)
                .Select(cb => (long)cb.Tag)
                .ToList();

            if (selectedSlots.Count == 0)
            {
                ShowError("Please choose at least one time slot.");
                return;
            }

            try
            {
                // Per spec, scheduling on the notice board links the slot back to a matching ad
                // the booking tenant has created (opposite type, same category, overlapping dates).
                var requestingAdId = await FindMatchingAdForCurrentTenantAsync(_selectedAd);
                if (requestingAdId == null)
                {
                    var oppositeType = string.Equals(_selectedAd.Type, "offering", StringComparison.OrdinalIgnoreCase)
                        ? "seeking"
                        : "offering";
                    ShowError($"To schedule this appointment you first need an active '{oppositeType}' ad "
                              + $"in the same category ('{_selectedAd.Category}') with an overlapping date range. "
                              + "Create one from the notice board and try again.");
                    return;
                }

                await _bookingFacade.BookSlotsAsync(
                    offeredHelpAdId: _selectedAd.Id,
                    requestingHelpAdId: requestingAdId.Value,
                    bookingTenantId: _tenantId,
                    slotIds: selectedSlots);

                MessageBox.Show($"Appointment scheduled successfully ({selectedSlots.Count} slot(s))!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigateBackToNoticeBoard();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to schedule appointment: {ex.Message}");
            }
        }

        private async Task<long?> FindMatchingAdForCurrentTenantAsync(HelpAdDTO offeredAd)
        {
            var allAdsInBuilding = await _helpAdService.GetHelpAdsForBuildingAsync(_buildingId);
            var oppositeType = string.Equals(offeredAd.Type, "offering", StringComparison.OrdinalIgnoreCase)
                ? "seeking"
                : "offering";

            var matching = allAdsInBuilding
                .Where(a =>
                    a.TenantId == _tenantId &&
                    string.Equals(a.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.Type, oppositeType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.Category, offeredAd.Category, StringComparison.OrdinalIgnoreCase) &&
                    a.StartDate.Date <= offeredAd.EndDate.Date &&
                    a.EndDate.Date >= offeredAd.StartDate.Date)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefault();

            return matching?.Id;
        }

        private void NavigateBackToNoticeBoard()
        {
            var noticeBoard = new NoticeBoardWindow(_tenantId, _buildingId, _helpAdService);
            noticeBoard.Show();
            Close();
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, _buildingId, _helpAdService);
            notificationsWindow.Show();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToNoticeBoard();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            Close();
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
