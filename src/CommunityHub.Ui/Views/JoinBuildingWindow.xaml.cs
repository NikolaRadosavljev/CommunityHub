using System.Windows;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views;

public partial class JoinBuildingWindow : Window
{
    private readonly long _userId;
    private readonly int _buildingId;
    private readonly BuildingRequestService _requestService;
    private bool _hasWarned;
    private int _lastCheckedFlatNumber;

    public JoinBuildingWindow(long userId, int buildingId, string address)
    {
        InitializeComponent();
        _userId = userId;
        _buildingId = buildingId;
        BuildingAddressTextBlock.Text = $"Building: {address}";
        _requestService = new BuildingRequestService();
        _hasWarned = false;
        _lastCheckedFlatNumber = -1;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(FlatNumberTextBox.Text.Trim(), out int flatNumber) || flatNumber <= 0)
        {
            MessageBox.Show("Please enter a valid flat number (an integer greater than 0).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_lastCheckedFlatNumber != flatNumber)
        {
            _hasWarned = false;
            _lastCheckedFlatNumber = flatNumber;
            WarningTextBlock.Visibility = Visibility.Collapsed;
        }

        if (!_hasWarned)
        {
            bool isOccupied = _requestService.IsFlatOccupied(_buildingId, flatNumber);
            if (isOccupied)
            {
                _hasWarned = true;
                WarningTextBlock.Visibility = Visibility.Visible;
                return; // User can just click again to confirm
            }
        }

        try
        {
            _requestService.CreateRequest(_userId, _buildingId, flatNumber);
            MessageBox.Show("Building access request created and pending approval.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}