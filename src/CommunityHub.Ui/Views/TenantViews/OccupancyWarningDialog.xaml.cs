using System.Windows;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class OccupancyWarningDialog : Window
    {
        public bool UserConfirmed { get; private set; }

        public OccupancyWarningDialog()
        {
            InitializeComponent();
            UserConfirmed = false;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            this.Close();
        }
    }
}
