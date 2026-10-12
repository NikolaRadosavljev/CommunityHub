using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services.HelpAds.DTO;
using System.Windows.Media;
using CommunityHub.Ui.DependencyInjection;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class TenantWindow : Window
    {
        private readonly long _tenantId;
        private readonly BuildingService _buildingService;
        private readonly BuildingRequestService _requestService;
        private readonly IHelpAdService _helpAdService;
        private readonly HelpBoardRepository _helpBoardService;
        private readonly IBuildingRepository _buildingRepository;

        public TenantWindow(
            long tenantId,
            IHelpAdService? helpAdService = null,
            IBuildingRepository? buildingRepository = null)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _buildingRepository = buildingRepository ?? DIConfiguration.Factory.CreateBuildingRepository();
            _buildingService = new BuildingService(_buildingRepository);
            _requestService = new BuildingRequestService();
            _helpBoardService = new HelpBoardRepository();
            _helpAdService = helpAdService ?? DIConfiguration.Factory.CreateHelpAdService();

            Loaded += TenantWindow_Loaded;
        }

        private void TenantWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBuildings();
            LoadRequests();
            LoadHelpAds();
        }

        private async void LoadHelpAds()
        {
            // First we need to get user's building(s) to fetch ads
            // In a better approach, we'd have a GetMemberBuildings function, but we use an approximation
            var buildingMemberships = _buildingRepository.Search(null, null, null, null)
                .Where(b => _helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id))
                .ToList();

            var allHelpAds = new List<HelpAdDTO>();

            foreach (var building in buildingMemberships)
            {
                var ads = await _helpAdService.GetHelpAdsForBuildingAsync(building.Id);
                allHelpAds.AddRange(ads);
            }

            HelpAdsListView.ItemsSource = allHelpAds;
        }

        private void LoadBuildings()
        {
            string address = SearchAddressTextBox.Text.Trim();
            string settlement = SearchSettlementTextBox.Text.Trim();
            string location = SearchLocationTextBox.Text.Trim();

            var buildings = _buildingService.Search(
                string.IsNullOrEmpty(address) ? null : address,
                string.IsNullOrEmpty(settlement) ? null : settlement,
                null,
                null);

            if (!string.IsNullOrEmpty(location))
            {
                var lowerLocation = location.ToLower();
                buildings = buildings.Where(b => 
                    b.City.ToLower().Contains(lowerLocation) || 
                    b.Country.ToLower().Contains(lowerLocation) ||
                    $"{b.City}, {b.Country}".ToLower().Contains(lowerLocation)).ToList();
            }

            var uniqueBuildings = buildings
                .GroupBy(b => b.Address.Trim().ToLower())
                .Select(g => g.First())
                .ToList();

            var buildingDTOs = uniqueBuildings.Select(b => 
            {
                string flatsRange = "-";
                if (b.FlatNumbers != null && b.FlatNumbers.Any())
                {
                    int minFlat = b.FlatNumbers.Min();
                    int maxFlat = b.FlatNumbers.Max();
                    flatsRange = minFlat == maxFlat ? minFlat.ToString() : $"{minFlat}-{maxFlat}";
                }

                return new BuildingDTO
                {
                    Id = b.Id,
                    Address = b.Address ?? string.Empty,
                    Settlement = b.Settlement ?? string.Empty,
                    City = b.City ?? string.Empty,
                    Country = b.Country ?? string.Empty,
                    FloorNumber = b.FloorNumber,
                    TotalFlats = b.FlatNumbers?.Count ?? 0,
                    FlatsRange = flatsRange,
                    MainPicture = b.Pictures?.FirstOrDefault() ?? string.Empty,
                    FlatNumbers = b.FlatNumbers ?? new System.Collections.Generic.List<int>()
                }; 
            }).ToList();

            BuildingsListView.ItemsSource = buildingDTOs;
        }

        private string GetTextBoxValue(TextBox textBox, string placeholder)
        {
            return textBox.Text == placeholder ? string.Empty : textBox.Text.Trim();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBuildings();
        }

        private void ViewAllButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPlaceholder(SearchAddressTextBox, "Address");
            ResetPlaceholder(SearchSettlementTextBox, "Settlement");
            ResetPlaceholder(SearchLocationTextBox, "City or Country");

            LoadBuildings();
        }

        private void ResetPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.Foreground = Brushes.Gray;
        }

        private void Placeholder_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == textBox.Tag.ToString())
                {
                    textBox.Text = "";
                    textBox.Foreground = Brushes.Black;
                }
            }
        }

        private void Placeholder_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = textBox.Tag.ToString();
                    textBox.Foreground = Brushes.Gray;
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void FlatNumberComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is BuildingDTO building)
            {
                comboBox.ItemsSource = building.FlatNumbers;
            }
        }

        private void RequestAccessButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long buildingId)
            {
                // Pronađi ComboBox u celom redu zgrade
                var border = FindParent<Border>(btn);
                var comboBox = FindChild<ComboBox>(border, "FlatNumberComboBox");

                if (comboBox == null || comboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a flat number.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int flatNumber = (int)comboBox.SelectedItem;

                var selectedBuilding = BuildingsListView.Items
                    .OfType<BuildingDTO>()
                    .FirstOrDefault(b => b.Id == buildingId);

                if (selectedBuilding == null)
                {
                    MessageBox.Show("Could not find the building.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_requestService.IsFlatOccupied(selectedBuilding.Id, flatNumber))
                {
                    var warningDialog = new OccupancyWarningDialog();
                    warningDialog.Owner = this;
                    warningDialog.ShowDialog();
                    if (!warningDialog.UserConfirmed) return;
                }
                else
                {
                    var confirmResult = MessageBox.Show(
                        $"Are you sure you want to submit the request for apartment {flatNumber}?",
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmResult == MessageBoxResult.No) return;
                }

                try
                {
                    _requestService.CreateRequest(_tenantId, selectedBuilding.Id, flatNumber);
                    MessageBox.Show("The request has been successfully submitted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    comboBox.SelectedItem = null;
                    LoadRequests();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Helper metode za pronalazenje elemenata
        private T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;
            if (parent is T t) return t;
            return FindParent<T>(parent);
        }

        private T? FindChild<T>(DependencyObject? parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && (child as FrameworkElement)?.Name == childName)
                    return t;
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        private void LoadRequests()
        {
            var requests = _requestService.GetTenantRequests(_tenantId, null, true);

            // Removing UI binding logic momentarily since xaml elements were removed
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SortDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MyBuildingButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new MyBuildingsWindow(_tenantId);
            window.Show();
            this.Close();
        }

        private void RequestsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TenantBuildingRequestsWindow(_tenantId);
            window.Show();
            this.Close();
        }

        private void NoticeBoardButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if user is member of at least one building
            var userBuilding = _buildingRepository
                .Search(null, null, null, null)
                .OrderBy(b => b.Id)
                .FirstOrDefault(b => _helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id));

            if (userBuilding == null)
            {
                MessageBox.Show("You must be a member of a building to access the notice board.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open Notice Board with building selection page (buildingId = 0)
            var window = new NoticeBoardWindow(_tenantId, 0, _helpAdService);
            window.Show();
            this.Close();
        }

        private void MyAdsButton_Click(object sender, RoutedEventArgs e)
        {
            var userBuilding = _buildingRepository
                .Search(null, null, null, null)
                .OrderBy(b => b.Id)
                .FirstOrDefault(b => _helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id));

            if (userBuilding == null)
            {
                MessageBox.Show("You must be a member of a building to view your ads.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new MyAdsWindow(_tenantId, userBuilding.Id, _helpAdService);
            window.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, 0, _helpAdService);
            notificationsWindow.Show();
            this.Close();
        }

        private void CancelRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Removed to match simplified UI
        }

        private void RentingCommonRoomsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open renting common rooms window (with building selection inside)
            var rentalWindow = new RentalRequestsWindow(_tenantId);
            rentalWindow.Show();
            this.Close();
        }

        private void ProblemButton_Click(object sender, RoutedEventArgs e)
        {
            var problemWindow = new ProblemWindow(_tenantId);
            problemWindow.Show();
            this.Close();
        }

        private void HomeownersAssociationButton_Click(object sender, RoutedEventArgs e)
        {
            var homeownersWindow = new HomeownersMeetingWindow(_tenantId);
            homeownersWindow.Show();
            this.Close();
        }
    }
}