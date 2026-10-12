using System.Windows;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class NeighborhoodMembershipRequestViewWindow : Window
    {
        public NeighborhoodMembershipRequestViewWindow(long userId)
        {
            InitializeComponent();
            this.DataContext = new NeighborhoodMembershipRequestViewModel(userId, () => this.Close());
        }
    }
}