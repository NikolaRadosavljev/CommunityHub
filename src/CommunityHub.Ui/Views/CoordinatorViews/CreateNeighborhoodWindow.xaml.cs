using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using Microsoft.Win32;
using CommunityHub.Application;
using CommunityHub.Ui.Views.CoordinatorViews;



namespace CommunityHub.Ui.Views
{
    public partial class CreateNeighborhoodWindow : Window
    {
        private readonly long _userId;
        private readonly NeighborhoodService _neighborhoodService;
        private readonly CityDbRepository _cityRepository;
        private readonly List<Street> _streets;
        private List<string> imagePaths = new List<string>();

        public CreateNeighborhoodWindow(long userId)
        {
            InitializeComponent();
            _userId = userId;
            _neighborhoodService = Injector.CreateInstance<NeighborhoodService>();
            _cityRepository = new CityDbRepository();
            _streets = new List<Street>();

            LoadCities();
        }

        private void LoadCities()
        {
            List<City> cities = _cityRepository.GetAll();
            CityComboBox.ItemsSource = cities;
        }

        private void AddStreetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string streetName = StreetNameTextBox.Text;

                if (string.IsNullOrWhiteSpace(streetName))
                {
                    MessageBox.Show("You must enter a street name.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(StartNumberTextBox.Text, out int startNumber))
                {
                    MessageBox.Show("Start number is not valid.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(EndNumberTextBox.Text, out int endNumber))
                {
                    MessageBox.Show("End number is not valid.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (startNumber > endNumber)
                {
                    MessageBox.Show("Start number cannot be greater than end number.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Street street = new Street(streetName, startNumber, endNumber, 0);
                _streets.Add(street);

                StreetsListBox.Items.Add($"{street.Name} ({street.StartNumber} - {street.EndNumber})");

                StreetNameTextBox.Clear();
                StartNumberTextBox.Clear();
                EndNumberTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string name = NameTextBox.Text;
                string description = DescriptionTextBox.Text;

                City? selectedCity = CityComboBox.SelectedItem as City;
                if (selectedCity == null)
                {
                    MessageBox.Show("You must select a city.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_streets.Count == 0)
                {
                    MessageBox.Show("You must add at least one street.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var images = imagePaths;

                _neighborhoodService.Create(_userId, name, description, selectedCity, images, _streets);

                MessageBox.Show("Neighborhood successfully registered.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                HomeWindow homeWindow = new HomeWindow(_userId);
                homeWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddImagesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    imagePaths.Add(file);
                    ImagesListBox.Items.Add(file);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            HomeWindow homeWindow = new HomeWindow(_userId);
            homeWindow.Show();
            this.Close();
        }
    }
}