using System.Windows;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class CreateEventWindow : Window
    {
        public CreateEventWindow(long userId, long neighborhoodId)
        {
            InitializeComponent();
            DataContext = new CreateEventViewModel(userId, neighborhoodId, this);
        }
    }
}