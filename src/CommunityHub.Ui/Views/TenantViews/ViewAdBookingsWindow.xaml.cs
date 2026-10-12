using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class ViewAdBookingsWindow : Window
    {
        private static readonly string ConnectionString = CommunityHub.Application.Database.PostgresConnection.GetConnectionString();

        private readonly long _tenantId;
        private readonly long _buildingId;
        private readonly long _helpAdId;
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdBookingRepository _bookingRepository;
        private readonly UserDbRepository _userRepository = new();

        public ViewAdBookingsWindow(long tenantId, long buildingId, long helpAdId, IHelpAdService helpAdService)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingId = buildingId;
            _helpAdId = helpAdId;
            _helpAdService = helpAdService;
            _bookingRepository = new HelpAdBookingRepository(ConnectionString);

            Loaded += async (s, e) => await LoadBookingsAsync();
        }

        private async Task LoadBookingsAsync()
        {
            try
            {
                // Load the ad details
                var ad = await _helpAdService.GetHelpAdByIdAsync(_helpAdId);
                if (ad != null)
                {
                    TxtAdDescription.Text = ad.Description;
                    TxtCategory.Text = ad.Category;
                }

                // Load all bookings for this ad
                var bookings = await _bookingRepository.GetBookingsForAdAsync(_helpAdId);

                if (bookings == null || bookings.Count == 0)
                {
                    BookingsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                    return;
                }

                // Load all slots to get detailed slot information
                var allSlots = await _helpAdService.GetAvailableSlotsForAdAsync(_helpAdId);

                // Get reserved slots from bookings
                var reservedSlotIds = new HashSet<long>();
                foreach (var booking in bookings)
                {
                    // We need to get the actual slots that were booked
                    // The booking has SlotsCount, so we need to find which slots
                }

                // Create display models with full booking details
                var displayBookings = new List<BookingDisplayModel>();

                foreach (var booking in bookings)
                {
                    // Get the slots for this booking
                    var bookingSlots = await GetBookingSlotsAsync(booking.BookingId);

                    foreach (var slot in bookingSlots)
                    {
                        displayBookings.Add(new BookingDisplayModel
                        {
                            BookingId = booking.BookingId,
                            TenantName = booking.BookingTenantName,
                            RequestingAdDescription = booking.RequestingAdDescription,
                            SlotDate = slot.SlotDate,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            SlotDateDisplay = slot.SlotDate.ToString("dd/MM/yyyy"),
                            SlotTimeDisplay = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}"
                        });
                    }
                }

                if (displayBookings.Count == 0)
                {
                    BookingsListControl.Visibility = Visibility.Collapsed;
                    EmptyStateMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    BookingsListControl.ItemsSource = displayBookings.OrderBy(b => b.SlotDate).ThenBy(b => b.StartTime).ToList();
                    BookingsListControl.Visibility = Visibility.Visible;
                    EmptyStateMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading bookings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<List<HelpAdSlotDTO>> GetBookingSlotsAsync(long bookingId)
        {
            try
            {
                // Query the database to get the slots for this booking
                using (var connection = new Npgsql.NpgsqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                        SELECT s.id, s.help_ad_id, s.slot_date, s.start_time, s.end_time
                        FROM help_ad_slots s
                        JOIN help_ad_slot_bookings sb ON s.id = sb.slot_id
                        WHERE sb.booking_id = @bookingId
                        ORDER BY s.slot_date, s.start_time";

                    using (var command = new Npgsql.NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@bookingId", bookingId);

                        var slots = new List<HelpAdSlotDTO>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                slots.Add(new HelpAdSlotDTO
                                {
                                    Id = reader.GetInt64(0),
                                    HelpAdId = reader.GetInt64(1),
                                    SlotDate = reader.GetDateTime(2),
                                    StartTime = reader.GetTimeSpan(3),
                                    EndTime = reader.GetTimeSpan(4)
                                });
                            }
                        }
                        return slots;
                    }
                }
            }
            catch
            {
                return new List<HelpAdSlotDTO>();
            }
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class BookingDisplayModel
    {
        public long BookingId { get; set; }
        public string TenantName { get; set; }
        public string RequestingAdDescription { get; set; }
        public DateTime SlotDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string SlotDateDisplay { get; set; }
        public string SlotTimeDisplay { get; set; }
    }
}
