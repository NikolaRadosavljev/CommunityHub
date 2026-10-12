using System.Windows;
using System.Windows.Controls;

namespace CommunityHub.Ui.Views;

public partial class ManagerMainMenu : UserControl
{
    public event RoutedEventHandler? CloseMenuClicked;
    public event RoutedEventHandler? RefreshClicked;
    public event RoutedEventHandler? RegisterBuildingClicked;
    public event RoutedEventHandler? ManageRequestsClicked;
    public event RoutedEventHandler? RentalRequestsClicked;
    public event RoutedEventHandler? AssembliesClicked;
    public event RoutedEventHandler? UnresolvedProblemsClicked;
    public event RoutedEventHandler? DemoModeClicked;
    public event RoutedEventHandler? LogoutClicked;

    public ManagerMainMenu()
    {
        InitializeComponent();
    }

    private void CloseMenu_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CloseMenuClicked?.Invoke(this, new RoutedEventArgs());
    }

    private void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshClicked?.Invoke(this, e);
    }

    private void MenuRegisterBuilding_Click(object sender, RoutedEventArgs e)
    {
        RegisterBuildingClicked?.Invoke(this, e);
    }

    private void MenuManageRequests_Click(object sender, RoutedEventArgs e)
    {
        ManageRequestsClicked?.Invoke(this, e);
    }

    private void MenuRentalRequests_Click(object sender, RoutedEventArgs e)
    {
        RentalRequestsClicked?.Invoke(this, e);
    }

    private void MenuAssemblies_Click(object sender, RoutedEventArgs e)
    {
        AssembliesClicked?.Invoke(this, e);
    }

    private void MenuUnresolvedProblems_Click(object sender, RoutedEventArgs e)
    {
        UnresolvedProblemsClicked?.Invoke(this, e);
    }

    private void MenuDemoMode_Click(object sender, RoutedEventArgs e)
    {
        DemoModeClicked?.Invoke(this, e);
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LogoutClicked?.Invoke(this, e);
    }
}
