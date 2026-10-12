using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Infrastructure;
using CommunityHub.Ui.Security;

namespace CommunityHub.Ui.ViewModels.ManagerViewModels
{
    public class BuildingProblemsGroup
    {
        public string BuildingLabel { get; set; } = string.Empty;
        public ObservableCollection<ProblemDisplayItem> Problems { get; set; } = new();
    }

    public class UnresolvedProblemsViewModel : ViewModelBase
    {
        private readonly IProblemRepository _problemRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly long _managerId;

        private ObservableCollection<BuildingProblemsGroup> _buildingProblems = new();
        public ObservableCollection<BuildingProblemsGroup> BuildingProblems
        {
            get => _buildingProblems;
            set { _buildingProblems = value; OnPropertyChanged(); }
        }

        public UnresolvedProblemsViewModel()
        {
            _problemRepository = ManagerDependencyInjector.CreateInstance<IProblemRepository>();
            _buildingRepository = ManagerDependencyInjector.CreateInstance<IBuildingRepository>();
            _managerId = UserSession.CurrentUser?.Id ?? 0;
        }

        public async Task LoadUnresolvedProblemsAsync()
        {
            var buildings = _buildingRepository.GetByManagerId(_managerId);
            var groups = new ObservableCollection<BuildingProblemsGroup>();

            foreach (var building in buildings)
            {
                var unresolvedProblems = await _problemRepository.GetUnresolvedProblemsForBuildingAsync(building.Id);

                if (unresolvedProblems.Count == 0)
                    continue;

                var displayItems = unresolvedProblems.Select(p => new ProblemDisplayItem
                {
                    Id = p.Id,
                    TenantName = GetTenantName(p.TenantId),
                    Description = p.Description,
                    Priority = p.Priority,
                    PriorityLabel = GetPriorityLabel(p.Priority),
                    Status = p.Status,
                    StatusLabel = GetStatusLabel(p.Status),
                    ReportedDate = p.ReportedDate
                }).ToList();

                groups.Add(new BuildingProblemsGroup
                {
                    BuildingLabel = $"{building.Address}, {building.Settlement}, {building.City}",
                    Problems = new ObservableCollection<ProblemDisplayItem>(displayItems)
                });
            }

            BuildingProblems = groups;
        }

        public async Task MarkAsResolvedAsync(long problemId)
        {
            await _problemRepository.UpdateProblemStatusAsync(problemId, "Resolved");
            await LoadUnresolvedProblemsAsync();
        }

        private static string GetPriorityLabel(int priority)
        {
            return priority switch
            {
                1 => "Nizak",
                2 => "Srednji",
                3 => "Visok",
                _ => "Nepoznat"
            };
        }

        private static string GetStatusLabel(string status)
        {
            return status switch
            {
                "prijavljen" => "Prijavljen",
                "Potentially_resolved" => "Potentially  Resolved",
                "Resolved" => "Resolved",
                _ => status
            };
        }

        private static string GetTenantName(long tenantId)
        {
            try
            {
                using IDbConnection connection = PostgresConnection.CreateConnection();
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "SELECT name, surname FROM users WHERE id = @id";
                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = tenantId;
                command.Parameters.Add(param);

                using IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return $"{reader.GetString(0)} {reader.GetString(1)}";
                }
            }
            catch
            {
                // Fallback if lookup fails
            }
            return $"Stanar #{tenantId}";
        }
    }
}
