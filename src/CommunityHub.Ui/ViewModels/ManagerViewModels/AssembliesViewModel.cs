using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.ViewModels.ManagerViewModels
{
    public class AssembliesViewModel : ViewModelBase
    {
        private readonly IAssemblyService _assemblyService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly long _managerId;
        private readonly int _buildingId;

        private ObservableCollection<HomeownersMeetingDTO> _meetings = new();
        public ObservableCollection<HomeownersMeetingDTO> Meetings
        {
            get => _meetings;
            set { _meetings = value; OnPropertyChanged(); }
        }

        private Dictionary<long, string> _buildingAddressMap = new();
        public Dictionary<long, string> BuildingAddressMap
        {
            get => _buildingAddressMap;
            set { _buildingAddressMap = value; OnPropertyChanged(); }
        }

        public AssembliesViewModel(int buildingId = 0)
        {
            _assemblyService = ManagerDependencyInjector.CreateInstance<IAssemblyService>();
            _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
            _managerId = UserSession.CurrentUser?.Id ?? 0;
            _buildingId = buildingId;
        }

        public async Task LoadAssembliesAsync()
        {
            var buildings = _buildingRepository.GetByManagerId(_managerId);
            var addressMap = new Dictionary<long, string>();
            foreach (var b in buildings)
            {
                addressMap[b.Id] = b.Address;
            }
            BuildingAddressMap = addressMap;

            var allMeetings = new List<HomeownersMeetingDTO>();

            if (_buildingId > 0)
            {
                var meetings = await _assemblyService.GetAssembliesForBuildingAsync(_buildingId, _managerId);
                allMeetings.AddRange(meetings);
            }
            else
            {
                foreach (var building in buildings)
                {
                    var meetings = await _assemblyService.GetAssembliesForBuildingAsync(building.Id, _managerId);
                    allMeetings.AddRange(meetings);
                }
            }

            Meetings = new ObservableCollection<HomeownersMeetingDTO>(
                allMeetings.OrderByDescending(m => m.ScheduledDate));
        }
    }
}
