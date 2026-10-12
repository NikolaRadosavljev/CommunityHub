using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Ui.Security;


namespace CommunityHub.Ui.Views;

public partial class CitiesWindow : Window
{
    private readonly long _countryId;
    private readonly CityDbRepository _cityRepository;
    private readonly CountryDbRepository _countryRepository;
    private ObservableCollection<City> _cities;


    public CitiesWindow(long countryId)
    {
        InitializeComponent();

        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može pristupiti gradovima.", "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _countryId = countryId;
        _cityRepository = new CityDbRepository();
        _countryRepository = new CountryDbRepository();
        LoadCities();
    }

    private void LoadCities()
    {
        var country = _countryRepository.GetWithCities(_countryId);

        if (country.Cities != null)
        {
            _cities = new ObservableCollection<City>(country.Cities);
        } else
        {
            _cities = new ObservableCollection<City>([]);
        }

        CitiesItemsControl.ItemsSource = _cities;
    }

    private void CreateCityButton_Click(object sender, RoutedEventArgs e)
    {
        CreateCityWindow createCityWindow = new CreateCityWindow(_countryId);
        createCityWindow.Show();
        this.Close();
    }

    private void CountriesButton_Click(object sender, RoutedEventArgs e)
    {
        CountriesWindow countriesWindow = new CountriesWindow();
        countriesWindow.Show();
        this.Close();
    }

    private void DeleteCity_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        var city = button.Tag as City;
        if (city == null) return;

        _cityRepository.Delete(city.Id);

        LoadCities();
    }

    private void UpdateCity_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        var city = button.Tag as City;
        if (city == null) return;

        var updateWindow = new UpdateCityWindow(city);
        updateWindow.ShowDialog();

        LoadCities();
    }

}
