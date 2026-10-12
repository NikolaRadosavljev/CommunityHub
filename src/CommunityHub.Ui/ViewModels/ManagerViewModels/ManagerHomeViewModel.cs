using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.ViewModels.ManagerViewModels
{
    public class BuildingDashboardItem
    {
        public int Id { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Settlement { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int NumberOfFlats { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }

    public class ManagerHomeViewModel : ViewModelBase
    {
        private readonly IBuildingRepository _buildingRepository;
        private readonly BuildingRequestDbRepository _requestRepository;
        private readonly ICommonSpaceRentalRequestService _rentalRequestService;
        private readonly IAssemblyService _assemblyService;
        private readonly IProblemRepository _problemRepository;
        private readonly long _managerId;

        private int _membershipRequestCount;
        public int MembershipRequestCount
        {
            get => _membershipRequestCount;
            set { _membershipRequestCount = value; OnPropertyChanged(); }
        }

        private int _rentalRequestCount;
        public int RentalRequestCount
        {
            get => _rentalRequestCount;
            set { _rentalRequestCount = value; OnPropertyChanged(); }
        }

        private int _assembliesCount;
        public int AssembliesCount
        {
            get => _assembliesCount;
            set { _assembliesCount = value; OnPropertyChanged(); }
        }

        private int _problemsCount;
        public int ProblemsCount
        {
            get => _problemsCount;
            set { _problemsCount = value; OnPropertyChanged(); }
        }

        private ObservableCollection<BuildingDashboardItem> _buildings = new();
        public ObservableCollection<BuildingDashboardItem> Buildings
        {
            get => _buildings;
            set { _buildings = value; OnPropertyChanged(); }
        }

        public ManagerHomeViewModel()
        {
            _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
            _requestRepository = ManagerDependencyInjector.CreateInstance<BuildingRequestDbRepository>();
            _rentalRequestService = ManagerDependencyInjector.CreateInstance<ICommonSpaceRentalRequestService>();
            _assemblyService = ManagerDependencyInjector.CreateInstance<IAssemblyService>();
            _problemRepository = ManagerDependencyInjector.CreateInstance<IProblemRepository>();
            _managerId = UserSession.CurrentUser?.Id ?? 0;
        }

        public async Task LoadDashboardAsync()
        {
            await Task.Run(() =>
            {
                var buildings = _buildingRepository.GetByManagerId(_managerId);
                var buildingIds = buildings.Select(b => b.Id).ToList();

                var pendingRequests = _requestRepository.GetForManager(_managerId, RequestStatus.Pending, false);
                MembershipRequestCount = pendingRequests.Count;

                var pendingRentals = _rentalRequestService.GetPendingRequests(buildingIds);
                RentalRequestCount = pendingRentals.Count;

                var dashboardItems = new ObservableCollection<BuildingDashboardItem>();
                int totalAssemblies = 0;
                int totalProblems = 0;

                foreach (var building in buildings)
                {
                    int occupancy = _buildingRepository.GetOccupancyCount(building.Id);
                    int totalFlats = building.FlatNumbers?.Count ?? 0;

                    string statusText = occupancy == totalFlats ? "Popunjeno" : $"{occupancy}/{totalFlats}";
                    string statusColor = occupancy == totalFlats ? "#4CAF50" : "#FF9800";

                    dashboardItems.Add(new BuildingDashboardItem
                    {
                        Id = building.Id,
                        Address = building.Address,
                        Settlement = building.Settlement,
                        City = building.City,
                        FloorNumber = building.FloorNumber,
                        NumberOfFlats = totalFlats,
                        StatusText = statusText,
                        StatusColor = statusColor
                    });

                    var assemblies = _assemblyService.GetAssembliesForBuildingAsync(building.Id, _managerId).Result;
                    totalAssemblies += assemblies.Count;

                    var problems = _problemRepository.GetProblemsByBuildingAsync(building.Id).Result;
                    totalProblems += problems.Count(p => p.Status != "Resolved");
                }

                AssembliesCount = totalAssemblies;
                ProblemsCount = totalProblems;
                Buildings = dashboardItems;
            });
        }
    }
}
