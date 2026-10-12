using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.Views.ManagerViews.RentalRequests;

public partial class RentalRequestsPage : Page
{
    private readonly CommonSpaceRentalRequestService _rentalService;
    private readonly BuildingDbRepository _buildingRepository;
    private readonly CommonSpaceDbRepository _commonSpaceRepository;
    private readonly UserDbRepository _userRepository;

    private List<RentalRequestViewModel> _allViewModels = new();

    public RentalRequestsPage()
    {
        InitializeComponent();
        _rentalService = new CommonSpaceRentalRequestService();
        _buildingRepository = new BuildingDbRepository();
        _commonSpaceRepository = new CommonSpaceDbRepository();
        _userRepository = new UserDbRepository();

        // Refresh svaki put kada stranica postane vidljiva (npr. po povratku sa SelectDay/SetDate)
        IsVisibleChanged += (s, e) =>
        {
            if ((bool)e.NewValue) LoadRequests();
        };

        LoadRequests();
    }

    private void LoadRequests()
    {
        var currentUser = UserSession.CurrentUser;
        if (currentUser == null) return;

        var buildings = _buildingRepository.GetByManagerId(currentUser.Id);
        var buildingIds = buildings.ConvertAll(b => b.Id);

        // Učitati SVE zahteve (ne samo pending) za sve zgrade koje upravlja trenutni korisnik
        var requests = _rentalService.GetAllRequests(buildingIds);

        _allViewModels = new List<RentalRequestViewModel>();

        foreach (var request in requests)
        {
            string tenantName = _userRepository.GetFullNameById(request.TenantId);

            var commonSpace = _commonSpaceRepository.GetById(request.CommonSpaceId);
            string spaceName = commonSpace?.Name ?? $"Prostorija #{request.CommonSpaceId}";

            // Dodaj adresu zgrade
            var building = commonSpace != null ? _buildingRepository.GetById(commonSpace.BuildingId) : null;
            string buildingAddress = building?.Address ?? "Unknown Building";
            string displayName = $"{spaceName} ({buildingAddress})";

            bool isSingleDay = commonSpace?.RentalType == CommonSpaceRentalType.SingleDay;
            string rentalPeriodLabel = isSingleDay ? "One day" : "More days";

            string dateRange = $"{request.RequestedStartDate:dd.MM.yyyy} - {request.RequestedEndDate:dd.MM.yyyy}";

            string statusLabel = request.Status switch
            {
                RentalRequestStatus.Pending => "Pending",
                RentalRequestStatus.PendingChangeOfDate => "Pending (Date Change)",
                RentalRequestStatus.Approved => "Approved",
                RentalRequestStatus.Rejected => "Rejected",
                _ => request.Status.ToString()
            };

            // Dugme samo za akcione statuse
            bool showButton = request.Status == RentalRequestStatus.Pending ||
                              request.Status == RentalRequestStatus.PendingChangeOfDate;

            _allViewModels.Add(new RentalRequestViewModel
            {
                Id = request.Id,
                SpaceName = displayName,
                TenantName = tenantName,
                DateRangeString = $"Period: {dateRange}",
                RentalPeriodLabel = $"Rental Period: {rentalPeriodLabel}",
                Status = statusLabel,
                ButtonText = isSingleDay ? "Select Day" : "Set Date",
                ButtonVisibility = showButton ? Visibility.Visible : Visibility.Collapsed,
                IsSingleDay = isSingleDay
            });
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (RequestsList == null || _allViewModels == null) return;
        string selected = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

        var filtered = selected == "All"
            ? _allViewModels
            : _allViewModels.Where(vm => vm.Status == selected).ToList();

        RequestsList.ItemsSource = filtered;
    }

    private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_allViewModels == null) return;
        ApplyFilter();
    }

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int requestId)
        {
            var vm = btn.DataContext as RentalRequestViewModel;
            if (vm != null && vm.IsSingleDay)
                NavigationService.Navigate(new SelectDayPage(requestId));
            else
                NavigationService.Navigate(new SetDatePage(requestId));
        }
    }
}

public class RentalRequestViewModel
{
    public int Id { get; set; }
    public string SpaceName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string DateRangeString { get; set; } = string.Empty;
    public string RentalPeriodLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public Visibility ButtonVisibility { get; set; }
    public bool IsSingleDay { get; set; }
}

