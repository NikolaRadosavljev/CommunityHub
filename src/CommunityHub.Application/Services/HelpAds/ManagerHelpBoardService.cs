using System;
using System.Collections.Generic;
using System.Linq;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Services.HelpAds.DTO;

namespace CommunityHub.Application.Services.HelpAds
{
    public class ManagerHelpBoardService
    {
        private readonly HelpBoardRepository _helpBoardRepository;

        public ManagerHelpBoardService()
        {
            _helpBoardRepository = new HelpBoardRepository();
        }

        public List<ManagerHelpAdOverviewDTO> GetAdsForBuilding(long managerId, int buildingId)
        {
            return _helpBoardRepository.GetManagerAdsForBuilding(managerId, buildingId);
        }

        public ManagerHelpBoardStatisticsDTO GetStatisticsForBuilding(long managerId, int buildingId, int year, int? month)
        {
            List<ManagerHelpAdOverviewDTO> allAds = _helpBoardRepository.GetManagerAdsForBuilding(managerId, buildingId);

            IEnumerable<ManagerHelpAdOverviewDTO> periodAds = allAds.Where(ad => ad.StartDate.Year == year);
            if (month.HasValue)
            {
                periodAds = periodAds.Where(ad => ad.StartDate.Month == month.Value);
            }

            int offeringCreatedCount = periodAds.Count(ad => ad.Type.Equals("offering", StringComparison.OrdinalIgnoreCase));
            int seekingCreatedCount = periodAds.Count(ad => ad.Type.Equals("seeking", StringComparison.OrdinalIgnoreCase));

            // Aktivni i archivirani oglasi se broje za TRENUTNO STANJE (bez obzira na periodu)
            // jer se radi o sadašnjem stanju, ne o istoriji
            int activeCount = allAds.Count(ad => ad.IsCurrentlyActive);
            int archivedCount = allAds.Count(ad => ad.Status.Equals("ARCHIVED", StringComparison.OrdinalIgnoreCase));

            List<ManagerCategorySplitCountDTO> categorySplitCounts =
                _helpBoardRepository.GetCategorySplitCountsForPeriod(managerId, buildingId, year, month);

            List<ManagerCategoryActiveCountDTO> activeByCategory =
                _helpBoardRepository.GetActiveCountsByCategory(managerId, buildingId);

            ManagerMostHelpfulTenantDTO? mostHelpfulTenant = _helpBoardRepository.GetMostHelpfulTenant(managerId, buildingId);

            return new ManagerHelpBoardStatisticsDTO
            {
                OfferingCreatedCount = offeringCreatedCount,
                SeekingCreatedCount = seekingCreatedCount,
                ActiveCount = activeCount,
                ArchivedCount = archivedCount,
                CategorySplitCounts = categorySplitCounts,
                ActiveByCategory = activeByCategory,
                MostHelpfulTenant = mostHelpfulTenant
            };
        }
    }
}
