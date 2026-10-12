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
    public partial class AssemblyDetailsPage : Page
    {
        private readonly IAssemblyService? _service;
        private readonly IHomeownersMeetingRepository? _meetingRepository;
        private readonly IBuildingRepository? _buildingRepository;
        private readonly long _meetingId;
        private readonly long _managerId;

        public HomeownersMeetingDTO? Meeting { get; private set; }

        public AssemblyDetailsPage() : this(0) { }

        public AssemblyDetailsPage(long meetingId)
        {
            InitializeComponent();
            _meetingId = meetingId;
            _managerId = UserSession.CurrentUser?.Id ?? 0;

            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                _service = ManagerDependencyInjector.CreateInstance<IAssemblyService>();
                _meetingRepository = ManagerDependencyInjector.CreateInstance<IHomeownersMeetingRepository>();
                _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
                this.Loaded += async (s, e) => await LoadDetailsAsync();
            }
        }

        private async Task LoadDetailsAsync()
        {
            if (_service == null || _meetingRepository == null || _buildingRepository == null) return;

            Meeting = await _service.GetAssemblyDetailsAsync(_meetingId, _managerId);
            if (Meeting == null) return;

            // Get building info
            var building = _buildingRepository.GetById((int)Meeting.BuildingId);
            string buildingLabel = building != null ? $"{building.Address}, {building.Settlement}" : $"Building #{Meeting.BuildingId}";

            TbBuilding.Text = buildingLabel;

            // Update status in the header border
            var statusBorder = (Border)((Grid)TbBuilding.Parent).Children[1];
            var statusText = (TextBlock)statusBorder.Child;
            string detailStatusLabel = Meeting.Status?.ToLower() switch
            {
                "scheduled" or "zakazana" => "Scheduled",
                "confirmed" or "potvrđena" => "Confirmed",
                "canceled" or "otkazana" => "Canceled",
                _ => Meeting.Status ?? ""
            };
            statusText.Text = detailStatusLabel;

            var detailStatusColor = Meeting.Status?.ToLower() switch
            {
                "scheduled" or "zakazana" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)),
                "confirmed" or "potvrđena" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 139, 34)),
                "canceled" or "otkazana" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 20)),
                _ => (System.Windows.Media.SolidColorBrush)System.Windows.Media.Brushes.Black
            };
            statusBorder.BorderBrush = detailStatusColor;
            statusText.Foreground = detailStatusColor;
            statusText.FontWeight = FontWeights.SemiBold;

            // Time in 24h format
            string timeStr = $"{(int)Meeting.ScheduledTime.TotalHours:D2}:{Meeting.ScheduledTime.Minutes:D2}";
            TbDateTime.Text = $"{Meeting.ScheduledDate:dd.MM.yyyy}  |  {timeStr}";

            TbTopics.Text = Meeting.Topics.Count > 0
                ? string.Join("\n", Meeting.Topics.ConvertAll(t => t.TopicText))
                : "No topics";

            // Get REAL attendance data from DB
            var attendanceRecords = await _meetingRepository.GetAllAttendanceForMeetingAsync(_meetingId);
            // tenant_id values that confirmed attendance
            var confirmedTenantIds = new HashSet<long>(
                attendanceRecords.Where(a => a.Attended).Select(a => a.TenantId)
            );

            // Get flats with their tenant user_ids directly from DB
            var flatInfoList = GetFlatsWithTenantIds((int)Meeting.BuildingId);

            // Group by flat number
            var flatGroups = flatInfoList
                .GroupBy(f => f.FlatNumber)
                .OrderBy(g => g.Key)
                .ToList();

            SpAttendance.Children.Clear();

            foreach (var flatGroup in flatGroups)
            {
                int flatNumber = flatGroup.Key;
                int floorNumber = flatGroup.First().FloorNumber;

                // A flat is confirmed if ANY tenant in that flat confirmed attendance
                bool isConfirmed = flatGroup.Any(f => confirmedTenantIds.Contains(f.UserId));

                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(8)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var chk = new CheckBox
                {
                    IsChecked = isConfirmed,
                    IsEnabled = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(chk, 0);
                grid.Children.Add(chk);

                var label = new TextBlock
                {
                    Text = $"Flat {flatNumber}  |  Floor {floorNumber}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };
                Grid.SetColumn(label, 1);
                grid.Children.Add(label);

                var status = new TextBlock
                {
                    Text = isConfirmed ? "Confirmed" : "Pending",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = isConfirmed ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isConfirmed
                        ? System.Windows.Media.Brushes.Green
                        : System.Windows.Media.Brushes.Gray,
                    FontSize = 12
                };
                Grid.SetColumn(status, 2);
                grid.Children.Add(status);

                border.Child = grid;
                SpAttendance.Children.Add(border);
            }

            if (flatGroups.Count == 0)
            {
                SpAttendance.Children.Add(new TextBlock
                {
                    Text = "No flats registered in this building.",
                    FontSize = 12,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }
        }

        /// <summary>
        /// Gets flat info with actual user_ids from building_memberships table.
        /// This is needed to match tenant_id in meeting_attendance with the correct flat.
        /// </summary>
        private static List<FlatTenantInfo> GetFlatsWithTenantIds(int buildingId)
        {
            var result = new List<FlatTenantInfo>();
            using var connection = Application.Database.PostgresConnection.CreateConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT bm.user_id, bm.flat_number, bm.floor_number
                FROM building_memberships bm
                WHERE bm.building_id = @buildingId
                ORDER BY bm.flat_number";

            var param = command.CreateParameter();
            param.ParameterName = "@buildingId";
            param.Value = buildingId;
            command.Parameters.Add(param);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new FlatTenantInfo
                {
                    UserId = System.Convert.ToInt64(reader["user_id"]),
                    FlatNumber = System.Convert.ToInt32(reader["flat_number"]),
                    FloorNumber = System.Convert.ToInt32(reader["floor_number"])
                });
            }
            return result;
        }

        private class FlatTenantInfo
        {
            public long UserId { get; set; }
            public int FlatNumber { get; set; }
            public int FloorNumber { get; set; }
        }
    }
}
