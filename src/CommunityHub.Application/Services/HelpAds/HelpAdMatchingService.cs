using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Application.Services.HelpAds
{
    public class HelpAdMatchingService
    {
        private readonly IHelpAdService _helpAdService;
        private readonly HelpAdBookingRepository _bookingRepository;
        private readonly HelpAdNotificationRepository _notificationRepository;

        public HelpAdMatchingService(IHelpAdService helpAdService, 
            HelpAdBookingRepository bookingRepository,
            HelpAdNotificationRepository notificationRepository)
        {
            _helpAdService = helpAdService;
            _bookingRepository = bookingRepository;
            _notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Find matching help ads based on category, date range, and type (offering/seeking)
        /// </summary>
        public async Task<List<MatchingHelpAdDTO>> FindMatchingAdsAsync(long helpAdId, long buildingId)
        {
            try
            {
                // Get the help ad that's looking for matches
                var helpAd = await _helpAdService.GetHelpAdByIdAsync(helpAdId);
                if (helpAd == null || string.Equals(helpAd.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase))
                    return new List<MatchingHelpAdDTO>();

                // Determine what we're looking for based on the ad type
                string targetType = helpAd.Type == "seeking" ? "offering" : "seeking";

                // Get all ads for the building
                var allAds = await _helpAdService.GetHelpAdsForBuildingAsync(buildingId);

                // Filter matching ads
                var matchingAds = allAds.Where(ad =>
                    ad.Id != helpAdId // Don't include the original ad
                    && ad.Type == targetType // Opposite type (seeking/offering)
                    && ad.Category == helpAd.Category // Same category
                    && !string.Equals(ad.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) // Not archived
                    && DateRangesOverlap(helpAd.StartDate, helpAd.EndDate, ad.StartDate, ad.EndDate)
                ).ToList();

            // Convert to matching DTOs with available slots info
            var result = new List<MatchingHelpAdDTO>();
            foreach (var ad in matchingAds)
            {
                var slots = await _helpAdService.GetAvailableSlotsForAdAsync(ad.Id);

                // Filter slots to only include those within the current ad's date range
                var compatibleSlots = slots.Where(slot =>
                    slot.SlotDate.Date >= helpAd.StartDate.Date &&
                    slot.SlotDate.Date <= helpAd.EndDate.Date
                ).ToList();

                if (compatibleSlots.Any())
                {
                    result.Add(new MatchingHelpAdDTO
                    {
                        HelpAdId = ad.Id,
                        TenantId = ad.TenantId,
                        Description = ad.Description,
                        Category = ad.Category,
                        Type = ad.Type,
                        StartDate = ad.StartDate,
                        EndDate = ad.EndDate,
                        AvailableSlots = compatibleSlots
                    });
                }
            }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in FindMatchingAdsAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Book slots from a matching help ad
        /// </summary>
        public async Task<long> BookSlotsAsync(long offeredHelpAdId, long requestingHelpAdId, 
            long bookingTenantId, List<long> slotIds)
        {
            if (!slotIds.Any())
                throw new ArgumentException("At least one slot must be selected");

            // Get the offered ad info (the one being booked from)
            var offeredAd = await _helpAdService.GetHelpAdByIdAsync(offeredHelpAdId);
            if (offeredAd == null)
                throw new InvalidOperationException("Offered help ad not found");

            // Create the booking
            var createBookingDto = new CreateHelpAdBookingDTO
            {
                OfferedHelpAdId = offeredHelpAdId,
                RequestingHelpAdId = requestingHelpAdId,
                BookingTenantId = bookingTenantId,
                SlotIds = slotIds
            };

            long bookingId = await _bookingRepository.CreateBookingAsync(createBookingDto);

            // Create notification for the offering tenant
            await _notificationRepository.CreateNotificationAsync(
                tenantId: offeredAd.TenantId,
                helpAdId: offeredHelpAdId,
                bookingId: bookingId,
                bookingTenantId: bookingTenantId,
                requestingHelpAdId: requestingHelpAdId,
                slotsCount: slotIds.Count
            );

            return bookingId;
        }

        /// <summary>
        /// Check if two date ranges overlap
        /// </summary>
        private bool DateRangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1.Date <= end2.Date && end1.Date >= start2.Date;
        }
    }

    /// <summary>
    /// DTO for displaying matching help ads with their available slots
    /// </summary>
    public class MatchingHelpAdDTO
    {
        public long HelpAdId { get; set; }
        public long TenantId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<HelpAdSlotDTO> AvailableSlots { get; set; } = new();
    }
}