using System.Windows;
using System.Windows.Controls;

namespace CommunityHub.Ui.Views;

public partial class ManagerSubmenu : UserControl
{
    public event RoutedEventHandler? RefreshClicked;
    public event RoutedEventHandler? ManageRequestsClicked;
    public event RoutedEventHandler? RentalRequestsClicked;
    public event RoutedEventHandler? AssembliesClicked;

    public ManagerSubmenu()
    {
        InitializeComponent();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshClicked?.Invoke(this, e);
    }

    private void ManageBuildingRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        ManageRequestsClicked?.Invoke(this, e);
    }

    private void RentalRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        RentalRequestsClicked?.Invoke(this, e);
    }

    private void AssembliesButton_Click(object sender, RoutedEventArgs e)
    {
        AssembliesClicked?.Invoke(this, e);
    }
}
