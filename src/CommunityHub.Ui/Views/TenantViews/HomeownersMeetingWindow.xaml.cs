using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.DependencyInjection;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class HomeownersMeetingWindow : Window
    {
        private readonly long _tenantId;
        private long _selectedBuildingId = 0;
        private long _selectedMeetingId = 0;
        private readonly HomeownersMeetingService _meetingService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IUserRepository _userRepository;
        private List<BuildingDTO> _userBuildings;

        public HomeownersMeetingWindow(long tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _meetingService = DIConfiguration.Factory.CreateHomeownersMeetingService();
            _buildingRepository = DIConfiguration.Factory.CreateBuildingRepository();
            _userRepository = DIConfiguration.Factory.CreateUserRepository();
            _userBuildings = new List<BuildingDTO>();

            Loaded += HomeownersMeetingWindow_Loaded;
        }

        private async void HomeownersMeetingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserBuildings();
        }

        private void LoadUserBuildings()
        {
            try
            {
                var helpBoardService = new HelpBoardRepository();
                var allBuildings = _buildingRepository.Search(null, null, null, null);
                var userBuildings = allBuildings.Where(b => helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id)).ToList();

                _userBuildings = userBuildings.Select(b => new BuildingDTO
                {
                    Id = b.Id,
                    Address = b.Address,
                    Settlement = b.Settlement,
                    City = b.City,
                    Country = b.Country
                }).ToList();

                BuildingsListControl.ItemsSource = _userBuildings;

                if (_userBuildings.Count > 0)
                {
                    _selectedBuildingId = _userBuildings[0].Id;
                    LoadMeetings();

                    // Select the first RadioButton visually after UI renders
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var firstRadioButton = FindChild<RadioButton>(BuildingsListControl, null);
                        if (firstRadioButton != null)
                        {
                            firstRadioButton.IsChecked = true;
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading buildings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadMeetings()
        {
            try
            {
                if (_selectedBuildingId == 0)
                {
                    MeetingsListControl.ItemsSource = null;
                    MeetingsListControl.Visibility = Visibility.Collapsed;
                    EmptyMeetingsMessage.Visibility = Visibility.Visible;
                    ClearDetails();
                    return;
                }

                var meetings = await _meetingService.GetMeetingsByBuildingAsync(_selectedBuildingId, _tenantId);

                if (meetings.Count == 0)
                {
                    MeetingsListControl.Visibility = Visibility.Collapsed;
                    EmptyMeetingsMessage.Visibility = Visibility.Visible;
                    ClearDetails();
                }
                else
                {
                    MeetingsListControl.ItemsSource = meetings;
                    MeetingsListControl.Visibility = Visibility.Visible;
                    EmptyMeetingsMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading meetings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildingRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is long buildingId)
            {
                _selectedBuildingId = buildingId;
                LoadMeetings();
            }
        }

        private void MeetingBorder_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Get the data context which is HomeownersMeetingDTO
                if (border.DataContext is HomeownersMeetingDTO meeting)
                {
                    // Find the AttendanceSegment in this border and generate pie chart
                    var segment = FindChild<System.Windows.Shapes.Path>(border, "AttendanceSegment");
                    if (segment != null)
                    {
                        segment.Data = Geometry.Parse(GeneratePieChartPathData(meeting.AttendancePercentage));
                    }
                }
            }
        }

        private async void MeetingItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is long meetingId)
            {
                _selectedMeetingId = meetingId;

                // Get meeting data and generate pie chart
                var meeting = await _meetingService.GetMeetingAsync(meetingId, _tenantId);
                if (meeting != null)
                {
                    // Find the AttendanceSegment in the clicked border and generate pie chart
                    var segment = FindChild<System.Windows.Shapes.Path>(border, "AttendanceSegment");
                    if (segment != null)
                    {
                        segment.Data = Geometry.Parse(GeneratePieChartPathData(meeting.AttendancePercentage));
                    }
                }
            }
        }


        private void ClearDetails()
        {
            _selectedMeetingId = 0;
            EmptyDetailsMessage.Visibility = Visibility.Visible;
            MeetingsListControl.ItemsSource = null;
        }

        private async void ReportAttendance_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Get meeting ID from the button's parent Border
            var border = FindParent<Border>(button);
            if (border == null) return;

            long meetingId = 0;
            if (border.Tag is long id)
            {
                meetingId = id;
            }

            if (meetingId == 0)
            {
                MessageBox.Show("Could not determine meeting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _meetingService.ReportAttendanceAsync(meetingId, _tenantId, true);
                MessageBox.Show("Attendance reported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMeetings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reporting attendance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelAttendance_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            // Get meeting ID from the button's parent Border
            var border = FindParent<Border>(button);
            if (border == null) return;

            long meetingId = 0;
            if (border.Tag is long id)
            {
                meetingId = id;
            }

            if (meetingId == 0)
            {
                MessageBox.Show("Could not determine meeting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _meetingService.ReportAttendanceAsync(meetingId, _tenantId, false);
                MessageBox.Show("Attendance cancelled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMeetings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cancelling attendance: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ProposeTopic_Click(object sender, RoutedEventArgs e)
        {
            // Find the TextBox in the button's parent Border
            var button = sender as Button;
            if (button == null) return;

            // Navigate up to Border, then find TextBox
            var border = FindParent<Border>(button);
            if (border == null) return;

            var topicInput = FindChild<TextBox>(border, "ProposedTopicInput");
            if (topicInput == null) return;

            var topicText = topicInput.Text?.Trim();

            if (string.IsNullOrEmpty(topicText))
            {
                MessageBox.Show("Please enter a topic.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get meeting ID from the border's Tag attribute
            long meetingId = 0;
            if (border.Tag is long id)
            {
                meetingId = id;
            }

            if (meetingId == 0)
            {
                MessageBox.Show("Could not determine meeting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                await _meetingService.ProposeMeetingTopicAsync(meetingId, _tenantId, topicText);
                MessageBox.Show("Topic proposed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                topicInput.Clear();
                LoadMeetings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error proposing topic: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                T parentT = parent as T;
                if (parentT != null) return parentT;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "Home":
                        var homeWindow = new TenantWindow(_tenantId);
                        homeWindow.Show();
                        this.Close();
                        break;
                    case "MyBuilding":
                        var buildingWindow = new MyBuildingsWindow(_tenantId);
                        buildingWindow.Show();
                        this.Close();
                        break;
                    case "NoticeBoard":
                        var helpBoardService = new HelpBoardRepository();
                        var buildingRepo = DIConfiguration.Factory.CreateBuildingRepository();
                        var userBuilding = buildingRepo.Search(null, null, null, null)
                            .OrderBy(b => b.Id)
                            .FirstOrDefault(b => helpBoardService.IsUserMemberOfBuilding(_tenantId, b.Id));

                        if (userBuilding == null)
                        {
                            MessageBox.Show("You must be a member of a building to access the notice board.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var noticeBoardWindow = new NoticeBoardWindow(_tenantId, userBuilding.Id, DIConfiguration.Factory.CreateHelpAdService());
                        noticeBoardWindow.Show();
                        this.Close();
                        break;
                    case "Requests":
                        var requestsWindow = new TenantBuildingRequestsWindow(_tenantId);
                        requestsWindow.Show();
                        this.Close();
                        break;
                    case "RentingRooms":
                        var rentalWindow = new RentalRequestsWindow(_tenantId);
                        rentalWindow.Show();
                        this.Close();
                        break;
                    case "Homeowners":
                        // Already on this page
                        break;
                    case "Problem":
                        var problemWindow = new ProblemWindow(_tenantId);
                        problemWindow.Show();
                        this.Close();
                        break;
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectedMeetingId = 0;
            ClearDetails();
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = childType;
                        break;
                    }
                }
                else
                {
                    foundChild = childType;
                    break;
                }
            }

            return foundChild;
        }

        private string GeneratePieChartPathData(decimal percentage)
        {
            if (percentage <= 0)
                return "";

            if (percentage >= 100)
            {
                // Full circle
                return "M 50,10 A 40,40 0 1 1 49.999,10";
            }

            double angle = (double)percentage * 360.0 / 100.0;
            double angleRad = angle * System.Math.PI / 180.0;

            double centerX = 50;
            double centerY = 50;
            double radius = 40;

            double startX = centerX;
            double startY = centerY - radius;

            double endX = centerX + radius * System.Math.Sin(angleRad);
            double endY = centerY - radius * System.Math.Cos(angleRad);

            int largeArcFlag = angle > 180 ? 1 : 0;

            string pathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "M {0:F1},{1:F1} L {2:F1},{3:F1} A {4:F1},{4:F1} 0 {5} 1 {6:F1},{7:F1} Z",
                centerX, centerY,
                startX, startY,
                radius,
                largeArcFlag,
                endX, endY);

            return pathData;
        }
    }
}
