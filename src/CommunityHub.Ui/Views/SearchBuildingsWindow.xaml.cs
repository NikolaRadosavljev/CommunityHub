using System.Windows;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Views.CoordinatorViews;

namespace CommunityHub.Ui.Views;

public partial class SearchBuildingsWindow : Window
{
    private readonly long _userId;
    private readonly BuildingService _buildingService;

    public SearchBuildingsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _buildingService = new BuildingService(CommunityHub.Ui.DependencyInjection.DIConfiguration.Factory.CreateBuildingRepository());

        if (!UserSession.IsAuthenticated)
        {
            RedirectToLogin();
            return;
        }

        LoadBuildings(null, null, null, null);
    }

    private void LoadBuildings(string? address, string? settlement, string? city, string? country)
    {
        var buildings = _buildingService.Search(address, settlement, city, country);

        var displayBuildings = buildings.Select(b => new BuildingDisplayViewModel
        {
            Id = b.Id,
            Address = b.Address,
            Settlement = b.Settlement,
            Location = $"{b.City}, {b.Country}",
            FloorNumber = b.FloorNumber,
            FlatsCount = b.FlatNumbers.Count,
            FirstPicture = b.Pictures.FirstOrDefault() ?? string.Empty
        }).ToList();

        BuildingsListView.ItemsSource = displayBuildings;
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        string? address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();
        string? settlement = string.IsNullOrWhiteSpace(SettlementTextBox.Text) ? null : SettlementTextBox.Text.Trim();
        string? city = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim();
        string? country = string.IsNullOrWhiteSpace(CountryTextBox.Text) ? null : CountryTextBox.Text.Trim();

        LoadBuildings(address, settlement, city, country);
    }

    private void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        if (BuildingsListView.SelectedItem is not BuildingDisplayViewModel selectedBuilding)
        {
            MessageBox.Show("Please select a building.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        JoinBuildingWindow joinWindow = new JoinBuildingWindow(_userId, selectedBuilding.Id, selectedBuilding.Address);
        joinWindow.ShowDialog();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }

    private void RedirectToLogin()
    {
        LogInForm loginForm = new LogInForm();
        loginForm.Show();
        this.Close();
    }
}

public class BuildingDisplayViewModel
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Settlement { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public int FlatsCount { get; set; }
    public string FirstPicture { get; set; } = string.Empty;
}