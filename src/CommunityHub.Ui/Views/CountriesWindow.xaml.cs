using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Ui.Security;


namespace CommunityHub.Ui.Views;

public partial class CountriesWindow : Window
{
    private readonly CountryDbRepository _countryRepository;
    private ObservableCollection<Country> _countries;


    public CountriesWindow()
    {
        InitializeComponent();

        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može pristupiti državama.", "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _countryRepository = new CountryDbRepository();
        LoadCountries();
    }

    private void LoadCountries()
    {
        var countries = _countryRepository.GetAll();

        _countries = new ObservableCollection<Country>(countries);
        CountriesItemsControl.ItemsSource = _countries;
    }

    private void CreateCountryButton_Click(object sender, RoutedEventArgs e)
    {
        CreateCountryWindow createCountryWindow = new CreateCountryWindow();
        createCountryWindow.Show();
        this.Close();
    }

    private void Country_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        var country = button.Tag as Country;
        if (country == null) return;

        var citiesWindow = new CitiesWindow(country.Id);
        citiesWindow.Show();
        this.Close();
    }

}
