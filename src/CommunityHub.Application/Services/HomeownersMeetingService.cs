using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;

namespace CommunityHub.Application.Services
{
    public class HomeownersMeetingService
    {
        private readonly IHomeownersMeetingRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IBuildingRepository _buildingRepository;

        public HomeownersMeetingService(
            IHomeownersMeetingRepository repository,
            IUserRepository userRepository = null,
            IBuildingRepository buildingRepository = null)
        {
            _repository = repository;
            _userRepository = userRepository;
            _buildingRepository = buildingRepository;
        }

        public async Task<long> CreateMeetingAsync(CreateMeetingDTO meetingDto)
        {
            // Validate that meeting is at least 24 hours in the future
            var meetingDateTime = meetingDto.ScheduledDate.Add(meetingDto.ScheduledTime);
            if (meetingDateTime < DateTime.Now.AddHours(24))
            {
                throw new InvalidOperationException("Meeting must be scheduled at least 24 hours in advance.");
            }

            return await _repository.CreateMeetingAsync(meetingDto);
        }

        public async Task<HomeownersMeetingDTO> GetMeetingAsync(long meetingId, long tenantId)
        {
            var meeting = await _repository.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                return null;

            var attendanceCount = await _repository.GetAttendanceCountAsync(meetingId);
            var stanCount = await _repository.GetBuildingStanCount(meeting.BuildingId);
            var topics = await _repository.GetMeetingTopicsAsync(meetingId);
            var userReported = await _repository.HasUserReportedAttendanceAsync(meetingId, tenantId);

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
                UserAttending = userReported,
                Topics = topics.Select(t => new MeetingTopicDTO
                {
                    Id = t.Id,
                    TopicText = t.TopicText,
                    ProposedByTenantId = t.ProposedByTenantId,
                    ProposedByName = GetTenantName(t.ProposedByTenantId),
                    ProposedDate = t.ProposedDate
                }).ToList(),
                CreatedDate = meeting.CreatedDate,
                UpdatedDate = meeting.UpdatedDate
            };
        }

        public async Task<List<HomeownersMeetingDTO>> GetMeetingsByBuildingAsync(long buildingId, long tenantId)
        {
            var meetings = await _repository.GetMeetingsByBuildingAsync(buildingId);
            var dtos = new List<HomeownersMeetingDTO>();

            foreach (var meeting in meetings)
            {
                var attendanceCount = await _repository.GetAttendanceCountAsync(meeting.Id);
                var stanCount = await _repository.GetBuildingStanCount(buildingId);
                var topics = await _repository.GetMeetingTopicsAsync(meeting.Id);
                var userReported = await _repository.HasUserReportedAttendanceAsync(meeting.Id, tenantId);

                var attendancePercentage = stanCount > 0 ? (decimal)attendanceCount / stanCount * 100 : 0;

                dtos.Add(new HomeownersMeetingDTO
                {
                    Id = meeting.Id,
                    BuildingId = meeting.BuildingId,
                    ScheduledDate = meeting.ScheduledDate,
                    ScheduledTime = meeting.ScheduledTime,
                    Status = meeting.Status,
                    TotalStans = stanCount,
                    AttendanceCount = attendanceCount,
                    AttendancePercentage = attendancePercentage,
                    UserAttending = userReported,
                    Topics = topics.Select(t => new MeetingTopicDTO
                    {
                        Id = t.Id,
                        TopicText = t.TopicText,
                        ProposedByTenantId = t.ProposedByTenantId,
                        ProposedByName = GetTenantName(t.ProposedByTenantId),
                        ProposedDate = t.ProposedDate
                    }).ToList(),
                    CreatedDate = meeting.CreatedDate,
                    UpdatedDate = meeting.UpdatedDate
                });
            }

            return dtos.OrderByDescending(m => m.ScheduledDate).ToList();
        }

