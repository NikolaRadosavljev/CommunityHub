using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.Views.ManagerViews.Assemblies
{
    public partial class ScheduleAssemblyPage : Page
    {
        private readonly IAssemblyService? _service;
        private readonly IBuildingRepository? _buildingRepository;
        private readonly long _managerId;
        private readonly int _preselectedBuildingId;
        private List<Building> _buildings = new();
        internal CancellationTokenSource? _demoCts;

        public ScheduleAssemblyPage() : this(0) { }

        public ScheduleAssemblyPage(int buildingId)
        {
            InitializeComponent();
            _preselectedBuildingId = buildingId;
            _managerId = UserSession.CurrentUser?.Id ?? 0;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _service = ManagerDependencyInjector.CreateInstance<IAssemblyService>();
                _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();

                BtnAddTopic.Click += (s, e) => AddTopic();
                BtnScheduleNow.Click += async (s, e) => await ScheduleFromUiAsync();
                BtnCancel.Click += (s, e) => { if (NavigationService?.CanGoBack == true) NavigationService.GoBack(); };

                this.Loaded += (s, e) => LoadBuildings();
            }
        }

        private void LoadBuildings()
        {
            if (_buildingRepository == null) return;

            _buildings = _buildingRepository.GetByManagerId(_managerId);
            CbBuilding.ItemsSource = _buildings;

            if (_preselectedBuildingId > 0)
            {
                // Use SelectedIndex — more reliable than SelectedItem for WPF ComboBox
                for (int i = 0; i < _buildings.Count; i++)
                {
                    if (_buildings[i].Id == _preselectedBuildingId)
                    {
                        CbBuilding.SelectedIndex = i;
                        CbBuilding.IsEnabled = false;
                        break;
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // Demo
        // ---------------------------------------------------------------
        public async void StartDemo()
        {
            if (_demoCts != null) { _demoCts.Cancel(); return; }
            _demoCts = new CancellationTokenSource();
            var token = _demoCts.Token;

            // Save originals
            var origBuilding = CbBuilding.SelectedItem;
            var origDate = DpDate.SelectedDate;
            string origTime = TbTime.Text;
            bool origBuildingEnabled = CbBuilding.IsEnabled;
            var origTopicItems = new List<object>();
            foreach (var item in IcTopics.Items) origTopicItems.Add(item);

            // Lock inputs
            CbBuilding.IsEnabled = false;
            DpDate.IsEnabled = false;
            TbTime.IsReadOnly = true;
            BtnAddTopic.IsEnabled = false;
            TbNewTopic.IsReadOnly = true;

            try
            {
                // Clear fields
                CbBuilding.SelectedItem = null;
                DpDate.SelectedDate = null;
                TbTime.Text = "";
                IcTopics.Items.Clear();

                // Demo: select first building
                await Task.Delay(400, token);
                if (_buildings.Count > 0)
                {
                    CbBuilding.SelectedIndex = 0;
                    await Task.Delay(600, token);
                }

                // Demo: set date to 2 weeks from now
                var demoDate = DateTime.Today.AddDays(14);
                DpDate.SelectedDate = demoDate;
                await Task.Delay(600, token);

                // Demo: type time
                string demoTime = "18:00";
                foreach (char c in demoTime)
                {
                    token.ThrowIfCancellationRequested();
                    TbTime.Text += c;
                    await Task.Delay(120, token);
                }
                await Task.Delay(400, token);

                // Demo: add topics
                var demoTopics = new[] { "Yearly maintenance plan", "Building renovation budget", "Parking lot rules" };
                foreach (var topic in demoTopics)
                {
                    TbNewTopic.Text = "";
                    TbNewTopic.Foreground = System.Windows.Media.Brushes.Black;
                    foreach (char c in topic)
                    {
                        token.ThrowIfCancellationRequested();
                        TbNewTopic.Text += c;
                        await Task.Delay(50, token);
                    }
                    await Task.Delay(300, token);
                    AddTopic();
                    await Task.Delay(400, token);
                }

                await Task.Delay(2000, token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                // Restore
                IcTopics.Items.Clear();
                foreach (var item in origTopicItems) IcTopics.Items.Add(item);
                CbBuilding.SelectedItem = origBuilding;
                DpDate.SelectedDate = origDate;
                TbTime.Text = origTime;
                CbBuilding.IsEnabled = origBuildingEnabled;
                DpDate.IsEnabled = true;
                TbTime.IsReadOnly = false;
                BtnAddTopic.IsEnabled = true;
                TbNewTopic.IsReadOnly = false;
                TbNewTopic.Text = "+ Add new topic...";
                TbNewTopic.Foreground = System.Windows.Media.Brushes.Gray;

                _demoCts?.Dispose();
                _demoCts = null;
            }
        }

        // ---------------------------------------------------------------
        // Topic management
        // ---------------------------------------------------------------
        private void TbNewTopic_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TbNewTopic.Text == "+ Add new topic...")
            {
                TbNewTopic.Text = "";
                TbNewTopic.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TbNewTopic_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbNewTopic.Text))
            {
                TbNewTopic.Text = "+ Add new topic...";
                TbNewTopic.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void AddTopic()
        {
            var txt = TbNewTopic.Text?.Trim();
            if (string.IsNullOrEmpty(txt) || txt == "+ Add new topic...") return;

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8, 4, 8, 4),
                Background = System.Windows.Media.Brushes.White
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tb = new TextBlock { Text = txt, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(tb, 0);
            grid.Children.Add(tb);

            var btnRemove = new Button
            {
                Content = "x",
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontWeight = FontWeights.Bold
            };
            btnRemove.Click += (s, e) => IcTopics.Items.Remove(border);
            Grid.SetColumn(btnRemove, 1);
            grid.Children.Add(btnRemove);

            border.Child = grid;
            IcTopics.Items.Add(border);
            TbNewTopic.Text = string.Empty;
        }

        private List<string> GetTopicsFromUi()
        {
            var topics = new List<string>();
            foreach (var item in IcTopics.Items)
            {
                if (item is Border border && border.Child is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            topics.Add(tb.Text);
                            break;
                        }
                    }
                }
            }
            return topics;
        }

        // ---------------------------------------------------------------
        // Schedule
        // ---------------------------------------------------------------
        private async Task ScheduleFromUiAsync()
        {
            var mainWin = ManagerMainWindow.GetFrom(this);

            if (CbBuilding.SelectedItem is not Building selectedBuilding)
            {
                mainWin?.ShowErrorToast("Please select a building.");
                return;
            }

            if (DpDate.SelectedDate == null)
            {
                mainWin?.ShowErrorToast("Please select a date.");
                return;
            }

            // Parse 24h time format HH:mm
            if (!TryParse24hTime(TbTime.Text, out var time))
            {
                mainWin?.ShowErrorToast("Please enter time in 24h format (e.g. 10:00 or 22:00).");
                return;
            }

            var topics = GetTopicsFromUi();
            if (topics.Count == 0)
            {
                mainWin?.ShowErrorToast("Please add at least one topic.");
                return;
            }

            var dto = new CreateMeetingDTO
            {
                BuildingId = selectedBuilding.Id,
                ScheduledDate = DpDate.SelectedDate.Value.Date,
                ScheduledTime = time
            };

            try
            {
                if (_service != null)
                {
                    long meetingId = await _service.ScheduleAssemblyAsync(dto);

                    // Add official topics via repository
                    var meetingRepo = ManagerDependencyInjector.CreateInstance<IHomeownersMeetingRepository>();
                    foreach (var topicText in topics)
                    {
                        await meetingRepo.AddOfficialTopicAsync(meetingId, topicText, _managerId);
                    }

                    mainWin?.ShowSuccessToast("Assembly scheduled successfully.", goBack: true);
                }
            }
            catch (Exception ex)
            {
                mainWin?.ShowErrorToast($"Error scheduling assembly: {ex.Message}");
            }
        }

        private static bool TryParse24hTime(string input, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var parts = input.Trim().Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int hours) || !int.TryParse(parts[1], out int minutes)) return false;
            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return false;

            result = new TimeSpan(hours, minutes, 0);
            return true;
        }
    }
}
