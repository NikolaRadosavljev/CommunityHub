using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Ui.Security;


namespace CommunityHub.Ui.Views;

public partial class CreateCityWindow : Window
{
    private readonly CityDbRepository _cityRepository;
    private readonly long _countryId;

    public City? CreatedCity { get; private set; }

    public CreateCityWindow(long countryId)
    {
        InitializeComponent();

        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Samo korisnik sa ulogom Cordinator može kreirati grad.", "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _countryId = countryId;
        _cityRepository = new CityDbRepository();
    }

    private void NameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool isValid = NameTextBox.Text.Length >= 3;
        SaveButton.IsEnabled = isValid;
        ValidationErrorTextBlock.Visibility = NameTextBox.Text.Length > 0 && !isValid
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void PostalCodeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool isValid = PostalCodeTextBox.Text.Length >= 3;
        SaveButton.IsEnabled = isValid;
        ValidationErrorTextBlock.Visibility = PostalCodeTextBox.Text.Length > 0 && !isValid
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        City city = new City(NameTextBox.Text, PostalCodeTextBox.Text);
        CreatedCity = _cityRepository.Create(city, _countryId);

        CitiesWindow citiesWindow = new CitiesWindow(_countryId);
        citiesWindow.Show();
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CitiesWindow citiesWindow = new CitiesWindow(_countryId);
        citiesWindow.Show();
        this.Close();
    }
}
