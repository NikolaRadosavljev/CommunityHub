using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CommunityHub.Ui.Views.ManagerViews
{
    public partial class ToastNotification : UserControl
    {
        private CancellationTokenSource? _autoDismissCts;

        public ToastNotification()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show a SUCCESS (green) toast that auto-dismisses after 5 seconds.
        /// </summary>
        public void ShowSuccess(string message, string title = "Success")
        {
            Show(title, message, isError: false);
        }

        /// <summary>
        /// Show an ERROR (red) toast that stays until the user clicks X.
        /// </summary>
        public void ShowError(string message, string title = "Error")
        {
            Show(title, message, isError: true);
        }

        private void Show(string title, string message, bool isError)
        {
            // Cancel any previous auto-dismiss timer
            _autoDismissCts?.Cancel();
            _autoDismissCts?.Dispose();
            _autoDismissCts = null;

            TbTitle.Text = title;
            TbMessage.Text = message;

            if (isError)
            {
                ToastBorder.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // red
                TbIcon.Text = "✕";
            }
            else
            {
                ToastBorder.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // green
                TbIcon.Text = "✓";
            }

            Visibility = Visibility.Visible;

            if (!isError)
            {
                // Auto-dismiss after 5 seconds
                _autoDismissCts = new CancellationTokenSource();
                var token = _autoDismissCts.Token;
                _ = AutoDismissAsync(token);
            }
        }

        private async Task AutoDismissAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(5000, token);
                if (!token.IsCancellationRequested)
                {
                    Dispatcher.Invoke(() => Hide());
                }
            }
            catch (TaskCanceledException) { }
        }

        public void Hide()
        {
            _autoDismissCts?.Cancel();
            _autoDismissCts?.Dispose();
            _autoDismissCts = null;
            Visibility = Visibility.Collapsed;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
