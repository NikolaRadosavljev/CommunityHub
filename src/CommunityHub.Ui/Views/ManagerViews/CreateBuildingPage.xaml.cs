using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Application.Services.DTO;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.Infrastructure;
using Microsoft.Win32;

namespace CommunityHub.Ui.Views;

public partial class CreateBuildingPage : Page
{
    private readonly long _managerId;
    private readonly IBuildingService _buildingService = null!;
    private readonly CountryDbRepository _countryRepository = null!;
    private readonly ObservableCollection<string> _picturePaths = null!;
    private readonly ObservableCollection<FloorViewModel> _floors = null!;
    internal CancellationTokenSource? _demoCts;

    public event System.Action? OnBuildingCreated;

    public Building? CreatedBuilding { get; private set; }

    public CreateBuildingPage()
    {
        InitializeComponent();

        if (!UserSession.IsInRole(UserRole.Manager))
        {
            // Access denied — toast won't work yet (window not loaded), keep as MessageBox
            MessageBox.Show("Only users with the Manager role can register a building.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            return;
        }

        _managerId = UserSession.CurrentUser!.Id;
        _buildingService = ManagerDependencyInjector.CreateInstance<IBuildingService>();
        _countryRepository = new CountryDbRepository();

        _picturePaths = new ObservableCollection<string>();
        ImagesItemsControl.ItemsSource = _picturePaths;

        _floors = new ObservableCollection<FloorViewModel>();
        FloorsItemsControl.ItemsSource = _floors;

        LoadCountries();
    }

    private void LoadCountries()
    {
        List<Country> countries = _countryRepository.GetAll();
        CountryComboBox.ItemsSource = countries;
    }

    public async void StartDemo()
    {
        if (_demoCts != null)
        {
            _demoCts.Cancel();
            return;
        }

        _demoCts = new CancellationTokenSource();
        var token = _demoCts.Token;

        string origAddress = AddressTextBox.Text;
        string origSettlement = SettlementTextBox.Text;
        var origCountry = CountryComboBox.SelectedItem;
        var origCity = CityComboBox.SelectedItem;
        string origFloors = FloorCountTextBox.Text;
        var origPage1Visibility = Page1Grid.Visibility;
        var origPage2Visibility = Page2Grid.Visibility;

        AddressTextBox.IsReadOnly = true;
        SettlementTextBox.IsReadOnly = true;
        FloorCountTextBox.IsReadOnly = true;
        CountryComboBox.IsEnabled = false;
        CityComboBox.IsEnabled = false;

        try
        {
            if (Page1Grid.Visibility == Visibility.Visible)
            {
                AddressTextBox.Text = "";
                SettlementTextBox.Text = "";
                FloorCountTextBox.Text = "";
                CountryComboBox.SelectedItem = null;
                CityComboBox.SelectedItem = null;

                string demoAddress = "Fruskogorska 10";
                foreach (char c in demoAddress) { token.ThrowIfCancellationRequested(); AddressTextBox.Text += c; await Task.Delay(100, token); }

                string demoSettlement = "Liman";
                foreach (char c in demoSettlement) { token.ThrowIfCancellationRequested(); SettlementTextBox.Text += c; await Task.Delay(100, token); }

                if (CountryComboBox.Items.Count > 0)
                {
                    CountryComboBox.SelectedIndex = 0;
                    await Task.Delay(500, token);
                    if (CityComboBox.Items.Count > 0)
                    {
                        CityComboBox.SelectedIndex = 0;
                        await Task.Delay(500, token);
                    }
                }

                string demoFloors = "3";
                foreach (char c in demoFloors) { token.ThrowIfCancellationRequested(); FloorCountTextBox.Text += c; await Task.Delay(100, token); }

                await Task.Delay(2000, token);
            }
            else if (Page2Grid.Visibility == Visibility.Visible)
            {
                await Task.Delay(2000, token);
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            AddressTextBox.Text = origAddress;
            SettlementTextBox.Text = origSettlement;
            CountryComboBox.SelectedItem = origCountry;
            CityComboBox.SelectedItem = origCity;
            FloorCountTextBox.Text = origFloors;
            Page1Grid.Visibility = origPage1Visibility;
            Page2Grid.Visibility = origPage2Visibility;

            AddressTextBox.IsReadOnly = false;
            SettlementTextBox.IsReadOnly = false;
            FloorCountTextBox.IsReadOnly = false;
            CountryComboBox.IsEnabled = true;
            CityComboBox.IsEnabled = true;

            _demoCts?.Dispose();
            _demoCts = null;
        }
    }

    private void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CountryComboBox.SelectedItem is Country selectedCountry)
        {
            var countryWithCities = _countryRepository.GetWithCities(selectedCountry.Id);
            CityComboBox.ItemsSource = countryWithCities?.Cities ?? new List<City>();
            CityComboBox.SelectedIndex = -1;
        }
        else
        {
            CityComboBox.ItemsSource = null;
        }
    }

