using System.Windows;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views
{
    public partial class NeighborhoodTrustRecordsWindow : Window
    {
        public NeighborhoodTrustRecordsWindow(long neighborhoodId)
        {
            InitializeComponent();

            this.DataContext = new NeighborhoodTrustRecordsViewModel(neighborhoodId, () => this.Close());
        }
    }
}