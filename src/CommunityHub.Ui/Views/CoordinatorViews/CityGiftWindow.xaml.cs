using System.Windows;
using CommunityHub.Application;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views.CoordinatorViews;

public partial class CityGiftWindow : Window
{
    private readonly long _userId;
    private readonly CityGiftService _cityGiftService;
    private readonly NeighborhoodService _neighborhoodService;
    private List<CityGift> _gifts;
    private List<Neighborhood> _neighborhoods;

    public CityGiftWindow(long userId)
    {
        InitializeComponent();
        _userId = userId;
        _cityGiftService = Injector.CreateInstance<CityGiftService>();
        _neighborhoodService = Injector.CreateInstance<NeighborhoodService>();
        _gifts = new List<CityGift>();
        _neighborhoods = new List<Neighborhood>();

        LoadNeighborhoods();
        LoadGifts();
        _cityGiftService.ProcessExpiredGifts();
    }

    private void LoadNeighborhoods()
    {
        _neighborhoods = _neighborhoodService.GetByCoordinatorId(_userId);
        NeighborhoodComboBox.ItemsSource = _neighborhoods;
        if (_neighborhoods.Count > 0)
            NeighborhoodComboBox.SelectedIndex = 0;
    }

    private void LoadGifts()
    {
        _gifts = _cityGiftService.GetAvailable();
        GiftsDataGrid.ItemsSource = _gifts.Select(g => new
        {
            Id = g.Id,
            Amount = $"{g.Amount:F2} RSD",
            Deadline = g.Deadline.ToString(),
            Status = g.IsAssigned ? "Dodeljen" : "Aktivan"
        }).ToList();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (GiftsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Odaberite poklon.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NeighborhoodComboBox.SelectedItem is not Neighborhood neighborhood)
            {
                MessageBox.Show("Odaberite kvart.", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long giftId = (long)GiftsDataGrid.SelectedItem
                .GetType().GetProperty("Id")!
                .GetValue(GiftsDataGrid.SelectedItem)!;

            _cityGiftService.Apply(giftId, neighborhood.Id, _userId);

            MessageBox.Show("Uspešno ste se prijavili za poklon grada!", "Uspeh",
                MessageBoxButton.OK, MessageBoxImage.Information);

            LoadGifts();
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