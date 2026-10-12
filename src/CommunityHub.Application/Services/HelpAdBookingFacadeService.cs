using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Domain.RepositoryInterfaces.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Application.Services
{
    /// <summary>
    /// Facade service for help ad booking operations
    /// Can be used by WPF frontend or future web API
    /// </summary>
    public class HelpAdBookingFacadeService
    {
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdBookingRepository _bookingRepository;
        private readonly HelpAdNotificationRepository _notificationRepository;
        private readonly HelpAdMatchingService _matchingService;
        private readonly string _connectionString;

        public HelpAdBookingFacadeService(
            IHelpAdService helpAdService,
            HelpAdBookingRepository bookingRepository,
            HelpAdNotificationRepository notificationRepository,
            string connectionString)
        {
            _helpAdService = helpAdService;
            _bookingRepository = bookingRepository;
            _notificationRepository = notificationRepository;
            _matchingService = new HelpAdMatchingService(helpAdService, bookingRepository, notificationRepository);
            _connectionString = connectionString;
        }

        /// <summary>
        /// Find all matching help ads for a given ad with available slots
        /// </summary>
        public async Task<List<MatchingHelpAdDTO>> FindMatchingAdsAsync(long helpAdId, long buildingId)
        {
            try
            {
                return await _matchingService.FindMatchingAdsAsync(helpAdId, buildingId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error finding matching ads: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Book one or more time slots from a matching help ad
        /// </summary>
        public async Task<long> BookSlotsAsync(long offeredHelpAdId, long requestingHelpAdId, 
            long bookingTenantId, List<long> slotIds)
        {
            if (slotIds == null || !slotIds.Any())
                throw new ArgumentException("At least one slot must be selected");

            if (slotIds.Count == 0)
                throw new ArgumentException("At least one slot must be selected");

            try
            {
                return await _matchingService.BookSlotsAsync(offeredHelpAdId, requestingHelpAdId, 
                    bookingTenantId, slotIds);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error booking slots: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all bookings (reservations) for a given help ad
        /// Shows who booked what slots and links to their ads
        /// </summary>
        public async Task<List<HelpAdBookingDetailDTO>> GetBookingsForAdAsync(long helpAdId)
        {
            try
            {
                return await _bookingRepository.GetBookingsForAdAsync(helpAdId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving bookings: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all unread notifications for a tenant
        /// </summary>
        public async Task<List<HelpAdNotificationDTO>> GetUnreadNotificationsAsync(long tenantId)
        {
            try
            {
                return await _notificationRepository.GetUnreadNotificationsAsync(tenantId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving notifications: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all notifications (read and unread) for a tenant
        /// </summary>
        public async Task<List<HelpAdNotificationDTO>> GetAllNotificationsAsync(long tenantId)
        {
            try
            {
                return await _notificationRepository.GetAllNotificationsAsync(tenantId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving notifications: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        public async Task MarkNotificationAsReadAsync(long notificationId)
        {
            try
            {
                await _notificationRepository.MarkAsReadAsync(notificationId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating notification: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cancel a booking
        /// </summary>
        public async Task CancelBookingAsync(long bookingId)
        {
            try
            {
                await _bookingRepository.CancelBookingAsync(bookingId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error canceling booking: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Archive a help ad (hides from suggestions but keeps booking history)
        /// </summary>
        public async Task ArchiveAdAsync(long helpAdId)
        {
            try
            {
                await _helpAdService.ArchiveHelpAdAsync(helpAdId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error archiving ad: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all available time slots for a help ad
        /// (slots that haven't been booked yet)
        /// </summary>
        public async Task<List<HelpAdSlotDTO>> GetAvailableSlotsAsync(long helpAdId)
        {
            try
            {
                return await _helpAdService.GetAvailableSlotsForAdAsync(helpAdId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving available slots: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get all slots for a help ad (including booked ones)
        /// </summary>
        public async Task<List<HelpAdSlotDTO>> GetAllSlotsAsync(long helpAdId)
        {
            var repo = new HelpAdDbRepository(_connectionString);
            try
            {
                return await repo.GetSlotsForAd(helpAdId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving slots: {ex.Message}", ex);
            }
        }
    }
}
