using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels.ManagerViewModels;

namespace CommunityHub.Ui.Views.ManagerViews.Assemblies
{
    public partial class AssembliesPage : Page
    {
        private readonly AssembliesViewModel _viewModel;
        private readonly int _buildingId;

        /// <summary>All buildings mode — shows assemblies from every building this manager owns.</summary>
        public AssembliesPage() : this(0) { }

        /// <summary>
        /// Single-building mode when buildingId > 0; all-buildings mode when buildingId == 0.
        /// </summary>
        public AssembliesPage(int buildingId)
        {
            InitializeComponent();
            _buildingId = buildingId;
            _viewModel = new AssembliesViewModel(buildingId);
            DataContext = _viewModel;

            BtnSchedule.Click += (s, e) => NavigateToSchedule();

            this.Loaded += async (s, e) => await LoadAssembliesAsync();
        }

        private async Task LoadAssembliesAsync()
        {
            try
            {
                await _viewModel.LoadAssembliesAsync();

                SpMeetings.Children.Clear();
                foreach (var m in _viewModel.Meetings)
                {
                    string buildingLabel = _viewModel.BuildingAddressMap.TryGetValue(m.BuildingId, out var addr) ? addr : $"Building #{m.BuildingId}";

                    var border = new Border
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = System.Windows.Media.Brushes.Black,
                        Margin = new Thickness(0, 0, 0, 12),
                        Padding = new Thickness(8)
                    };

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // Row 0: Building & Status
                    var buildingText = new TextBlock { Text = buildingLabel, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
                    Grid.SetRow(buildingText, 0); Grid.SetColumn(buildingText, 0);
                    grid.Children.Add(buildingText);

                    string assemblyStatusLabel = m.Status?.ToLower() switch
                    {
                        "scheduled" or "zakazana" => "Scheduled",
                        "confirmed" or "potvrđena" => "Confirmed",
                        "canceled" or "otkazana" => "Canceled",
                        _ => m.Status ?? ""
                    };
                    var assemblyStatusColor = m.Status?.ToLower() switch
                    {
                        "scheduled" or "zakazana" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                        "confirmed" or "potvrđena" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 139, 34)),
                        "canceled" or "otkazana" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 20)),
                        _ => System.Windows.Media.Brushes.Black
                    };
                    var statusBorder = new Border { BorderBrush = assemblyStatusColor, BorderThickness = new Thickness(1), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(0, 0, 0, 4) };
                    var statusText = new TextBlock { Text = assemblyStatusLabel, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = assemblyStatusColor, FontWeight = FontWeights.SemiBold };
                    statusBorder.Child = statusText;
                    Grid.SetRow(statusBorder, 0); Grid.SetColumn(statusBorder, 1);
                    grid.Children.Add(statusBorder);

                    // Row 1: Date | Time (24h format)
                    string timeStr = $"{(int)m.ScheduledTime.TotalHours:D2}:{m.ScheduledTime.Minutes:D2}";
                    var dateTimeText = new TextBlock { Text = $"{m.ScheduledDate:dd.MM.yyyy}  |  {timeStr}", FontSize = 12, Margin = new Thickness(0, 0, 0, 4) };
                    Grid.SetRow(dateTimeText, 1); Grid.SetColumn(dateTimeText, 0); Grid.SetColumnSpan(dateTimeText, 2);
                    grid.Children.Add(dateTimeText);

                    // Row 2: Topics
                    string topicsList = m.Topics.Count > 0
                        ? string.Join(", ", m.Topics.ConvertAll(t => t.TopicText))
                        : "No topics";
                    var topicsText = new TextBlock { Text = $"Topics: {topicsList}", FontSize = 12, Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap };
                    Grid.SetRow(topicsText, 2); Grid.SetColumn(topicsText, 0); Grid.SetColumnSpan(topicsText, 2);
                    grid.Children.Add(topicsText);

                    // Row 3: Attendance
                    var attendanceText = new TextBlock { Text = $"Attendance: {m.AttendanceCount} / {m.TotalStans} flats confirmed", FontSize = 12, Margin = new Thickness(0, 0, 0, 8) };
                    Grid.SetRow(attendanceText, 3); Grid.SetColumn(attendanceText, 0); Grid.SetColumnSpan(attendanceText, 2);
                    grid.Children.Add(attendanceText);

                    // Row 4: Buttons
                    var btnGrid = new Grid();
                    btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    Grid.SetRow(btnGrid, 4); Grid.SetColumn(btnGrid, 0); Grid.SetColumnSpan(btnGrid, 2);

                    var detailsBtn = new Button
                    {
                        Content = "Details",
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Background = System.Windows.Media.Brushes.White,
                        Foreground = System.Windows.Media.Brushes.Black,
                        BorderBrush = System.Windows.Media.Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(12, 4, 12, 4),
                        FontWeight = FontWeights.SemiBold
                    };
                    detailsBtn.Click += (s, e) => NavigateToDetails(m.Id);
                    Grid.SetColumn(detailsBtn, 0);
                    btnGrid.Children.Add(detailsBtn);

                    var proposedTopicsBtn = new Button
                    {
                        Content = "Proposed Topics",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Background = System.Windows.Media.Brushes.White,
                        Foreground = System.Windows.Media.Brushes.Black,
                        BorderBrush = System.Windows.Media.Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(12, 4, 12, 4),
                        FontWeight = FontWeights.SemiBold
                    };
                    long meetingIdForNav = m.Id;
                    proposedTopicsBtn.Click += (s, e) => NavigationService?.Navigate(new ProposedTopicsPage(meetingIdForNav));
                    Grid.SetColumn(proposedTopicsBtn, 1);
                    btnGrid.Children.Add(proposedTopicsBtn);

                    grid.Children.Add(btnGrid);
                    border.Child = grid;
                    SpMeetings.Children.Add(border);
                }

                if (_viewModel.Meetings.Count == 0)
                {
                    SpMeetings.Children.Add(new TextBlock
                    {
                        Text = "No assemblies scheduled.",
                        FontSize = 12,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }
            }
            catch (Exception ex)
            {
                ManagerMainWindow.GetFrom(this)?.ShowErrorToast($"Error loading assemblies: {ex.Message}");
            }
        }

        public void NavigateToSchedule()
        {
            if (_buildingId <= 0)
                NavigationService?.Navigate(new ScheduleAssemblyPage(0));
            else
                NavigationService?.Navigate(new ScheduleAssemblyPage(_buildingId));
        }

        public void NavigateToDetails(long meetingId)
        {
            NavigationService?.Navigate(new AssemblyDetailsPage(meetingId));
        }
    }
}
