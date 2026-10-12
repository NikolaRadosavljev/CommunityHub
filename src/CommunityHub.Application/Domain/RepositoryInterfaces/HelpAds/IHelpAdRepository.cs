using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Application.Domain.RepositoryInterfaces.HelpAds
{
    public interface IHelpAdRepository
    {
        Task<HelpAdDTO> CreateHelpAd(CreateHelpAdDTO createHelpAdDTO);
        Task<List<HelpAdDTO>> GetHelpAdsForBuilding(long buildingId);
        Task CreateHelpAdSlots(List<HelpAdSlotDTO> slots);
        Task<HelpAdDTO> GetHelpAdById(long helpAdId);
        Task<List<HelpAdSlotDTO>> GetSlotsForAd(long helpAdId);
        Task ArchiveHelpAd(long helpAdId);
        string GetConnectionString();
    }
}
    