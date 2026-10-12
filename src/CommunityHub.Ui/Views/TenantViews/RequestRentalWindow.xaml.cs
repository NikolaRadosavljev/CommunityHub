using System.Windows;
using CommunityHub.Application.Services;
using CommunityHub.Ui.ViewModels;

namespace CommunityHub.Ui.Views.TenantViews;

public partial class RequestRentalWindow : Window
{
    private readonly long _tenantId;
    private readonly int _commonRoomId;
    private readonly RentalRequestsViewModel _viewModel;
    private readonly CommonRoomService _commonRoomService;

    public RequestRentalWindow(long tenantId, int commonRoomId, RentalRequestsViewModel viewModel)
    {
        InitializeComponent();
        _tenantId = tenantId;
        _commonRoomId = commonRoomId;
        _viewModel = viewModel;
        _commonRoomService = new CommonRoomService();

        Loaded += RequestRentalWindow_Loaded;
    }

    private void RequestRentalWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Set minimum date to today
        StartDatePicker.DisplayDateStart = DateTime.Now.Date;
        EndDatePicker.DisplayDateStart = DateTime.Now.Date;

        // Load common room information
        try
        {
            var room = _commonRoomService.GetCommonRoomById(_commonRoomId);
            if (room != null)
            {
                RoomNameTextBlock.Text = room.Name;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading room information: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate dates
        if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
        {
            InfoTextBlock.Text = "Please select both start and end dates.";
            InfoTextBlock.Visibility = Visibility.Visible;
            return;
        }

        DateTime startDate = StartDatePicker.SelectedDate.Value;
        DateTime endDate = EndDatePicker.SelectedDate.Value;

        if (startDate > endDate)
        {
            InfoTextBlock.Text = "Start date must be before end date.";
            InfoTextBlock.Visibility = Visibility.Visible;
            return;
        }

        if (startDate.Date < DateTime.Now.Date)
        {
            InfoTextBlock.Text = "Start date cannot be in the past.";
            InfoTextBlock.Visibility = Visibility.Visible;
            return;
        }

        // Submit request
        try
        {
            var requestId = _viewModel.CreateRentalRequest(_commonRoomId, startDate, endDate);
            MessageBox.Show($"Request created successfully (ID: {requestId})", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
        catch (Exception ex)
        {
            InfoTextBlock.Text = $"Error: {ex.Message}";
            InfoTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
