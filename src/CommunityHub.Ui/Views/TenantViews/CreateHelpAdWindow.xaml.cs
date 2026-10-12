using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Ui.DependencyInjection;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class CreateHelpAdWindow : Window
    {
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdMatchingService _matchingService;
        private readonly IUserRepository _userRepository;
        private readonly long _buildingId;
        private readonly long _tenantId;
        private long _createdAdId = 0;

        public CreateHelpAdWindow(
            long tenantId,
            long buildingId,
            IHelpAdService? helpAdService = null,
            HelpAdMatchingService? matchingService = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;

            // Koristi prosleđeni servis ili kreiraj default implementaciju
            _helpAdService = helpAdService ?? DIConfiguration.Factory.CreateHelpAdService();
            _matchingService = matchingService ?? DIConfiguration.Factory.CreateHelpAdMatchingService();
            _userRepository = DIConfiguration.Factory.CreateUserRepository();

            DpFromDate.SelectedDate = DateTime.Now.Date;
            DpToDate.SelectedDate = DateTime.Now.Date.AddDays(7);
            CbCategory.SelectedIndex = 0;
        }

        private async void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            var description = TxtDescription.Text?.Trim();
            var category = (CbCategory.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            var startDate = DpFromDate.SelectedDate;
            var endDate = DpToDate.SelectedDate;
            var isOffering = RbOffering.IsChecked ?? true;

            if (string.IsNullOrEmpty(description))
            {
                ShowError("Description is required.");
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                ShowError("Category is required.");
                return;
            }

            if (startDate == null || endDate == null)
            {
                ShowError("Both start and end dates are required.");
                return;
            }

            if (startDate.Value.Date < DateTime.Now.Date)
            {
                ShowError("Start date cannot be in the past.");
                return;
            }

            if (endDate.Value.Date < startDate.Value.Date)
            {
                ShowError("End date must be after or equal to the start date.");
                return;
            }

            // Parse and validate working hours
            if (!TimeSpan.TryParse(TxtStartTime.Text, out var startTime))
            {
                ShowError("Start time format is invalid. Use HH:mm format (e.g., 13:00).");
                return;
            }

            if (!TimeSpan.TryParse(TxtEndTime.Text, out var endTime))
            {
                ShowError("End time format is invalid. Use HH:mm format (e.g., 20:00).");
                return;
            }

            if (startTime >= endTime)
            {
                ShowError("End time must be after start time.");
                return;
            }

            try
            {
                var createDto = new CreateHelpAdDTO
                {
                    BuildingId = _buildingId,
                    TenantId = _tenantId,
                    Description = description!,
                    Category = category!,
                    StartDate = startDate.Value.Date,
                    EndDate = endDate.Value.Date,
                    Type = isOffering ? "offering" : "seeking",
                    StartTime = startTime,
                    EndTime = endTime
                };

                var createdAd = await _helpAdService.CreateHelpAdAsync(createDto);
                _createdAdId = createdAd.Id;

                MessageBox.Show("Ad created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Display matching ads for the newly created ad
                await DisplayMatchingAds(_createdAdId);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to create ad: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToNoticeBoard();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToNoticeBoard();
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }

        private void NavigateBackToNoticeBoard()
        {
            var noticeBoard = new NoticeBoardWindow(_tenantId, _buildingId, _helpAdService);
            noticeBoard.Show();
            Close();
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private async System.Threading.Tasks.Task DisplayMatchingAds(long helpAdId)
        {
            try
            {
                // Find matching ads
                var matchingAds = await _matchingService.FindMatchingAdsAsync(helpAdId, _buildingId);

                // If no matching ads found, just navigate back to notice board
                // The newly created ad will be visible there
                if (matchingAds.Count == 0)
                {
                    NavigateBackToNoticeBoard();
                    return;
                }

                // Prepare display data with formatted slot information
                var displayAds = new List<MatchingAdDisplayViewModel>();
                foreach (var ad in matchingAds)
                {
                    try
                    {
                        // Get tenant name
                        var user = _userRepository.GetById(ad.TenantId);
                        var tenantName = user != null ? $"{user.Name} {user.Surname}".Trim() : $"Tenant #{ad.TenantId}";

                        // Format available slots as free appointments (time ranges)
                        string freeAppointments;
                        string slotsText;

                        if (ad.AvailableSlots == null || ad.AvailableSlots.Count == 0)
                        {
                            freeAppointments = "No available slots";
                            slotsText = "No slots";
                        }
                        else
                        {
                            try
                            {
                                // Show only time range (earliest start to latest end) for each day
                                freeAppointments = string.Join("\n",
                                    ad.AvailableSlots
                                        .GroupBy(s => s.SlotDate.Date)
                                        .Select(g =>
                                        {
                                            var minStartTime = g.Min(s => s.StartTime);
                                            var maxEndTime = g.Max(s => s.EndTime);
                                            return $"{g.Key:dd/MM}: {minStartTime:hh\\:mm} - {maxEndTime:hh\\:mm}";
                                        }));

                                slotsText = string.Join(", ",
                                    ad.AvailableSlots
                                        .GroupBy(s => s.SlotDate.Date)
                                        .Select(g => $"{g.Key:MM/dd}"));
                            }
                            catch (Exception slotEx)
                            {
                                throw new Exception($"Error formatting slots for ad {ad.HelpAdId}: {slotEx.Message}", slotEx);
                            }
                        }

                        displayAds.Add(new MatchingAdDisplayViewModel
                        {
                            HelpAdId = ad.HelpAdId,
                            TenantId = ad.TenantId,
                            Description = ad.Description,
                            Category = ad.Category,
                            Type = ad.Type,
                            StartDate = ad.StartDate,
                            EndDate = ad.EndDate,
                            AvailableSlots = ad.AvailableSlots,
                            SlotsDisplay = slotsText,
                            DateRangeDisplay = $"{ad.StartDate:MM/dd/yyyy} - {ad.EndDate:MM/dd/yyyy}",
                            TenantName = tenantName,
                            TypeBadge = ad.Type == "offering" ? "Offering" : "Seeking",
                            FreeAppointments = freeAppointments
                        });
                    }
                    catch (Exception adEx)
                    {
                        var innerMsg = adEx.InnerException?.Message ?? "";
                        throw new Exception(
                            $"Error processing matching ad {ad.HelpAdId}: {adEx.Message}" +
                            (string.IsNullOrEmpty(innerMsg) ? "" : $" | Inner: {innerMsg}"),
                            adEx);
                    }
                }

                // Sort by ID descending (newest first)
                displayAds = displayAds.OrderByDescending(a => a.HelpAdId).ToList();

                // Display matching ads section
                MatchingAdsList.ItemsSource = displayAds;
                MatchingAdsSection.Visibility = Visibility.Visible;

                // Update the ItemsControl template to bind DateRangeText properly
                UpdateMatchingAdsDisplay(displayAds);
            }
            catch (Exception ex)
            {
                var fullMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    fullMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                MessageBox.Show($"Error finding matching ads:\n{fullMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateBackToNoticeBoard();
            }
        }

        private void UpdateMatchingAdsDisplay(List<MatchingAdDisplayViewModel> displayAds)
        {
            // Since the DataTemplate doesn't directly bind TextBlock.Text with complex formatting,
            // we need to update the TextBlocks manually
            var itemsPanel = MatchingAdsList.Items;
            var itemContainers = MatchingAdsList.ItemsSource as List<MatchingAdDisplayViewModel>;

            // The ItemsControl will automatically bind, but we need to set the template correctly
            // Let's update the display by refreshing the binding
            MatchingAdsList.ItemsSource = null;
            MatchingAdsList.ItemsSource = displayAds;
        }

        private void CloseMatchingAdsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToNoticeBoard();
        }

        private void ScheduleButton_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Get the HelpAdId from the Border's Tag or Button's Tag
                long helpAdId = 0;

                if (sender is Border border && border.Tag is long borderId)
                {
                    helpAdId = borderId;
                }
                else if (sender is Button button && button.Tag is long buttonId)
                {
                    helpAdId = buttonId;
                }

                if (helpAdId > 0)
                {
                    // Open the schedule appointment window for this matching ad
                    var scheduleWindow = new ScheduleAppointmentWindow(
                        _tenantId,
                        _buildingId,
                        helpAdId,
                        _helpAdService);

                    scheduleWindow.Show();

                    // Optionally close the matching ads section or the whole window
                    // For now, just close this window after scheduling
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening schedule window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MatchingAdDisplayViewModel
    {
        public long HelpAdId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<HelpAdSlotDTO> AvailableSlots { get; set; } = new();
        public string SlotsDisplay { get; set; } = string.Empty;
        public string DateRangeDisplay { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string TypeBadge { get; set; } = string.Empty;
        public string FreeAppointments { get; set; } = string.Empty;
    }
}

        