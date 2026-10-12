using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.ViewModels.ManagerViewModels
{
    public class ManageBuildingRequestsViewModel : ViewModelBase
    {
        private readonly BuildingRequestService _requestService;
        private readonly long _managerId;

        private ObservableCollection<BuildingRequestDTO> _requests = new();
        public ObservableCollection<BuildingRequestDTO> Requests
        {
            get => _requests;
            set { _requests = value; OnPropertyChanged(); }
        }

        private RequestStatus? _selectedStatusFilter;
        public RequestStatus? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); LoadRequests(); }
        }

        private bool _sortAscending;
        public bool SortAscending
        {
            get => _sortAscending;
            set { _sortAscending = value; OnPropertyChanged(); LoadRequests(); }
        }

        public ManageBuildingRequestsViewModel()
        {
            _requestService = new BuildingRequestService();
            _managerId = UserSession.CurrentUser?.Id ?? 0;
        }

        public void LoadRequests()
        {
            var requests = _requestService.GetManagerRequests(_managerId, _selectedStatusFilter, _sortAscending);
            Requests = new ObservableCollection<BuildingRequestDTO>(requests);
        }

        public void ApproveRequest(long requestId)
        {
            _requestService.ApproveRequest(_managerId, requestId);
            LoadRequests();
        }

        public void RejectRequest(long requestId, string reason)
        {
            _requestService.RejectRequest(_managerId, requestId, reason);
            LoadRequests();
        }
    }
}
