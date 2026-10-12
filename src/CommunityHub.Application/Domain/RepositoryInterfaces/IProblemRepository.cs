using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Services;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface IProblemRepository
    {
        Task<long> CreateProblemAsync(CreateProblemDTO problem);
        Task<ProblemDTO> GetProblemByIdAsync(long problemId);
        Task<List<ProblemDTO>> GetProblemsByTenantAsync(long tenantId, long buildingId);
        Task<List<ProblemDTO>> GetProblemsByBuildingAsync(long buildingId);
        Task UpdateProblemStatusAsync(long problemId, string newStatus);
        Task<List<ProblemDTO>> GetUnresolvedProblemsForBuildingAsync(long buildingId);
    }
}
