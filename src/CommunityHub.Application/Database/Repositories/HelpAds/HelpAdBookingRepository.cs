using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Services.HelpAds.DTO;
using Npgsql;

namespace CommunityHub.Application.Database.Repositories.HelpAds
{
    public class HelpAdBookingRepository
    {
        private readonly string _connectionString;

        public HelpAdBookingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Create a new booking when a tenant reserves slots
        /// </summary>
        public async Task<long> CreateBookingAsync(CreateHelpAdBookingDTO dto)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO help_ad_bookings 
                    (offered_help_ad_id, requesting_help_ad_id, booking_tenant_id, offering_tenant_id, status, created_at)
                    VALUES (@offeredId, @requestingId, @bookingTenantId, 
                            (SELECT tenant_id FROM help_ads WHERE id = @offeredId), 'PENDING', NOW())
                    RETURNING id;";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@offeredId", dto.OfferedHelpAdId);
                    command.Parameters.AddWithValue("@requestingId", dto.RequestingHelpAdId);
                    command.Parameters.AddWithValue("@bookingTenantId", dto.BookingTenantId);

                    var result = await command.ExecuteScalarAsync();
                    long bookingId = Convert.ToInt64(result);

                    // Reserve the slots
                    if (dto.SlotIds.Any())
                    {
                        await ReserveSlotsAsync(connection, bookingId, dto.SlotIds);
                    }

                    return bookingId;
                }
            }
        }

        /// <summary>
        /// Reserve slots for a booking
        /// </summary>
        private async Task ReserveSlotsAsync(NpgsqlConnection connection, long bookingId, List<long> slotIds)
        {
            const string sql = @"
                INSERT INTO help_ad_slot_bookings (booking_id, slot_id, reserved_at)
                VALUES (@bookingId, @slotId, NOW());";

            foreach (var slotId in slotIds)
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@bookingId", bookingId);
                    command.Parameters.AddWithValue("@slotId", slotId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Get all bookings for an offered help ad
        /// </summary>
        public async Task<List<HelpAdBookingDetailDTO>> GetBookingsForAdAsync(long helpAdId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    SELECT
                        b.id as booking_id,
                        b.offered_help_ad_id,
                        b.requesting_help_ad_id,
                        req_ad.description as requesting_ad_description,
                        b.booking_tenant_id,
                        u.name || ' ' || u.surname as booking_tenant_name,
                        COUNT(sb.slot_id) as slots_count,
                        b.created_at,
                        MIN(s.slot_date) as first_slot_date,
                        (array_agg(s.start_time ORDER BY s.start_time))[1]::text as first_slot_time
                    FROM help_ad_bookings b
                    JOIN help_ads req_ad ON b.requesting_help_ad_id = req_ad.id
                    JOIN users u ON b.booking_tenant_id = u.id
                    LEFT JOIN help_ad_slot_bookings sb ON b.id = sb.booking_id
                    LEFT JOIN help_ad_slots s ON sb.slot_id = s.id
                    WHERE b.offered_help_ad_id = @helpAdId
                    GROUP BY b.id, b.offered_help_ad_id, b.requesting_help_ad_id,
                             req_ad.description, b.booking_tenant_id, u.name, u.surname, b.created_at
                    ORDER BY b.created_at DESC;";

                var bookings = new List<HelpAdBookingDetailDTO>();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@helpAdId", helpAdId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Get the first slot date and time
                            var slotDate = reader.IsDBNull(8) ? DateTime.Now.Date : reader.GetDateTime(8).Date;
                            var slotTimeText = reader.IsDBNull(9) ? "00:00:00" : reader.GetString(9);

                            // Parse the time from the text (format: HH:mm:ss)
                            DateTime slotDateTime = slotDate;
                            if (TimeSpan.TryParse(slotTimeText, out var slotTime))
                            {
                                slotDateTime = slotDate.Add(slotTime);
                            }

                            bookings.Add(new HelpAdBookingDetailDTO
                            {
                                BookingId = reader.GetInt64(0),
                                OfferedHelpAdId = reader.GetInt64(1),
                                RequestingHelpAdId = reader.GetInt64(2),
                                RequestingAdDescription = reader.GetString(3),
                                BookingTenantId = reader.GetInt64(4),
                                BookingTenantName = reader.GetString(5),
                                SlotsCount = reader.GetInt32(6),
                                CreatedAt = reader.GetDateTime(7),
                                SlotDateTime = slotDateTime
                            });
                        }
                    }
                }

                return bookings;
            }
        }

        /// <summary>
        /// Get reserved slot IDs for a help ad (slots that are no longer available)
        /// </summary>
        public async Task<List<long>> GetReservedSlotsAsync(long helpAdId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    SELECT DISTINCT sb.slot_id
                    FROM help_ad_slot_bookings sb
                    JOIN help_ad_bookings b ON sb.booking_id = b.id
                    WHERE b.offered_help_ad_id = @helpAdId AND b.status = 'PENDING';";

                var reservedSlots = new List<long>();

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@helpAdId", helpAdId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reservedSlots.Add(reader.GetInt64(0));
                        }
                    }
                }

                return reservedSlots;
            }
        }

        /// <summary>
        /// Cancel a booking
        /// </summary>
        public async Task CancelBookingAsync(long bookingId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE help_ad_bookings 
                    SET status = 'CANCELLED' 
                    WHERE id = @bookingId;";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@bookingId", bookingId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
