using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Ui.Security;


namespace CommunityHub.Ui.Views;

public partial class CreateCountryWindow : Window
{
    private readonly CountryDbRepository _countryRepository;

    public Country? CreatedCountry { get; private set; }

    public CreateCountryWindow()
    {
        InitializeComponent();

        if (!UserSession.IsInRole(UserRole.Cordinator))
        {
            MessageBox.Show("Only user with Coordinator role can create a country.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _countryRepository = new CountryDbRepository();
    }

    private void NameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        bool isValid = NameTextBox.Text.Length >= 3;
        SaveButton.IsEnabled = isValid;
        ValidationErrorTextBlock.Visibility = NameTextBox.Text.Length > 0 && !isValid
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Country country = new Country(NameTextBox.Text);
        CreatedCountry = _countryRepository.Create(country);

        CountriesWindow countriesWindow = new CountriesWindow();
        countriesWindow.Show();
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CountriesWindow countriesWindow = new CountriesWindow();
        countriesWindow.Show();
        this.Close();
    }
}
