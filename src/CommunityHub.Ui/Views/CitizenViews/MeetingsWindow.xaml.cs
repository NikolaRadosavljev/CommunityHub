using System.Windows;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class MeetingsWindow : Window
    {
        public MeetingsWindow(long userId)
        {
            InitializeComponent();

            this.DataContext = new MeetingsViewModel(userId, () => this.Close());
        }
    }
}