    private void SelectImagesButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "Select building images",
            Multiselect = true,
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (var file in openFileDialog.FileNames)
            {
                if (!_picturePaths.Contains(file))
                {
                    _picturePaths.Add(file);
                }
            }
        }
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            _picturePaths.Remove(path);
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        ValidationErrorTextBlock.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(AddressTextBox.Text) || 
            string.IsNullOrWhiteSpace(SettlementTextBox.Text) ||
            CityComboBox.SelectedItem == null ||
            CountryComboBox.SelectedItem == null)
        {
            ValidationErrorTextBlock.Text = "Please fill in all address fields.";
            ValidationErrorTextBlock.Visibility = Visibility.Visible;
            return;
        }

        if (!int.TryParse(FloorCountTextBox.Text.Trim(), out int floorCount) || floorCount <= 0)
        {
            ValidationErrorTextBlock.Text = "Floor count must be an integer greater than 0.";
            ValidationErrorTextBlock.Visibility = Visibility.Visible;
            return;
        }

        if (_picturePaths.Count == 0)
        {
            ValidationErrorTextBlock.Text = "At least one picture must be selected.";
            ValidationErrorTextBlock.Visibility = Visibility.Visible;
            return;
        }

        SetupFloors(floorCount);

        Page1Grid.Visibility = Visibility.Collapsed;
        Page2Grid.Visibility = Visibility.Visible;
    }

    private void SetupFloors(int count)
    {
        _floors.Clear();
        for (int i = 1; i <= count; i++)
        {
            _floors.Add(new FloorViewModel { FloorNumber = i, Title = $"Floor {i}:", FlatCount = 1 });
        }
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        Page2Grid.Visibility = Visibility.Collapsed;
        Page1Grid.Visibility = Visibility.Visible;
    }

    private void IncreaseFlatCount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FloorViewModel model)
        {
            model.FlatCount++;
        }
    }

    private void DecreaseFlatCount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FloorViewModel model && model.FlatCount > 1)
        {
            model.FlatCount--;
        }
    }

    private void FinishRegistration_Click(object sender, RoutedEventArgs e)
    {
        var rawFlatsBuilder = new System.Text.StringBuilder();
        int currentFlatNumber = 1;
        foreach (var floor in _floors)
        {
            rawFlatsBuilder.Append($"{floor.FloorNumber}: ");
            List<int> flats = new List<int>();
            for (int i = 1; i <= floor.FlatCount; i++)
            {
                flats.Add(currentFlatNumber++);
            }
            rawFlatsBuilder.AppendLine(string.Join(",", flats));
        }

        BuildingRegistrationRequestDTO request = new BuildingRegistrationRequestDTO
        {
            ManagerId = _managerId,
            Address = AddressTextBox.Text,
            Settlement = SettlementTextBox.Text,
            City = ((City)CityComboBox.SelectedItem).Name,
            Country = ((Country)CountryComboBox.SelectedItem).Name,
            FloorCount = _floors.Count,
            FlatsByFloorRaw = rawFlatsBuilder.ToString(),
            PicturePaths = [.. _picturePaths]
        };

        try
        {
            BuildingRegistrationDTO result = _buildingService.Register(request);
            if (!result.IsSuccess || result.Building == null)
            {
                ManagerMainWindow.GetFrom(this)?.ShowErrorToast(result.Message);
                return;
            }

            CreatedBuilding = result.Building;

            // Trigger refresh callback
            OnBuildingCreated?.Invoke();

            ManagerMainWindow.GetFrom(this)?.ShowSuccessToast("Building successfully registered.", goBack: true);
        }
        catch (System.Exception ex)
        {
            ManagerMainWindow.GetFrom(this)?.ShowErrorToast(ex.Message);
        }
    }
}

public class FloorViewModel : INotifyPropertyChanged
{
    private int _flatCount;

    public int FloorNumber { get; set; }
    public string Title { get; set; } = string.Empty;

    public int FlatCount
    {
        get => _flatCount;
        set
        {
            if (_flatCount == value)
            {
                return;
            }

            _flatCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlatCount)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
