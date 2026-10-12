using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.HelpAds.DTO;
using Npgsql;

namespace CommunityHub.Application.Database.Repositories.HelpAds
{
    public class HelpAdNotificationRepository
    {
        private readonly string _connectionString;

        public HelpAdNotificationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Create a notification when a tenant books slots
        /// </summary>
        public async Task<long> CreateNotificationAsync(long tenantId, long helpAdId, long bookingId,
            long bookingTenantId, long requestingHelpAdId, int slotsCount)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO help_ad_notifications
                    (tenant_id, help_ad_id, booking_id, booking_tenant_id, requesting_help_ad_id, slots_count, type, is_read, created_at)
                    VALUES (@tenantId, @helpAdId, @bookingId, @bookingTenantId, @requestingHelpAdId, @slotsCount, 'SLOT_BOOKED', false, NOW())
                    RETURNING id;";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@tenantId", tenantId);
                    command.Parameters.AddWithValue("@helpAdId", helpAdId);
                    command.Parameters.AddWithValue("@bookingId", bookingId);
                    command.Parameters.AddWithValue("@bookingTenantId", bookingTenantId);
                    command.Parameters.AddWithValue("@requestingHelpAdId", requestingHelpAdId);
                    command.Parameters.AddWithValue("@slotsCount", slotsCount);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt64(result);
                }
            }
        }

        /// <summary>
        /// Create a notification when a matching ad is found
        /// </summary>
        public async Task<long> CreateMatchingAdNotificationAsync(long tenantId, long matchingHelpAdId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO help_ad_notifications
                    (tenant_id, help_ad_id, type, is_read, created_at)
                    VALUES (@tenantId, @helpAdId, 'AD_MATCHED', false, NOW())
                    RETURNING id;";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@tenantId", tenantId);
                    command.Parameters.AddWithValue("@helpAdId", matchingHelpAdId);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt64(result);
                }
            }
        }

        /// <summary>
        /// Get unread notifications for a tenant
        /// </summary>
        public async Task<List<HelpAdNotificationDTO>> GetUnreadNotificationsAsync(long tenantId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    SELECT id, tenant_id, help_ad_id, booking_id, booking_tenant_id, 
                           requesting_help_ad_id, slots_count, type, is_read, created_at
                    FROM help_ad_notifications
                    WHERE tenant_id = @tenantId AND is_read = false
                    ORDER BY created_at DESC;";

                var notifications = new List<HelpAdNotificationDTO>();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@tenantId", tenantId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new HelpAdNotificationDTO
                            {
                                Id = reader.GetInt64(0),
                                TenantId = reader.GetInt64(1),
                                HelpAdId = reader.GetInt64(2),
                                BookingId = reader.GetInt64(3),
                                BookingTenantId = reader.GetInt64(4),
                                RequestingHelpAdId = reader.GetInt64(5),
                                SlotsCount = reader.GetInt32(6),
                                Type = reader.GetString(7),
                                IsRead = reader.GetBoolean(8),
                                CreatedAt = reader.GetDateTime(9)
                            });
                        }
                    }
                }

                return notifications;
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public async Task MarkAsReadAsync(long notificationId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE help_ad_notifications 
                    SET is_read = true 
                    WHERE id = @notificationId;";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@notificationId", notificationId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Get all notifications for a tenant (read and unread)
        /// </summary>
        public async Task<List<HelpAdNotificationDTO>> GetAllNotificationsAsync(long tenantId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    SELECT id, tenant_id, help_ad_id, booking_id, booking_tenant_id, 
                           requesting_help_ad_id, slots_count, type, is_read, created_at
                    FROM help_ad_notifications
                    WHERE tenant_id = @tenantId
                    ORDER BY created_at DESC;";

                var notifications = new List<HelpAdNotificationDTO>();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@tenantId", tenantId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new HelpAdNotificationDTO
                            {
                                Id = reader.GetInt64(0),
                                TenantId = reader.GetInt64(1),
                                HelpAdId = reader.GetInt64(2),
                                BookingId = reader.GetInt64(3),
                                BookingTenantId = reader.GetInt64(4),
                                RequestingHelpAdId = reader.GetInt64(5),
                                SlotsCount = reader.GetInt32(6),
                                Type = reader.GetString(7),
                                IsRead = reader.GetBoolean(8),
                                CreatedAt = reader.GetDateTime(9)
                            });
                        }
                    }
                }

                return notifications;
            }
        }
    }
}
