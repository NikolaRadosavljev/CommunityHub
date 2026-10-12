using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Services.HelpAds.DTO;
using CommunityHub.Application.Domain.RepositoryInterfaces.HelpAds;
using Npgsql;
using System;

namespace CommunityHub.Application.Database.Repositories.HelpAds
{
    public class HelpAdDbRepository : IHelpAdRepository
    {
        private readonly string _connectionString;

        public HelpAdDbRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<HelpAdDTO> CreateHelpAd(CreateHelpAdDTO createHelpAdDTO)
        {
            var query = "INSERT INTO help_ads (building_id, tenant_id, description, start_date, end_date, category, type, status) VALUES (@building_id, @tenant_id, @description, @start_date, @end_date, @category, @type, 'ACTIVE') RETURNING id;";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("building_id", createHelpAdDTO.BuildingId);
            command.Parameters.AddWithValue("tenant_id", createHelpAdDTO.TenantId);
            command.Parameters.AddWithValue("description", createHelpAdDTO.Description);
            command.Parameters.AddWithValue("start_date", createHelpAdDTO.StartDate);
            command.Parameters.AddWithValue("end_date", createHelpAdDTO.EndDate);
            command.Parameters.AddWithValue("category", createHelpAdDTO.Category);
            command.Parameters.AddWithValue("type", createHelpAdDTO.Type ?? "offering");

            var id = (long)await command.ExecuteScalarAsync();

            return new HelpAdDTO
            {
                Id = id,
                BuildingId = createHelpAdDTO.BuildingId,
                TenantId = createHelpAdDTO.TenantId,
                Description = createHelpAdDTO.Description,
                StartDate = createHelpAdDTO.StartDate,
                EndDate = createHelpAdDTO.EndDate,
                Category = createHelpAdDTO.Category,
                Type = createHelpAdDTO.Type ?? "offering",
                Status = "ACTIVE"
            };
        }

        public async Task<List<HelpAdDTO>> GetHelpAdsForBuilding(long buildingId)
        {
            var query = "SELECT id, building_id, tenant_id, description, start_date, end_date, category, type, status FROM help_ads WHERE building_id = @building_id;";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("building_id", buildingId);

            using var reader = await command.ExecuteReaderAsync();
            var helpAds = new List<HelpAdDTO>();
            while (await reader.ReadAsync())
            {
                helpAds.Add(new HelpAdDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    StartDate = reader.GetDateTime(4),
                    EndDate = reader.GetDateTime(5),
                    Category = reader.GetString(6),
                    Type = reader.IsDBNull(7) ? "offering" : reader.GetString(7),
                    Status = reader.GetString(8)
                });
            }
            return helpAds;
        }

        public async Task CreateHelpAdSlots(List<HelpAdSlotDTO> slots)
        {
            var query = "INSERT INTO help_ad_slots (help_ad_id, slot_date, start_time, end_time) VALUES (@help_ad_id, @slot_date, @start_time, @end_time);";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            foreach (var slot in slots)
            {
                try
                {
                    using var command = new NpgsqlCommand(query, connection);
                    command.Parameters.AddWithValue("help_ad_id", slot.HelpAdId);
                    command.Parameters.AddWithValue("slot_date", slot.SlotDate);
                    // Convert TimeSpan to TIME format for PostgreSQL
                    command.Parameters.AddWithValue("start_time", slot.StartTime);
                    command.Parameters.AddWithValue("end_time", slot.EndTime);
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Error creating slot for help ad {slot.HelpAdId} on {slot.SlotDate.Date:yyyy-MM-dd} " +
                        $"from {slot.StartTime:hh\\:mm\\:ss} to {slot.EndTime:hh\\:mm\\:ss}: {ex.Message}",
                        ex);
                }
            }
        }

        public async Task<HelpAdDTO> GetHelpAdById(long helpAdId)
        {
            var query = "SELECT id, building_id, tenant_id, description, start_date, end_date, category, type, status FROM help_ads WHERE id = @id;";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("id", helpAdId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new HelpAdDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    StartDate = reader.GetDateTime(4),
                    EndDate = reader.GetDateTime(5),
                    Category = reader.GetString(6),
                    Type = reader.IsDBNull(7) ? "offering" : reader.GetString(7),
                    Status = reader.GetString(8)
                };
            }
            return null;
        }

        public async Task<List<HelpAdSlotDTO>> GetSlotsForAd(long helpAdId)
        {
            var query = "SELECT id, help_ad_id, slot_date, start_time, end_time FROM help_ad_slots WHERE help_ad_id = @help_ad_id ORDER BY slot_date, start_time;";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("help_ad_id", helpAdId);

            using var reader = await command.ExecuteReaderAsync();
            var slots = new List<HelpAdSlotDTO>();
            int rowCount = 0;
            while (await reader.ReadAsync())
            {
                rowCount++;
                try
                {
                    var id = reader.GetInt64(0);
                    var helpAdIdValue = reader.GetInt64(1);
                    var slotDate = reader.GetDateTime(2);

                    TimeSpan startTime;
                    TimeSpan endTime;

                    // Handle start_time column (column index 3)
                    try
                    {
                        if (reader.IsDBNull(3))
                        {
                            startTime = TimeSpan.Zero;
                        }
                        else
                        {
                            try
                            {
                                startTime = reader.GetTimeSpan(3);
                            }
                            catch
                            {
                                var timeStr = reader.GetString(3);
                                if (TimeSpan.TryParse(timeStr, out var parsedTime))
                                {
                                    startTime = parsedTime;
                                }
                                else
                                {
                                    throw new FormatException($"Cannot parse start_time value '{timeStr}' as TimeSpan");
                                }
                            }
                        }
                    }
                    catch (Exception exStart)
                    {
                        throw new InvalidOperationException($"Failed to parse start_time for slot {id}: {exStart.Message}", exStart);
                    }

                    // Handle end_time column (column index 4)
                    try
                    {
                        if (reader.IsDBNull(4))
                        {
                            endTime = TimeSpan.Zero;
                        }
                        else
                        {
                            try
                            {
                                endTime = reader.GetTimeSpan(4);
                            }
                            catch
                            {
                                var timeStr = reader.GetString(4);
                                if (TimeSpan.TryParse(timeStr, out var parsedTime))
                                {
                                    endTime = parsedTime;
                                }
                                else
                                {
                                    throw new FormatException($"Cannot parse end_time value '{timeStr}' as TimeSpan");
                                }
                            }
                        }
                    }
                    catch (Exception exEnd)
                    {
                        throw new InvalidOperationException($"Failed to parse end_time for slot {id}: {exEnd.Message}", exEnd);
                    }

                    slots.Add(new HelpAdSlotDTO
                    {
                        Id = id,
                        HelpAdId = helpAdIdValue,
                        SlotDate = slotDate,
                        StartTime = startTime,
                        EndTime = endTime
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error reading slot #{rowCount} for help ad {helpAdId}: {ex.Message}", ex);
                }
            }
            return slots;
        }

        public async Task ArchiveHelpAd(long helpAdId)
        {
            var query = "UPDATE help_ads SET status = 'ARCHIVED' WHERE id = @id;";
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("id", helpAdId);
            await command.ExecuteNonQueryAsync();
        }

        public string GetConnectionString() => _connectionString;
    }
}