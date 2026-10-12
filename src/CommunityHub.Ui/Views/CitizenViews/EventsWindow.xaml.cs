using CommunityHub.Ui.ViewModels;
using CommunityHub.Ui.ViewModels.CitizenViewModels;
using System.Windows;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class EventsWindow : Window
    {
        public EventsWindow(long userId)
        {
            InitializeComponent();
            DataContext = new EventsViewModel(userId);
        }
    }
}