using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;

namespace CommunityHub.Ui.Views;

public partial class AddCommonSpacePage : Page
{
    private readonly ICommonSpaceService _commonSpaceService;
    private readonly int _buildingId;
    internal CancellationTokenSource? _demoCts;
    
    public event System.Action? OnCommonSpaceAdded;

    public AddCommonSpacePage(int buildingId)
    {
        InitializeComponent();
        _buildingId = buildingId;
        _commonSpaceService = ManagerDependencyInjector.CreateInstance<ICommonSpaceService>();
    }

    public async void StartDemo()
    {
        if (_demoCts != null)
        {
            _demoCts.Cancel();
            _demoCts = null;
            return;
        }

        _demoCts = new CancellationTokenSource();

        string originalName = NameTextBox.Text;
        string originalDescription = DescriptionTextBox.Text;
        string originalFloor = FloorNumberTextBox.Text;
        bool? originalOneDay = OneDayRadioButton.IsChecked;
        bool? originalMoreDays = MoreDaysRadioButton.IsChecked;

        ResetFields();

        try
        {
            await RunDemoAsync(_demoCts.Token);
        }
        catch (TaskCanceledException)
        {
        }
        finally
        {
            NameTextBox.Text = originalName;
            DescriptionTextBox.Text = originalDescription;
            FloorNumberTextBox.Text = originalFloor;
            OneDayRadioButton.IsChecked = originalOneDay;
            MoreDaysRadioButton.IsChecked = originalMoreDays;

            _demoCts?.Dispose();
            _demoCts = null;
        }
    }

    private void ResetFields()
    {
        NameTextBox.Text = string.Empty;
        DescriptionTextBox.Text = string.Empty;
        FloorNumberTextBox.Text = string.Empty;
        OneDayRadioButton.IsChecked = false;
        MoreDaysRadioButton.IsChecked = false;
    }

    private async Task RunDemoAsync(CancellationToken token)
    {
        string name = "Party Room";
        string description = "A room where tenants can host birthday parties and other gatherings.";
        string floor = "0";

        foreach (char c in name)
        {
            NameTextBox.Text += c;
            await Task.Delay(100, token);
        }
        await Task.Delay(500, token);

        foreach (char c in description)
        {
            DescriptionTextBox.Text += c;
            await Task.Delay(50, token);
        }
        await Task.Delay(500, token);

        foreach (char c in floor)
        {
            FloorNumberTextBox.Text += c;
            await Task.Delay(100, token);
        }
        await Task.Delay(500, token);

        OneDayRadioButton.IsChecked = true;
        await Task.Delay(1000, token);
    }

    private void FinishAdding_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = ManagerMainWindow.GetFrom(this);

        if (_demoCts != null)
        {
            mainWin?.ShowErrorToast("Please stop the demo before adding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(FloorNumberTextBox.Text))
        {
            mainWin?.ShowErrorToast("Please fill out all required fields.");
            return;
        }

        if (!int.TryParse(FloorNumberTextBox.Text, out int floorNumber))
        {
            mainWin?.ShowErrorToast("Floor number must be an integer.");
            return;
        }

        if (OneDayRadioButton.IsChecked != true && MoreDaysRadioButton.IsChecked != true)
        {
            mainWin?.ShowErrorToast("Please select a rental period.");
            return;
        }

        CommonSpaceRentalType rentalType = OneDayRadioButton.IsChecked == true
            ? CommonSpaceRentalType.SingleDay
            : CommonSpaceRentalType.MultipleDays;

        try
        {
            _commonSpaceService.CreateCommonSpace(
                _buildingId,
                NameTextBox.Text,
                DescriptionTextBox.Text,
                floorNumber,
                rentalType);

            // Trigger refresh callback
            OnCommonSpaceAdded?.Invoke();

            mainWin?.ShowSuccessToast("Common room successfully added.", goBack: true);
        }
        catch (System.Exception ex)
        {
            mainWin?.ShowErrorToast(ex.Message);
        }
    }
}
