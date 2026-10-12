using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;

namespace CommunityHub.Ui.Views;

public partial class BuildingMembershipsPage : Page
{
    private readonly int _buildingId;
    private readonly BuildingMembershipDbRepository _membershipRepository;

    public BuildingMembershipsPage(int buildingId)
    {
        InitializeComponent();
        _buildingId = buildingId;
        _membershipRepository = new BuildingMembershipDbRepository();

        LoadMemberships();
    }

    private void LoadMemberships()
    {
        MembershipsList.ItemsSource = _membershipRepository.GetMembershipsForBuilding(_buildingId);
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }
}
