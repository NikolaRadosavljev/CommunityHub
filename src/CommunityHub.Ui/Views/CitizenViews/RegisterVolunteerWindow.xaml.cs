using System.Windows;
using CommunityHub.Application.Domain;
using CommunityHub.Ui.ViewModels.CitizenViewModels;

namespace CommunityHub.Ui.Views.CitizenViews
{
    public partial class RegisterVolunteerWindow : Window
    {
        public RegisterVolunteerWindow(long userId, Event ev)
        {
            InitializeComponent();
            this.DataContext = new RegisterVolunteerViewModel(userId, ev, this);
        }
    }
}