using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;
using CommunityHub.Ui.ViewModels.ManagerViewModels;

namespace CommunityHub.Ui.Views.ManagerViews
{
    public partial class ProblemReportsPage : Page
    {
        private readonly ProblemReportsViewModel? _viewModel;
        private readonly int _buildingId;

        public ProblemReportsPage() : this(0) { }

        public ProblemReportsPage(int buildingId)
        {
            InitializeComponent();
            _buildingId = buildingId;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _viewModel = new ProblemReportsViewModel(buildingId);
                DataContext = _viewModel;
                this.Loaded += async (s, e) => await LoadProblemsAsync();
            }
        }

        private async Task LoadProblemsAsync()
        {
            if (_viewModel == null) return;

            await _viewModel.LoadProblemsAsync();
            ApplyFilters();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_viewModel == null) return;

            string statusFilter = (CbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Status";
            string priorityFilter = (CbPriorityFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Priority";

            IEnumerable<ProblemDTO> filtered = _viewModel.AllProblems;

            if (statusFilter == "Unresolved")
                filtered = filtered.Where(p => p.Status == "Unresolved");
            else if (statusFilter == "Potentially Resolved")
                filtered = filtered.Where(p => p.Status == "Potentially_resolved");
            else if (statusFilter == "Resolved")
                filtered = filtered.Where(p => p.Status == "Resolved");

            if (priorityFilter == "Urgent")
                filtered = filtered.Where(p => p.Priority == 3);
            else if (priorityFilter == "Resolve Soon")
                filtered = filtered.Where(p => p.Priority == 2);
            else if (priorityFilter == "Can Wait")
                filtered = filtered.Where(p => p.Priority == 1);

            var list = filtered.ToList();
            RenderProblems(list, showBuildingName: false);
        }

        private void RenderProblems(List<ProblemDTO> problems, bool showBuildingName)
        {
            // Get tenant info
            var tenantMap = GetTenantInfoForBuilding(_buildingId);

            SpProblems.Children.Clear();
            TbEmpty.Visibility = problems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (problems.Count == 0)
            {
                SpProblems.Children.Add(TbEmpty);
                return;
            }

            foreach (var p in problems)
            {
                SpProblems.Children.Add(BuildProblemCard(p, tenantMap, showBuildingName, null));
            }
        }

        internal static Border BuildProblemCard(ProblemDTO p, Dictionary<long, string> tenantMap, bool showBuildingName, string? buildingLabel)
        {
            string tenantName = tenantMap.TryGetValue(p.TenantId, out var name) ? name : $"Tenant #{p.TenantId}";
            string priorityLabel = p.Priority switch { 3 => "Priority: Urgent", 2 => "Priority: Resolve Soon", _ => "Priority: Can Wait" };
            string statusLabel = p.Status switch { "Unresolved" => "Unresolved", "Potentially_resolved" => "Potentially Resolved", "Resolved" => "Resolved", _ => p.Status };

            System.Windows.Media.SolidColorBrush statusColor = p.Status switch
            {
                "Resolved" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 139, 34)),       // green
                "Potentially_resolved" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)), // orange
                "Unresolved" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 20)),      // red
                _ => System.Windows.Media.Brushes.Black
            };

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };

            var stack = new StackPanel();

            // Title row + status
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            string title = showBuildingName && buildingLabel != null ? buildingLabel : "Problem";
            var titleText = new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var statusBorder = new Border
            {
                BorderBrush = statusColor,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 2, 8, 2)
            };
            statusBorder.Child = new TextBlock { Text = statusLabel, FontSize = 11, Foreground = statusColor, FontWeight = FontWeights.SemiBold };
            Grid.SetColumn(statusBorder, 1);
            headerGrid.Children.Add(statusBorder);
            stack.Children.Add(headerGrid);

            // Tenant | Date
            stack.Children.Add(new TextBlock
            {
                Text = $"{tenantName}  |  {p.ReportedDate:dd.MM.yyyy}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 2)
            });

            // Priority
            stack.Children.Add(new TextBlock
            {
                Text = priorityLabel,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 2)
            });

            // Description
            stack.Children.Add(new TextBlock
            {
                Text = $"\"{p.Description}\"",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 6)
            });

            // Mark as Resolved button (only if not already resolved/pot. resolved)
            if (p.Status == "Unresolved")
            {
                var resolveBtn = new Button
                {
                    Content = "Mark as Resolved",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = System.Windows.Media.Brushes.White,
                    Foreground = System.Windows.Media.Brushes.Black,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 4, 8, 4),
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = p.Id
                };
                resolveBtn.Click += async (s, e) =>
                {
                    try
                    {
                        var repo = ManagerDependencyInjector.CreateInstance<IProblemRepository>();
                        await repo.UpdateProblemStatusAsync(p.Id, "Potentially_resolved");
                        // Update card in-place
                        p.Status = "Potentially_resolved";
                        var statusTb = (TextBlock)statusBorder.Child;
                        statusTb.Text = "Potentially Resolved";
                        resolveBtn.Visibility = Visibility.Collapsed;

                        // Add waiting message
                        stack.Children.Add(new TextBlock
                        {
                            Text = "Waiting for tenant confirmation",
                            FontSize = 11,
                            FontStyle = FontStyles.Italic,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }
                    catch (Exception ex)
                    {
                        if (Window.GetWindow(stack) is ManagerMainWindow mw)
                            mw.ShowErrorToast($"Error: {ex.Message}");
                    }
                };
                stack.Children.Add(resolveBtn);
            }
            else if (p.Status == "Potentially_resolved")
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "Waiting for tenant confirmation",
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }

            border.Child = stack;
            return border;
        }

        internal static Dictionary<long, string> GetTenantInfoForBuilding(int buildingId)
        {
            var result = new Dictionary<long, string>();
            using var connection = Application.Database.PostgresConnection.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT bm.user_id, u.name, u.surname, bm.flat_number
                FROM building_memberships bm
                JOIN users u ON bm.user_id = u.id
                WHERE bm.building_id = @buildingId";
            var param = command.CreateParameter();
            param.ParameterName = "@buildingId";
            param.Value = buildingId;
            command.Parameters.Add(param);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                long userId = Convert.ToInt64(reader["user_id"]);
                if (!result.ContainsKey(userId))
                    result[userId] = $"{reader["name"]} {reader["surname"]} (Flat {reader["flat_number"]})";
            }
            return result;
        }
    }
}
