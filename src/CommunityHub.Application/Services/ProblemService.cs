using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Services
{
    public class ProblemService
    {
        private readonly IProblemRepository _problemRepository;

        public ProblemService(IProblemRepository problemRepository)
        {
            _problemRepository = problemRepository;
        }

        public async Task<long> ReportProblemAsync(CreateProblemDTO problem)
        {
            return await _problemRepository.CreateProblemAsync(problem);
        }

        public async Task<ProblemDTO> GetProblemByIdAsync(long problemId)
        {
            return await _problemRepository.GetProblemByIdAsync(problemId);
        }

        public async Task<List<ProblemDTO>> GetMyProblemsAsync(long tenantId, long buildingId)
        {
            return await _problemRepository.GetProblemsByTenantAsync(tenantId, buildingId);
        }

        public async Task<List<ProblemDTO>> GetBuildingProblemsAsync(long buildingId)
        {
            return await _problemRepository.GetProblemsByBuildingAsync(buildingId);
        }

        public async Task UpdateProblemStatusAsync(long problemId, string newStatus)
        {
            await _problemRepository.UpdateProblemStatusAsync(problemId, newStatus);
        }

        public async Task<List<ProblemDTO>> GetUnresolvedProblemsAsync(long buildingId)
        {
            return await _problemRepository.GetUnresolvedProblemsForBuildingAsync(buildingId);
        }

        public string GetPriorityText(int priority)
        {
            return priority switch
            {
                1 => "Can wait",
                2 => "Should be resolved soon",
                3 => "Urgent",
                _ => "Unknown"
            };
        }

        public string GetStatusText(string status)
        {
            return status switch
            {
                "Unresolved" => "Unresolved",
                "Potentially_resolved" => "Potentially resolved",
                "Resolved" => "Resolved",
                _ => status
            };
        }
    }
}
