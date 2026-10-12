using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityHub.Ui.Views.ManagerViews;
using CommunityHub.Ui.Views.ManagerViews.Assemblies;
using CommunityHub.Ui.ViewModels.ManagerViewModels;

namespace CommunityHub.Ui.Views;

public partial class ManagerHomePage : Page
{
    private readonly ManagerHomeViewModel _viewModel;

    public ManagerHomePage()
    {
        InitializeComponent();
        _viewModel = new ManagerHomeViewModel();
        DataContext = _viewModel;

        this.Loaded += async (s, e) =>
        {
            await _viewModel.LoadDashboardAsync();
            TxtBuildingCount.Text = _viewModel.MembershipRequestCount.ToString();
            TxtRequestsCount.Text = _viewModel.RentalRequestCount.ToString();
            TxtAssembliesCount.Text = _viewModel.AssembliesCount.ToString();
            TxtProblemsCount.Text = _viewModel.ProblemsCount.ToString();
            BuildingsItemsControl.ItemsSource = _viewModel.Buildings;
        };
    }

    // ---------------------------------------------------------------
    // Stats row click handlers
    // ---------------------------------------------------------------
    private void StatMembership_Click(object sender, MouseButtonEventArgs e)
        => NavigationService.Navigate(new ManageBuildingRequestsPage());

    private void StatRental_Click(object sender, MouseButtonEventArgs e)
        => NavigationService.Navigate(new ManagerViews.RentalRequests.RentalRequestsPage());

    private void StatAssemblies_Click(object sender, MouseButtonEventArgs e)
        => NavigationService.Navigate(new AssembliesPage());

    private void StatProblems_Click(object sender, MouseButtonEventArgs e)
        => NavigationService.Navigate(new UnresolvedProblemsPage());

    // ---------------------------------------------------------------
    // Building card buttons
    // ---------------------------------------------------------------
    private void OpenMemberships_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
            NavigationService.Navigate(new BuildingMembershipsPage(System.Convert.ToInt32(btn.Tag)));
    }

    private void OpenCommonRooms_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
            NavigationService.Navigate(new CommonRoomsPage(System.Convert.ToInt32(btn.Tag)));
    }

    private void OpenBulletinBoard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
            NavigationService.Navigate(new BulletinBoardPage(System.Convert.ToInt32(btn.Tag)));
    }

    private void RegisterBuildingButton_Click(object sender, RoutedEventArgs e)
        => NavigationService.Navigate(new CreateBuildingPage());

    private void ManageBuildingRequestsButton_Click(object sender, RoutedEventArgs e)
        => NavigationService.Navigate(new ManageBuildingRequestsPage());

    private void OpenAssemblies_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
            NavigationService.Navigate(new ScheduleAssemblyPage(System.Convert.ToInt32(btn.Tag)));
    }

    private void OpenProblems_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null)
            NavigationService.Navigate(new ProblemReportsPage(System.Convert.ToInt32(btn.Tag)));
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }
}
