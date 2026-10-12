using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;

namespace CommunityHub.Application.Database.Repositories
{
    public class HomeownersMeetingDbRepository : IHomeownersMeetingRepository
    {
        private readonly string _connectionString;

        public HomeownersMeetingDbRepository(string connectionString = null)
        {
            _connectionString = connectionString ?? PostgresConnection.GetConnectionString();
        }

        public async Task<long> CreateMeetingAsync(CreateMeetingDTO meeting)
        {
            const string sql = @"
                INSERT INTO homeowners_meetings (building_id, scheduled_date, scheduled_time, status, created_date, updated_date)
                VALUES (@building_id, @scheduled_date, @scheduled_time, 'scheduled', NOW(), NOW())
                RETURNING id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", meeting.BuildingId);
            command.Parameters.AddWithValue("@scheduled_date", meeting.ScheduledDate.Date);
            command.Parameters.AddWithValue("@scheduled_time", meeting.ScheduledTime);

            var result = await command.ExecuteScalarAsync();
            return result != null ? (long)result : 0;
        }

        public async Task<HomeownersMeeting> GetMeetingByIdAsync(long meetingId)
        {
            const string sql = @"
                SELECT id, building_id, scheduled_date, scheduled_time, status, created_date, updated_date
                FROM homeowners_meetings
                WHERE id = @id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", meetingId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new HomeownersMeeting
                {
                    Id = reader.GetInt64(0),
                    BuildingId = (int)reader.GetInt64(1),
                    ScheduledDate = reader.GetDateTime(2),
                    ScheduledTime = reader.GetTimeSpan(3),
                    Status = reader.GetString(4),
                    CreatedDate = reader.GetDateTime(5),
                    UpdatedDate = reader.GetDateTime(6)
                };
            }
            return null;
        }

