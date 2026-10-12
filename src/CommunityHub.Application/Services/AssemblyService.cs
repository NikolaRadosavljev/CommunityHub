using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Services
{
    public class AssemblyService : IAssemblyService
    {
        private readonly IHomeownersMeetingRepository _meetingRepository;

        public AssemblyService(IHomeownersMeetingRepository meetingRepository)
        {
            _meetingRepository = meetingRepository;
        }

        public async Task<long> ScheduleAssemblyAsync(CreateMeetingDTO dto)
        {
            var scheduledDateTime = dto.ScheduledDate.Add(dto.ScheduledTime);
            if (scheduledDateTime <= DateTime.Now)
                throw new InvalidOperationException("Scheduled date and time must be in the future.");

            var existing = await _meetingRepository.GetMeetingsByBuildingAsync(dto.BuildingId);
            var conflict = existing.FirstOrDefault(m => m.ScheduledDate == dto.ScheduledDate && m.ScheduledTime == dto.ScheduledTime
                                                       && (m.Status == "zakazana" || m.Status == "potvrđena"));
            if (conflict != null)
                throw new InvalidOperationException("There is already a meeting scheduled or confirmed for this building at the same date and time.");

            return await _meetingRepository.CreateMeetingAsync(dto);
        }

        public async Task<List<HomeownersMeetingDTO>> GetAssembliesForBuildingAsync(long buildingId, long managerId)
        {
            var meetings = await _meetingRepository.GetMeetingsByBuildingAsync(buildingId);
            var result = new List<HomeownersMeetingDTO>();
            foreach (var m in meetings)
            {
                var attendanceCount = await _meetingRepository.GetAttendanceCountAsync(m.Id);
                var stanCount = await _meetingRepository.GetBuildingStanCount(m.BuildingId);
                var topics = await _meetingRepository.GetMeetingTopicsAsync(m.Id);

                var attendancePercentage = stanCount > 0 ? (decimal)attendanceCount / stanCount * 100 : 0;

                result.Add(new HomeownersMeetingDTO
                {
                    Id = m.Id,
                    BuildingId = m.BuildingId,
                    ScheduledDate = m.ScheduledDate,
                    ScheduledTime = m.ScheduledTime,
                    Status = m.Status,
                    TotalStans = stanCount,
                    AttendanceCount = attendanceCount,
                    AttendancePercentage = attendancePercentage,
                    UserAttending = false,
                    Topics = topics.Select(t => new MeetingTopicDTO
                    {
                        Id = t.Id,
                        TopicText = t.TopicText,
                        ProposedByTenantId = t.ProposedByTenantId,
                        ProposedByName = $"Tenant #{t.ProposedByTenantId}",
                        ProposedDate = t.ProposedDate
                    }).ToList(),
                    CreatedDate = m.CreatedDate,
                    UpdatedDate = m.UpdatedDate
                });
            }

            return result.OrderByDescending(x => x.ScheduledDate).ToList();
        }

        public async Task<HomeownersMeetingDTO?> GetAssemblyDetailsAsync(long meetingId, long managerId)
        {
            var meeting = await _meetingRepository.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                return null;

            var attendanceCount = await _meetingRepository.GetAttendanceCountAsync(meetingId);
            var stanCount = await _meetingRepository.GetBuildingStanCount(meeting.BuildingId);
            var topics = await _meetingRepository.GetMeetingTopicsAsync(meetingId);

            var attendancePercentage = stanCount > 0 ? (decimal)attendanceCount / stanCount * 100 : 0;

            return new HomeownersMeetingDTO
            {
                Id = meeting.Id,
                BuildingId = meeting.BuildingId,
                ScheduledDate = meeting.ScheduledDate,
                ScheduledTime = meeting.ScheduledTime,
                Status = meeting.Status,
                TotalStans = stanCount,
                AttendanceCount = attendanceCount,
                AttendancePercentage = attendancePercentage,
                UserAttending = false,
                Topics = topics.Select(t => new MeetingTopicDTO
                {
                    Id = t.Id,
                    TopicText = t.TopicText,
                    ProposedByTenantId = t.ProposedByTenantId,
                    ProposedByName = $"Tenant #{t.ProposedByTenantId}",
                    ProposedDate = t.ProposedDate
                }).ToList(),
                CreatedDate = meeting.CreatedDate,
                UpdatedDate = meeting.UpdatedDate
            };
        }
    }
}
