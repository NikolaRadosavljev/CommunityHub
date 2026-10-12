using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;

namespace CommunityHub.Ui.Views;

public partial class CommonRoomsPage : Page
{
    private readonly int _buildingId;
    private readonly ICommonSpaceService _commonSpaceService;
    private List<Application.Domain.CommonSpace> _spaces;

    public CommonRoomsPage(int buildingId)
    {
        InitializeComponent();
        _buildingId = buildingId;
        _commonSpaceService = ManagerDependencyInjector.CreateInstance<ICommonSpaceService>();
        _spaces = new List<Application.Domain.CommonSpace>();

        LoadCommonRooms();
    }

    private void LoadCommonRooms()
    {
        _spaces = _commonSpaceService.GetCommonSpacesForBuilding(_buildingId);

        var viewModels = _spaces.ConvertAll(s => new
        {
            s.Id,
            s.Name,
            s.Description,
            s.FloorNumber,
            RentalPeriod = s.GetRentalTypeLabel()
        });

        CommonRoomsItemsControl.ItemsSource = viewModels;
    }

    private void AddCommonRoom_Click(object sender, RoutedEventArgs e)
    {
        var addPage = new AddCommonSpacePage(_buildingId);
        addPage.OnCommonSpaceAdded += () => LoadCommonRooms(); // Refresh on return
        NavigationService.Navigate(addPage);
    }

    private void Calendar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int roomId)
        {
            var space = _spaces.Find(s => s.Id == roomId);
            if (space != null)
            {
                NavigationService.Navigate(new CommonRoomCalendarPage(space));
            }
        }
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
