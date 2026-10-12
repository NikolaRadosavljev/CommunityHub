using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunityHub.Application.Services
{
    public interface IAssemblyService
    {
        Task<long> ScheduleAssemblyAsync(CreateMeetingDTO dto);
        Task<List<HomeownersMeetingDTO>> GetAssembliesForBuildingAsync(long buildingId, long managerId);
        Task<HomeownersMeetingDTO?> GetAssemblyDetailsAsync(long meetingId, long managerId);
    }
}
