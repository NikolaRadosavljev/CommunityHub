using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.Views.ManagerViews.Assemblies
{
    public partial class ProposedTopicsPage : Page
    {
        private readonly IHomeownersMeetingRepository? _meetingRepository;
        private readonly IBuildingRepository? _buildingRepository;
        private readonly IAssemblyService? _service;
        private readonly long _meetingId;
        private readonly long _managerId;

        public ProposedTopicsPage() : this(0) { }

        public ProposedTopicsPage(long meetingId)
        {
            InitializeComponent();
            _meetingId = meetingId;
            _managerId = UserSession.CurrentUser?.Id ?? 0;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _meetingRepository = ManagerDependencyInjector.CreateInstance<IHomeownersMeetingRepository>();
                _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
                _service = ManagerDependencyInjector.CreateInstance<IAssemblyService>();

                this.Loaded += async (s, e) => await LoadProposedTopicsAsync();
            }
        }

        private async Task LoadProposedTopicsAsync()
        {
            if (_meetingRepository == null || _buildingRepository == null || _service == null) return;

            // Get meeting info for header
            var meeting = await _service.GetAssemblyDetailsAsync(_meetingId, _managerId);
            if (meeting == null) return;

            var building = _buildingRepository.GetById((int)meeting.BuildingId);
            string buildingLabel = building != null ? $"{building.Address}, {building.Settlement}" : $"Building #{meeting.BuildingId}";
            string timeStr = $"{(int)meeting.ScheduledTime.TotalHours:D2}:{meeting.ScheduledTime.Minutes:D2}";
            TbAssemblyInfo.Text = $"Assembly:  {meeting.ScheduledDate:dd.MM.yyyy}  |  {timeStr}  |  {buildingLabel}";

            // Check 24h deadline
            var meetingDateTime = meeting.ScheduledDate.Date.Add(meeting.ScheduledTime);
            bool isWithin24h = (meetingDateTime - DateTime.Now).TotalHours < 24;

            // Get all topics with is_accepted status
            var allTopics = await _meetingRepository.GetMeetingTopicsWithStatusAsync(_meetingId);

            // Filter: only show topics proposed by tenants (not by the manager)
            var proposedByTenants = allTopics.Where(t => t.ProposedByTenantId != _managerId).ToList();

            // Build tenant name lookup from building_memberships
            var tenantInfoMap = GetTenantInfoForBuilding((int)meeting.BuildingId);

            SpTopics.Children.Clear();

            if (proposedByTenants.Count == 0)
            {
                TbEmpty.Visibility = Visibility.Visible;
                SpTopics.Children.Add(TbEmpty);
                return;
            }

            foreach (var topic in proposedByTenants)
            {
                string tenantLabel;
                if (tenantInfoMap.TryGetValue(topic.ProposedByTenantId, out var info))
                {
                    tenantLabel = $"{info.Name} (Flat {info.FlatNumber})";
                }
                else
                {
                    tenantLabel = $"Tenant #{topic.ProposedByTenantId}";
                }

                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(10)
                };

                var stack = new StackPanel();

                // Row 1: Tenant name + "Added" label if accepted
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tenantText = new TextBlock
                {
                    Text = tenantLabel,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tenantText, 0);
                headerGrid.Children.Add(tenantText);

                if (topic.IsAccepted)
                {
                    var addedLabel = new TextBlock
                    {
                        Text = "Added",
                        FontSize = 12,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontStyle = FontStyles.Italic
                    };
                    Grid.SetColumn(addedLabel, 1);
                    headerGrid.Children.Add(addedLabel);
                }

                stack.Children.Add(headerGrid);

                // Row 2: Topic text (italic, with quotes)
                var topicTextBlock = new TextBlock
                {
                    Text = $"\"{topic.TopicText}\"",
                    FontSize = 12,
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 6)
                };
                stack.Children.Add(topicTextBlock);

                // Row 3: "+ Add as Topic" button (only if not yet accepted AND within deadline)
                if (!topic.IsAccepted)
                {
                    if (isWithin24h)
                    {
                        var expiredLabel = new TextBlock
                        {
                            Text = "Deadline passed (< 24h)",
                            FontSize = 11,
                            Foreground = System.Windows.Media.Brushes.Red,
                            FontStyle = FontStyles.Italic
                        };
                        stack.Children.Add(expiredLabel);
                    }
                    else
                    {
                        var addBtn = new Button
                        {
                            Content = "+ Add as Topic",
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Background = System.Windows.Media.Brushes.White,
                            Foreground = System.Windows.Media.Brushes.Black,
                            BorderBrush = System.Windows.Media.Brushes.Black,
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(10, 4, 10, 4),
                            FontSize = 12,
                            FontWeight = FontWeights.SemiBold,
                            Cursor = System.Windows.Input.Cursors.Hand,
                            Tag = topic.Id
                        };
                        addBtn.Click += async (s, e) => await AcceptTopicAsync(addBtn, topic.Id);
                        stack.Children.Add(addBtn);
                    }
                }

                border.Child = stack;
                SpTopics.Children.Add(border);
            }
        }

        private async Task AcceptTopicAsync(Button button, long topicId)
        {
            if (_meetingRepository == null) return;

            try
            {
                await _meetingRepository.AcceptProposedTopicAsync(topicId);

                // Refresh the page
                await LoadProposedTopicsAsync();
            }
            catch (Exception ex)
            {
                ManagerMainWindow.GetFrom(this)?.ShowErrorToast($"Error adding topic: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets tenant name and flat info from building_memberships.
        /// </summary>
        private static Dictionary<long, TenantDisplayInfo> GetTenantInfoForBuilding(int buildingId)
        {
            var result = new Dictionary<long, TenantDisplayInfo>();
            using var connection = Application.Database.PostgresConnection.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT bm.user_id, u.name, u.surname, bm.flat_number, bm.floor_number
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
                {
                    result[userId] = new TenantDisplayInfo
                    {
                        Name = $"{reader["name"]} {reader["surname"]}",
                        FlatNumber = Convert.ToInt32(reader["flat_number"]),
                        FloorNumber = Convert.ToInt32(reader["floor_number"])
                    };
                }
            }
            return result;
        }

        private class TenantDisplayInfo
        {
            public string Name { get; set; } = string.Empty;
            public int FlatNumber { get; set; }
            public int FloorNumber { get; set; }
        }
    }
}
