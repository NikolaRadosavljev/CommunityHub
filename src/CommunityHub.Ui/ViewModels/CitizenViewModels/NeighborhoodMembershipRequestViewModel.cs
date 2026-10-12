using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application;
using CommunityHub.Ui.Commands;

namespace CommunityHub.Ui.ViewModels.CitizenViewModels
{
    public class NeighborhoodMembershipRequestViewModel : ViewModelBase
    {
        private readonly long _userId;
        private readonly NeighborhoodMembershipRequestService _requestService;
        private List<NeighborhoodMembershipRequest> _allRequests;

        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        { get => _selectedStatusFilter; set { _selectedStatusFilter = value; OnPropertyChanged(); ApplyFilterAndSort(); } }

        private string _selectedSortOrder = "Newest First";
        public string SelectedSortOrder
        { get => _selectedSortOrder; set { _selectedSortOrder = value; OnPropertyChanged(); ApplyFilterAndSort(); } }

        public ObservableCollection<NeighborhoodMembershipRequest> FilteredRequests { get; set; }

        public ICommand CancelRequestCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public NeighborhoodMembershipRequestViewModel(long userId, Action closeAction)
        {
            _userId = userId;
            _requestService = Injector.CreateInstance<NeighborhoodMembershipRequestService>();
            FilteredRequests = new ObservableCollection<NeighborhoodMembershipRequest>();

            CancelRequestCommand = new RelayCommand(o => ExecuteCancelRequest(o));
            CloseWindowCommand = new RelayCommand(o => closeAction?.Invoke());

            LoadRequests();
        }

        public void LoadRequests()
        {
            _allRequests = _requestService.GetRequestsByUser(_userId) ?? new List<NeighborhoodMembershipRequest>();
            ApplyFilterAndSort();
        }

        private void ApplyFilterAndSort()
        {
            if (_allRequests == null) return;

            var filtered = _allRequests.AsEnumerable();

            if (SelectedStatusFilter != "All")
            {
                filtered = filtered.Where(r => r.Status.ToString().Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            filtered = SelectedSortOrder == "Oldest First"
                ? filtered.OrderBy(r => r.CreatedAt)
                : filtered.OrderByDescending(r => r.CreatedAt);

            FilteredRequests.Clear();
            foreach (var req in filtered)
            {
                FilteredRequests.Add(req);
            }
        }

        private void ExecuteCancelRequest(object param)
        {
            if (param is NeighborhoodMembershipRequest request)
            {
                var result = MessageBox.Show("Are you sure you want to cancel this access request?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _requestService.DeleteRequest(request.Id);
                        LoadRequests();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}