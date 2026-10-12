using CommunityHub.Ui.ViewModels;
using CommunityHub.Ui.ViewModels.CitizenViewModels;
using System.Windows;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class NeighborhoodsWindow : Window
    {
        public NeighborhoodsWindow(long userId)
        {
            InitializeComponent();
            DataContext = new NeighborhoodsViewModel(userId);
        }
    }
}