using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Application.Services.HelpAds
{
    public interface IHelpAdService
    {
        Task<HelpAdDTO> CreateHelpAdAsync(CreateHelpAdDTO createHelpAdDTO);
        Task<List<HelpAdDTO>> GetHelpAdsForBuildingAsync(long buildingId);
        Task<HelpAdDTO> GetHelpAdByIdAsync(long helpAdId);
        Task<List<HelpAdSlotDTO>> GetAvailableSlotsForAdAsync(long helpAdId);
        Task ArchiveHelpAdAsync(long helpAdId);
    }
}
