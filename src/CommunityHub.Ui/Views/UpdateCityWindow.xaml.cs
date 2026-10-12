using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;


namespace CommunityHub.Ui.Views;

public partial class UpdateCityWindow : Window
{
    private readonly City _city;
    private readonly CityDbRepository _repository;

    public UpdateCityWindow(City city)
    {
        InitializeComponent();

        _city = city;
        _repository = new CityDbRepository();

        NameTextBox.Text = _city.Name;
        PostalCodeTextBox.Text = _city.PostalCode;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Naziv grada je obavezan.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PostalCodeTextBox.Text))
        {
            MessageBox.Show("Poštanski broj je obavezan.");
            return;
        }

        var updatedCity = new City(
            _city.Id,
            NameTextBox.Text,
            PostalCodeTextBox.Text
        );

        _repository.Update(updatedCity);

        this.Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
