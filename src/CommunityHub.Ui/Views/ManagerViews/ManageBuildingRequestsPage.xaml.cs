using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels.ManagerViewModels;

namespace CommunityHub.Ui.Views;

public partial class ManageBuildingRequestsPage : Page
{
    private readonly ManageBuildingRequestsViewModel _viewModel;
    private bool _isLoaded;
    private readonly string? _buildingAddress;
    private readonly string? _buildingSettlement;

    public ManageBuildingRequestsPage(string? address = null, string? settlement = null)
    {
        _viewModel = new ManageBuildingRequestsViewModel();
        _isLoaded = false;
        _buildingAddress = address;
        _buildingSettlement = settlement;
        InitializeComponent();
        DataContext = _viewModel;

        if (!UserSession.IsInRole(UserRole.Manager))
        {
            MessageBox.Show("Only user with Manager role can manage building requests.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            return;
        }

        LoadRequests();
        _isLoaded = true;
    }

    private void LoadRequests()
    {
        _viewModel.SelectedStatusFilter = GetSelectedStatusFilter();
        _viewModel.SortAscending = GetSelectedSortAscending();

        // The ViewModel's property setters call LoadRequests() automatically,
        // but we need to apply the building filter on top
        if (!string.IsNullOrEmpty(_buildingAddress) && !string.IsNullOrEmpty(_buildingSettlement))
        {
            var filtered = new ObservableCollection<BuildingRequestDTO>();
            foreach (var r in _viewModel.Requests)
            {
                if (r.Address == _buildingAddress && r.Settlement == _buildingSettlement)
                    filtered.Add(r);
            }
            _viewModel.Requests = filtered;
        }

        RequestsList.ItemsSource = _viewModel.Requests;
    }

    private RequestStatus? GetSelectedStatusFilter()
    {
        if (StatusFilterComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return null;
        }

        string? statusValue = selectedItem.Tag as string;
        if (string.IsNullOrWhiteSpace(statusValue))
        {
            return null;
        }

        return Enum.Parse<RequestStatus>(statusValue, true);
    }

    private bool GetSelectedSortAscending()
    {
        if (SortComboBox.SelectedItem is not ComboBoxItem selectedItem)
        {
            return true;
        }

        string? sortValue = selectedItem.Tag as string;
        return !string.Equals(sortValue, "DESC", StringComparison.OrdinalIgnoreCase);
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded)
        {
            return;
        }

        LoadRequests();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadRequests();
    }

    private void ApproveItem_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = ManagerMainWindow.GetFrom(this);
        if (sender is Button btn && btn.Tag is BuildingRequestDTO selectedRequest)
        {
            if (selectedRequest.Status != RequestStatus.Pending)
            {
                mainWin?.ShowErrorToast("You can only process pending requests.");
                return;
            }

            try
            {
                _viewModel.ApproveRequest(selectedRequest.Id);
                LoadRequests();
                mainWin?.ShowSuccessToast("Request accepted and membership created.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("23505") || ex.Message.Contains("duplicate key value") || (ex.InnerException != null && ex.InnerException.Message.Contains("23505")))
                {
                    mainWin?.ShowErrorToast("This flat is already rented out.");
                }
                else
                {
                    mainWin?.ShowErrorToast(ex.Message);
                }
            }
        }
    }

    private void RejectItem_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = ManagerMainWindow.GetFrom(this);
        if (sender is Button btn && btn.Tag is BuildingRequestDTO selectedRequest)
        {
            if (selectedRequest.Status != RequestStatus.Pending)
            {
                mainWin?.ShowErrorToast("You can only process pending requests.");
                return;
            }

            NavigationService.Navigate(new RejectBuildingRequestPage(selectedRequest, (reason) =>
            {
                try
                {
                    _viewModel.RejectRequest(selectedRequest.Id, reason);
                    LoadRequests();
                    ManagerMainWindow.GetFrom(this)?.ShowSuccessToast("Membership request successfully declined.", goBack: true);
                }
                catch (Exception ex)
                {
                    ManagerMainWindow.GetFrom(this)?.ShowErrorToast(ex.Message);
                }
            }));
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }
}