        public async Task ReportAttendanceAsync(long meetingId, long tenantId, bool attending)
        {
            var meeting = await _repository.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                throw new InvalidOperationException("Meeting not found.");

            // Check if meeting is more than 24h away
            var meetingDateTime = meeting.ScheduledDate.Add(meeting.ScheduledTime);
            var hoursUntilMeeting = (meetingDateTime - DateTime.Now).TotalHours;
            if (hoursUntilMeeting <= 24)
            {
                throw new InvalidOperationException("Attendance can only be reported more than 24 hours before the meeting.");
            }

            if (meeting.Status != "scheduled" && meeting.Status != "confirmed")
            {
                throw new InvalidOperationException("Cannot report attendance for a cancelled meeting.");
            }

            await _repository.ReportAttendanceAsync(meetingId, tenantId, attending);

            // Update meeting status based on attendance
            await UpdateMeetingStatusAsync(meetingId);
        }

        public async Task ProposeMeetingTopicAsync(long meetingId, long tenantId, string topicText)
        {
            if (string.IsNullOrWhiteSpace(topicText))
                throw new InvalidOperationException("Topic cannot be empty.");

            var meeting = await _repository.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                throw new InvalidOperationException("Meeting not found.");

            // Check if meeting is more than 24h away
            var meetingDateTime = meeting.ScheduledDate.Add(meeting.ScheduledTime);
            var hoursUntilMeeting = (meetingDateTime - DateTime.Now).TotalHours;
            if (hoursUntilMeeting <= 24)
            {
                throw new InvalidOperationException("Topics can only be proposed more than 24 hours before the meeting.");
            }

            if (meeting.Status == "cancelled")
            {
                throw new InvalidOperationException("Cannot propose topics for a cancelled meeting.");
            }

            await _repository.ProposeMeetingTopicAsync(meetingId, tenantId, topicText);
        }

        public async Task UpdateMeetingStatusAsync(long meetingId)
        {
            var meeting = await _repository.GetMeetingByIdAsync(meetingId);
            if (meeting == null)
                return;

            var attendanceCount = await _repository.GetAttendanceCountAsync(meetingId);
            var stanCount = await _repository.GetBuildingStanCount(meeting.BuildingId);

            if (stanCount == 0)
                return;

            var attendancePercentage = (decimal)attendanceCount / stanCount;

            if (attendancePercentage >= 0.5m)
            {
                // More than 50% attendance - confirm meeting
                if (meeting.Status == "scheduled")
                {
                    await _repository.UpdateMeetingStatusAsync(meetingId, "confirmed");
                }
            }
            else
            {
                // Less than 50% attendance - check if 24h before deadline
                var meetingDateTime = meeting.ScheduledDate.Add(meeting.ScheduledTime);
                var hoursUntilMeeting = (meetingDateTime - DateTime.Now).TotalHours;

                if (hoursUntilMeeting <= 24 && hoursUntilMeeting > 0)
                {
                    // Within 24h before meeting and attendance dropped below 50%
                    if (meeting.Status == "confirmed")
                    {
                        await _repository.UpdateMeetingStatusAsync(meetingId, "scheduled");
                    }
                }
            }
        }

        public async Task CheckAndCancelMeetingsAsync()
        {
            var meetingsNearingDeadline = await _repository.GetMeetingsNearingDeadlineAsync();

            foreach (var meeting in meetingsNearingDeadline)
            {
                var attendanceCount = await _repository.GetAttendanceCountAsync(meeting.Id);
                var stanCount = await _repository.GetBuildingStanCount(meeting.BuildingId);

                if (stanCount > 0)
                {
                    var attendancePercentage = (decimal)attendanceCount / stanCount;

                    if (attendancePercentage < 0.5m)
                    {
                        await _repository.UpdateMeetingStatusAsync(meeting.Id, "cancelled");
                    }
                }
            }
        }

        private string GetTenantName(long tenantId)
        {
            try
            {
                var user = _userRepository?.GetById(tenantId);
                if (user != null)
                {
                    var fullName = $"{user.Name} {user.Surname}".Trim();
                    if (!string.IsNullOrWhiteSpace(fullName))
                        return fullName;
                }
            }
            catch
            {
                // Fall through to default
            }
            return $"Tenant #{tenantId}";
        }

        public string GetStatusText(string status)
        {
            return status switch
            {
                "scheduled" => "Scheduled",
                "confirmed" => "Confirmed",
                "cancelled" => "Cancelled",
                _ => status
            };
        }
    }
}
