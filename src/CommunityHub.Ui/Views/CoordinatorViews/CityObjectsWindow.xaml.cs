using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class CityObjectsWindow : Window
{
    private readonly long _userId;
    private readonly CityObjectService _cityObjectService;
    private readonly NeighborhoodService _neighborhoodService;
    private List<CityObject> _cityObjects;
    private List<Neighborhood> _neighborhoods;
    private List<DateTime> _availableSlots;

    public CityObjectsWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _cityObjectService = Injector.CreateInstance<CityObjectService>();
        _neighborhoodService = Injector.CreateInstance<NeighborhoodService>();
        _cityObjects = new List<CityObject>();
        _neighborhoods = new List<Neighborhood>();
        _availableSlots = new List<DateTime>();

        LoadNeighborhoods();
        LoadCityObjects();
    }

    private void LoadNeighborhoods()
    {
        _neighborhoods = _neighborhoodService.GetByCoordinatorId(_userId);
        NeighborhoodComboBox.ItemsSource = _neighborhoods;
        if (_neighborhoods.Count > 0)
            NeighborhoodComboBox.SelectedIndex = 0;
    }

    private void LoadCityObjects()
    {
        _cityObjects = _cityObjectService.GetAll();
        CityObjectsDataGrid.ItemsSource = _cityObjects;
    }

    private void NeighborhoodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void FindSlotsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CityObjectsDataGrid.SelectedItem is not CityObject selected)
            {
                MessageBox.Show("Odaberite gradski objekat.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
            {
                MessageBox.Show("Unesite ispravno trajanje u minutima.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Odaberite opseg datuma.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime rangeStart = StartDatePicker.SelectedDate.Value;
            DateTime rangeEnd = EndDatePicker.SelectedDate.Value.AddHours(23).AddMinutes(59);

            _availableSlots = _cityObjectService.FindFreeSlots(
                selected.Id, duration, rangeStart, rangeEnd);

            if (_availableSlots.Count == 0)
            {
                SlotsLabelTextBlock.Text = "Nema slobodnih termina u zadatom opsegu. Alternativni termini:";
                _availableSlots = _cityObjectService.FindFreeSlotsOutsideRange(
                    selected.Id, duration, rangeStart, rangeEnd);
            }
            else
            {
                SlotsLabelTextBlock.Text = "Slobodni termini:";
            }

            SlotsListBox.ItemsSource = _availableSlots
                .Select(s => s.ToString("dd.MM.yyyy HH:mm")).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReserveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CityObjectsDataGrid.SelectedItem is not CityObject selected)
            {
                MessageBox.Show("Odaberite gradski objekat.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SlotsListBox.SelectedItem == null)
            {
                MessageBox.Show("Odaberite termin.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NeighborhoodComboBox.SelectedItem is not Neighborhood neighborhood)
            {
                MessageBox.Show("Odaberite kvart.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int selectedIndex = SlotsListBox.SelectedIndex;
            DateTime chosenSlot = _availableSlots[selectedIndex];
            int duration = int.Parse(DurationTextBox.Text);

            _cityObjectService.Reserve(selected.Id, neighborhood.Id, chosenSlot, duration);

            MessageBox.Show("Termin uspešno rezervisan!", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LoadCityObjects();
            SlotsListBox.ItemsSource = null;
            _availableSlots.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        HomeWindow homeWindow = new HomeWindow(_userId);
        homeWindow.Show();
        this.Close();
    }
}