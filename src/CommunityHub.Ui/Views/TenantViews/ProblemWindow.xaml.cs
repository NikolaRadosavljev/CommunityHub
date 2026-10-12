using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.DependencyInjection;
using CommunityHub.Ui.ViewModels;

namespace CommunityHub.Ui.Views.TenantViews
{
    public partial class ProblemWindow : Window
    {
        private readonly long _tenantId;
        private long _selectedBuildingId = 0;
        private readonly ProblemService _problemService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IUserRepository _userRepository;
        private List<BuildingDTO> _userBuildings;

        public ProblemWindow(long tenantId)
        {
            InitializeComponent();
            _tenantId = tenantId;
            _problemService = new ProblemService(new ProblemDbRepository());
            _buildingRepository = DIConfiguration.Factory.CreateBuildingRepository();
            _userRepository = DIConfiguration.Factory.CreateUserRepository();
            _userBuildings = new List<BuildingDTO>();

            Loaded += ProblemWindow_Loaded;
        }

        private async void ProblemWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserBuildings();
            await LoadProblemsAsync();
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

                BuildingComboBox.ItemsSource = _userBuildings;
                if (_userBuildings.Count > 0)
                {
                    BuildingComboBox.SelectedIndex = 0;
                    _selectedBuildingId = _userBuildings[0].Id;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading buildings: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadProblemsAsync()
        {
            try
            {
                var problems = await _problemService.GetMyProblemsAsync(_tenantId, _selectedBuildingId);

                var displayProblems = problems.Select(p =>
                {
                    var building = _userBuildings.FirstOrDefault(b => b.Id == p.BuildingId);
                    var buildingName = building?.Address ?? $"Building #{p.BuildingId}";

                    return new ProblemDisplayModel
                    {
                        Id = p.Id,
                        BuildingId = p.BuildingId,
                        BuildingName = buildingName,
                        Description = p.Description,
                        Priority = p.Priority,
                        PriorityText = _problemService.GetPriorityText(p.Priority),
                        Status = p.Status,
                        StatusText = _problemService.GetStatusText(p.Status),
                        ReportedDate = p.ReportedDate,
                        ApproveButtonVisibility = p.Status == "Potentially_resolved" ? Visibility.Visible : Visibility.Collapsed,
                        DisapproveButtonVisibility = p.Status == "Potentially_resolved" ? Visibility.Visible : Visibility.Collapsed
                    };
                }).ToList();

                if (displayProblems.Count == 0)
                {
                    ProblemsListControl.Visibility = Visibility.Collapsed;
                    EmptyProblemsMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    ProblemsListControl.ItemsSource = displayProblems;
                    ProblemsListControl.Visibility = Visibility.Visible;
                    EmptyProblemsMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading problems: {ex.Message}");
            }
        }

        private async void BuildingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BuildingComboBox.SelectedValue is long selectedId)
            {
                _selectedBuildingId = selectedId;
                await LoadProblemsAsync();
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            var description = TxtDescription.Text?.Trim();
            if (string.IsNullOrEmpty(description))
            {
                ShowError("Description is required.");
                return;
            }

            int priority = 1;
            if (RbNeedsSoon.IsChecked == true)
                priority = 2;
            else if (RbUrgent.IsChecked == true)
                priority = 3;

            if (_selectedBuildingId == 0)
            {
                ShowError("Please select a building.");
                return;
            }

            try
            {
                var createDto = new CreateProblemDTO
                {
                    BuildingId = _selectedBuildingId,
                    TenantId = _tenantId,
                    Description = description,
                    Priority = priority
                };

                await _problemService.ReportProblemAsync(createDto);

                MessageBox.Show("Problem reported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear form
                TxtDescription.Clear();
                RbCanWait.IsChecked = true;

                // Reload problems
                await LoadProblemsAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to report problem: {ex.Message}");
            }
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long problemId)
            {
                var result = MessageBox.Show("Confirm that this problem is resolved?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _problemService.UpdateProblemStatusAsync(problemId, "Resolved");
                        MessageBox.Show("Problem marked as resolved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadProblemsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating problem: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DisapproveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long problemId)
            {
                var result = MessageBox.Show("Confirm that this problem is NOT resolved?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _problemService.UpdateProblemStatusAsync(problemId, "Unresolved");
                        MessageBox.Show("Problem marked as unresolved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadProblemsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating problem: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
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
                        var noticeBoardWindow = new NoticeBoardWindow(_tenantId, _selectedBuildingId, DIConfiguration.Factory.CreateHelpAdService());
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
                        var homeownersWindow = new HomeownersMeetingWindow(_tenantId);
                        homeownersWindow.Show();
                        this.Close();
                        break;
                    case "Problem":
                        var problemWindow = new ProblemWindow(_tenantId);
                        problemWindow.Show();
                        this.Close();
                        break;
                }
            }
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            var notificationsWindow = new NotificationsWindow(_tenantId, _selectedBuildingId, DIConfiguration.Factory.CreateHelpAdService());
            notificationsWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CommunityHub.Ui.Security.UserSession.SignOut();
            var loginForm = new LogInForm();
            loginForm.Show();
            this.Close();
        }
    }

    // ViewModel for problem display
    public class ProblemDisplayModel
    {
        public long Id { get; set; }
        public long BuildingId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string PriorityText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
        public Visibility ApproveButtonVisibility { get; set; } = Visibility.Collapsed;
        public Visibility DisapproveButtonVisibility { get; set; } = Visibility.Collapsed;
    }
}
