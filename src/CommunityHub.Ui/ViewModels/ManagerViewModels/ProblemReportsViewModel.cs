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

namespace CommunityHub.Ui.ViewModels.ManagerViewModels
{
    public class ProblemDisplayItem
    {
        public long Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string PriorityLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
    }

    public class ProblemReportsViewModel : ViewModelBase
    {
        private readonly IProblemRepository _problemRepository;
        private readonly int _buildingId;

        private ObservableCollection<ProblemDisplayItem> _problems = new();
        public ObservableCollection<ProblemDisplayItem> Problems
        {
            get => _problems;
            set { _problems = value; OnPropertyChanged(); }
        }

        private List<ProblemDTO> _allProblems = new();
        public List<ProblemDTO> AllProblems
        {
            get => _allProblems;
            set { _allProblems = value; OnPropertyChanged(); }
        }

        private string _selectedStatusFilter = "Svi";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); ApplyFilters(); }
        }

        private string _selectedPriorityFilter = "Svi";
        public string SelectedPriorityFilter
        {
            get => _selectedPriorityFilter;
            set { _selectedPriorityFilter = value; OnPropertyChanged(); ApplyFilters(); }
        }

        public ProblemReportsViewModel(int buildingId)
        {
            _problemRepository = ManagerDependencyInjector.CreateInstance<IProblemRepository>();
            _buildingId = buildingId;
        }

        public async Task LoadProblemsAsync()
        {
            AllProblems = await _problemRepository.GetProblemsByBuildingAsync(_buildingId);
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            var filtered = AllProblems.AsEnumerable();

            if (_selectedStatusFilter != "Svi")
            {
                filtered = filtered.Where(p => p.Status == _selectedStatusFilter);
            }

            if (_selectedPriorityFilter != "Svi")
            {
                if (int.TryParse(_selectedPriorityFilter, out int priority))
                {
                    filtered = filtered.Where(p => p.Priority == priority);
                }
            }

            var displayItems = filtered.Select(p => new ProblemDisplayItem
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

            Problems = new ObservableCollection<ProblemDisplayItem>(displayItems);
        }

        public async Task MarkAsResolvedAsync(long problemId)
        {
            await _problemRepository.UpdateProblemStatusAsync(problemId, "Resolved");
            await LoadProblemsAsync();
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