        public async Task<List<HomeownersMeeting>> GetMeetingsByBuildingAsync(long buildingId)
        {
            const string sql = @"
                SELECT id, building_id, scheduled_date, scheduled_time, status, created_date, updated_date
                FROM homeowners_meetings
                WHERE building_id = @building_id
                ORDER BY scheduled_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var meetings = new List<HomeownersMeeting>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                meetings.Add(new HomeownersMeeting
                {
                    Id = reader.GetInt64(0),
                    BuildingId = (int)reader.GetInt64(1),
                    ScheduledDate = reader.GetDateTime(2),
                    ScheduledTime = reader.GetTimeSpan(3),
                    Status = reader.GetString(4),
                    CreatedDate = reader.GetDateTime(5),
                    UpdatedDate = reader.GetDateTime(6)
                });
            }
            return meetings;
        }

        public async Task UpdateMeetingStatusAsync(long meetingId, string newStatus)
        {
            const string sql = @"
                UPDATE homeowners_meetings
                SET status = @status, updated_date = NOW()
                WHERE id = @id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@status", newStatus);
            command.Parameters.AddWithValue("@id", meetingId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<int> GetAttendanceCountAsync(long meetingId)
        {
            const string sql = @"
                SELECT COUNT(*) FROM meeting_attendance
                WHERE meeting_id = @meeting_id AND attended = true;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task<int> GetBuildingStanCount(long buildingId)
        {
            const string sql = @"
                SELECT COUNT(DISTINCT flat_number)
                FROM building_memberships
                WHERE building_id = @building_id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public async Task ReportAttendanceAsync(long meetingId, long tenantId, bool attended)
        {
            const string sql = @"
                INSERT INTO meeting_attendance (meeting_id, tenant_id, attended, reported_date)
                VALUES (@meeting_id, @tenant_id, @attended, NOW())
                ON CONFLICT (meeting_id, tenant_id)
                DO UPDATE SET attended = @attended, reported_date = NOW();";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);
            command.Parameters.AddWithValue("@tenant_id", tenantId);
            command.Parameters.AddWithValue("@attended", attended);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> HasUserReportedAttendanceAsync(long meetingId, long tenantId)
        {
            const string sql = @"
                SELECT EXISTS(SELECT 1 FROM meeting_attendance
                WHERE meeting_id = @meeting_id AND tenant_id = @tenant_id);";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);
            command.Parameters.AddWithValue("@tenant_id", tenantId);

            var result = await command.ExecuteScalarAsync();
            return result != null && (bool)result;
        }

        public async Task<List<MeetingAttendance>> GetAllAttendanceForMeetingAsync(long meetingId)
        {
            const string sql = @"
                SELECT id, meeting_id, tenant_id, attended, reported_date
                FROM meeting_attendance
                WHERE meeting_id = @meeting_id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);

            var result = new List<MeetingAttendance>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MeetingAttendance
                {
                    Id = reader.GetInt64(0),
                    MeetingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Attended = reader.GetBoolean(3),
                    ReportedDate = reader.GetDateTime(4)
                });
            }
            return result;
        }

        public async Task ProposeMeetingTopicAsync(long meetingId, long tenantId, string topicText)
        {
            const string sql = @"
                INSERT INTO meeting_topics (meeting_id, topic_text, proposed_by_tenant_id, proposed_date, is_accepted)
                VALUES (@meeting_id, @topic_text, @tenant_id, NOW(), FALSE);";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);
            command.Parameters.AddWithValue("@topic_text", topicText);
            command.Parameters.AddWithValue("@tenant_id", tenantId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task AddOfficialTopicAsync(long meetingId, string topicText, long managerId)
        {
            const string sql = @"
                INSERT INTO meeting_topics (meeting_id, topic_text, proposed_by_tenant_id, proposed_date, is_accepted)
                VALUES (@meeting_id, @topic_text, @manager_id, NOW(), TRUE);";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);
            command.Parameters.AddWithValue("@topic_text", topicText);
            command.Parameters.AddWithValue("@manager_id", managerId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task AcceptProposedTopicAsync(long topicId)
        {
            const string sql = @"
                UPDATE meeting_topics SET is_accepted = TRUE WHERE id = @id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", topicId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ProposedTopic>> GetMeetingTopicsAsync(long meetingId)
        {
            const string sql = @"
                SELECT id, meeting_id, topic_text, proposed_by_tenant_id, proposed_date
                FROM meeting_topics
                WHERE meeting_id = @meeting_id
                ORDER BY proposed_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);

            var topics = new List<ProposedTopic>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                topics.Add(new ProposedTopic
                {
                    Id = reader.GetInt64(0),
                    MeetingId = reader.GetInt64(1),
                    TopicText = reader.GetString(2),
                    ProposedByTenantId = reader.GetInt64(3),
                    ProposedDate = reader.GetDateTime(4)
                });
            }
            return topics;
        }

        public async Task<List<ProposedTopic>> GetMeetingTopicsWithStatusAsync(long meetingId)
        {
            const string sql = @"
                SELECT id, meeting_id, topic_text, proposed_by_tenant_id, proposed_date,
                       COALESCE(is_accepted, FALSE)
                FROM meeting_topics
                WHERE meeting_id = @meeting_id
                ORDER BY proposed_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@meeting_id", meetingId);

            var topics = new List<ProposedTopic>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                topics.Add(new ProposedTopic
                {
                    Id = reader.GetInt64(0),
                    MeetingId = reader.GetInt64(1),
                    TopicText = reader.GetString(2),
                    ProposedByTenantId = reader.GetInt64(3),
                    ProposedDate = reader.GetDateTime(4),
                    IsAccepted = reader.GetBoolean(5)
                });
            }
            return topics;
        }

        public async Task<List<HomeownersMeeting>> GetUpcomingMeetingsAsync(long buildingId)
        {
            const string sql = @"
                SELECT id, building_id, scheduled_date, scheduled_time, status, created_date, updated_date
                FROM homeowners_meetings
                WHERE building_id = @building_id
                  AND scheduled_date >= CURRENT_DATE
                  AND status IN ('scheduled', 'confirmed')
                ORDER BY scheduled_date ASC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var meetings = new List<HomeownersMeeting>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                meetings.Add(new HomeownersMeeting
                {
                    Id = reader.GetInt64(0),
                    BuildingId = (int)reader.GetInt64(1),
                    ScheduledDate = reader.GetDateTime(2),
                    ScheduledTime = reader.GetTimeSpan(3),
                    Status = reader.GetString(4),
                    CreatedDate = reader.GetDateTime(5),
                    UpdatedDate = reader.GetDateTime(6)
                });
            }
            return meetings;
        }

        public async Task<List<HomeownersMeeting>> GetMeetingsNearingDeadlineAsync()
        {
            const string sql = @"
                SELECT id, building_id, scheduled_date, scheduled_time, status, created_date, updated_date
                FROM homeowners_meetings
                WHERE status = 'scheduled'
                  AND scheduled_date - INTERVAL '24 hours' <= NOW()
                  AND scheduled_date > NOW();";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);

            var meetings = new List<HomeownersMeeting>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                meetings.Add(new HomeownersMeeting
                {
                    Id = reader.GetInt64(0),
                    BuildingId = (int)reader.GetInt64(1),
                    ScheduledDate = reader.GetDateTime(2),
                    ScheduledTime = reader.GetTimeSpan(3),
                    Status = reader.GetString(4),
                    CreatedDate = reader.GetDateTime(5),
                    UpdatedDate = reader.GetDateTime(6)
                });
            }
            return meetings;
        }
    }
}
