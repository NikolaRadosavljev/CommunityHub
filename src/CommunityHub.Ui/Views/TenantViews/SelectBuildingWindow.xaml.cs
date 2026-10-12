using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;

namespace CommunityHub.Ui.Views.TenantViews;

public partial class SelectBuildingWindow : Window
{
    private readonly long _tenantId;
    private readonly CommunityHub.Application.Database.Repositories.HelpBoardRepository _helpBoardService;
    private int _selectedBuildingId = -1;

    public int SelectedBuildingId => _selectedBuildingId;

    public SelectBuildingWindow(long tenantId)
    {
        InitializeComponent();
        _tenantId = tenantId;
        _helpBoardService = new CommunityHub.Application.Database.Repositories.HelpBoardRepository();

        Loaded += SelectBuildingWindow_Loaded;
    }

    private void SelectBuildingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadUserBuildings();
    }

    private void LoadUserBuildings()
    {
        try
        {
            var buildingRepository = new CommunityHub.Application.Database.Repositories.BuildingDbRepository();
            var buildingService = new BuildingService(CommunityHub.Ui.DependencyInjection.DIConfiguration.Factory.CreateBuildingRepository());

            // Get all buildings where user is a member
            var allBuildings = buildingRepository.Search(null, null, null, null);
            var userBuildings = allBuildings.Where(b => _helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id)).ToList();

            if (userBuildings.Count == 0)
            {
                MessageBox.Show("You are not a member of any building.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }

            // Convert to DTO for display
            var buildingDTOs = userBuildings.Select(b => new BuildingDTO
            {
                Id = (int)b.Id,
                Address = b.Address,
                Settlement = b.Settlement,
                City = b.City,
                Country = b.Country,
                FloorNumber = b.FloorNumber,
                TotalFlats = b.FlatNumbers?.Count ?? 0,
                FlatsRange = b.FlatNumbers?.Any() == true ?
                    $"{b.FlatNumbers.Min()}-{b.FlatNumbers.Max()}" : "-"
            }).ToList();

            BuildingsListView.ItemsSource = buildingDTOs;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading buildings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BuildingButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int buildingId && buildingId > 0)
        {
            // Open RentalRequestsWindow and close this window
            var rentalWindow = new RentalRequestsWindow(_tenantId, buildingId);
            rentalWindow.Show();
            this.Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
