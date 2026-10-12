using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Views;

public partial class RejectBuildingRequestPage : Page
{
    private CancellationTokenSource? _demoCts;
    private readonly BuildingRequestDTO _request;
    private readonly Action<string> _onRejectAction;

    public RejectBuildingRequestPage(BuildingRequestDTO request, Action<string> onRejectAction)
    {
        InitializeComponent();
        _request = request;
        _onRejectAction = onRejectAction;
        this.DataContext = request;
    }

    public async void StartDemo()
    {
        if (_demoCts != null)
        {
            _demoCts.Cancel();
            return;
        }

        _demoCts = new CancellationTokenSource();
        var token = _demoCts.Token;

        string originalText = ReasonTextBox.Text;
        ReasonTextBox.IsReadOnly = true;
        ReasonTextBox.Text = string.Empty;

        try
        {
            string demoText = "The provided details are incomplete or incorrect. Please verify your information and submit again.";
            foreach (char c in demoText)
            {
                token.ThrowIfCancellationRequested();
                ReasonTextBox.Text += c;
                await Task.Delay(100, token);
            }

            if (!token.IsCancellationRequested)
                await Task.Delay(2000, token);
        }
        catch (TaskCanceledException) { }
        finally
        {
            ReasonTextBox.Text = originalText;
            ReasonTextBox.IsReadOnly = false;
            _demoCts?.Dispose();
            _demoCts = null;
        }
    }

    private void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        string reason = ReasonTextBox.Text.Trim();
        _onRejectAction(reason);
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
    }
}
