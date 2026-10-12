using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Application.Domain.RepositoryInterfaces.HelpAds;
using CommunityHub.Application.Services.HelpAds;

namespace CommunityHub.Application.Services.HelpAds
{
    public class HelpAdService : IHelpAdService
    {
        private readonly IHelpAdRepository _helpAdRepository;
        private readonly HelpAdNotificationRepository _notificationRepository;

        public HelpAdService(IHelpAdRepository helpAdRepository, HelpAdNotificationRepository notificationRepository = null)
        {
            _helpAdRepository = helpAdRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<HelpAdDTO> CreateHelpAdAsync(CreateHelpAdDTO createHelpAdDTO)
        {
            if (createHelpAdDTO.StartDate.Date < DateTime.Now.Date)
            {
                throw new ArgumentException("Start date cannot be in the past.");
            }

            var helpAd = await _helpAdRepository.CreateHelpAd(createHelpAdDTO);

            var slots = new List<HelpAdSlotDTO>();
            for (var date = createHelpAdDTO.StartDate.Date; date <= createHelpAdDTO.EndDate.Date; date = date.AddDays(1))
            {
                // Generate hourly slots from StartTime to EndTime
                var currentTime = createHelpAdDTO.StartTime;
                while (currentTime < createHelpAdDTO.EndTime)
                {
                    var endTime = currentTime.Add(TimeSpan.FromHours(1));
                    // Don't create a slot that extends beyond EndTime
                    if (endTime > createHelpAdDTO.EndTime)
                        break;

                    slots.Add(new HelpAdSlotDTO
                    {
                        HelpAdId = helpAd.Id,
                        SlotDate = date,
                        StartTime = currentTime,
                        EndTime = endTime
                    });
                    currentTime = currentTime.Add(TimeSpan.FromHours(1));
                }
            }
            await _helpAdRepository.CreateHelpAdSlots(slots);

            // Create notifications for matching ads (2.6 specification)
            if (_notificationRepository != null)
            {
                await CreateMatchingAdNotificationsAsync(helpAd, createHelpAdDTO);
            }

            return helpAd;
        }

        /// <summary>
        /// Create notifications for all existing ads that match the newly created ad
        /// </summary>
        private async Task CreateMatchingAdNotificationsAsync(HelpAdDTO newAd, CreateHelpAdDTO createDto)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[HelpAdService] Creating matching ad notifications for ad {newAd.Id} (Type: {newAd.Type}, Category: {newAd.Category}, Building: {newAd.BuildingId})");

                // Get all ads for the building
                var allAds = await _helpAdRepository.GetHelpAdsForBuilding(newAd.BuildingId);
                System.Diagnostics.Debug.WriteLine($"[HelpAdService] Found {allAds.Count} total ads in building {newAd.BuildingId}");

                // Determine what type we're looking for (opposite of the new ad)
                string targetType = newAd.Type == "seeking" ? "offering" : "seeking";
                System.Diagnostics.Debug.WriteLine($"[HelpAdService] Looking for ads with type: {targetType}");

                // Find ads that match the new ad
                var matchingAds = allAds.Where(ad =>
                    ad.Id != newAd.Id // Don't match with itself
                    && ad.Type == targetType // Opposite type
                    && ad.Category == newAd.Category // Same category
                    && !string.Equals(ad.Status, "ARCHIVED", StringComparison.OrdinalIgnoreCase) // Not archived
                    && DateRangesOverlap(newAd.StartDate, newAd.EndDate, ad.StartDate, ad.EndDate)
                ).ToList();

                System.Diagnostics.Debug.WriteLine($"[HelpAdService] Found {matchingAds.Count} matching ads");

                // Create notifications for each matching ad's owner
                foreach (var matchingAd in matchingAds)
                {
                    System.Diagnostics.Debug.WriteLine($"[HelpAdService] Creating notification for tenant {matchingAd.TenantId} for matching ad {matchingAd.Id}");
                    await _notificationRepository.CreateMatchingAdNotificationAsync(matchingAd.TenantId, newAd.Id);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the ad creation
                System.Diagnostics.Debug.WriteLine($"Error creating matching ad notifications: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Check if two date ranges overlap
        /// </summary>
        private bool DateRangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1.Date <= end2.Date && end1.Date >= start2.Date;
        }

        public async Task<List<HelpAdDTO>> GetHelpAdsForBuildingAsync(long buildingId)
        {
            return await _helpAdRepository.GetHelpAdsForBuilding(buildingId);
        }

        public async Task<HelpAdDTO> GetHelpAdByIdAsync(long helpAdId)
        {
            return await _helpAdRepository.GetHelpAdById(helpAdId);
        }

        public async Task<List<HelpAdSlotDTO>> GetAvailableSlotsForAdAsync(long helpAdId)
        {
            var allSlots = await _helpAdRepository.GetSlotsForAd(helpAdId);

            // Get reserved slot IDs
            var bookingRepo = new HelpAdBookingRepository(_helpAdRepository.GetConnectionString());
            var reservedSlotIds = await bookingRepo.GetReservedSlotsAsync(helpAdId);

            // Filter out reserved slots
            return allSlots.Where(slot => !reservedSlotIds.Contains(slot.Id)).ToList();
        }

        public async Task ArchiveHelpAdAsync(long helpAdId)
        {
            await _helpAdRepository.ArchiveHelpAd(helpAdId);
        }
    }
}