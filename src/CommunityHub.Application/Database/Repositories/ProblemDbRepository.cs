using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Services;
using Npgsql;

namespace CommunityHub.Application.Database.Repositories
{
    public class ProblemDbRepository : IProblemRepository
    {
        private readonly string _connectionString;

        public ProblemDbRepository(string connectionString = null)
        {
            _connectionString = connectionString ?? PostgresConnection.GetConnectionString();
        }

        public async Task<long> CreateProblemAsync(CreateProblemDTO problem)
        {
            const string sql = @"
                INSERT INTO problems (building_id, tenant_id, description, priority, status, reported_date)
                VALUES (@building_id, @tenant_id, @description, @priority, 'Unresolved', NOW())
                RETURNING id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", problem.BuildingId);
            command.Parameters.AddWithValue("@tenant_id", problem.TenantId);
            command.Parameters.AddWithValue("@description", problem.Description);
            command.Parameters.AddWithValue("@priority", problem.Priority);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        public async Task<ProblemDTO> GetProblemByIdAsync(long problemId)
        {
            const string sql = @"
                SELECT id, building_id, tenant_id, description, priority, status, reported_date, resolved_date
                FROM problems
                WHERE id = @id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", problemId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ProblemDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    Priority = reader.GetInt32(4),
                    Status = reader.GetString(5),
                    ReportedDate = reader.GetDateTime(6),
                    ResolvedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                };
            }
            return null;
        }

        public async Task<List<ProblemDTO>> GetProblemsByTenantAsync(long tenantId, long buildingId)
        {
            const string sql = @"
                SELECT id, building_id, tenant_id, description, priority, status, reported_date, resolved_date
                FROM problems
                WHERE tenant_id = @tenant_id AND building_id = @building_id
                ORDER BY reported_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tenant_id", tenantId);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var problems = new List<ProblemDTO>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                problems.Add(new ProblemDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    Priority = reader.GetInt32(4),
                    Status = reader.GetString(5),
                    ReportedDate = reader.GetDateTime(6),
                    ResolvedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }
            return problems;
        }

        public async Task<List<ProblemDTO>> GetProblemsByBuildingAsync(long buildingId)
        {
            const string sql = @"
                SELECT id, building_id, tenant_id, description, priority, status, reported_date, resolved_date
                FROM problems
                WHERE building_id = @building_id
                ORDER BY reported_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var problems = new List<ProblemDTO>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                problems.Add(new ProblemDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    Priority = reader.GetInt32(4),
                    Status = reader.GetString(5),
                    ReportedDate = reader.GetDateTime(6),
                    ResolvedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }
            return problems;
        }

        public async Task UpdateProblemStatusAsync(long problemId, string newStatus)
        {
            const string sql = @"
                UPDATE problems
                SET status = @status, resolved_date = CASE WHEN @status = 'Resolved' THEN NOW() ELSE resolved_date END
                WHERE id = @id;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", problemId);
            command.Parameters.AddWithValue("@status", newStatus);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ProblemDTO>> GetUnresolvedProblemsForBuildingAsync(long buildingId)
        {
            const string sql = @"
                SELECT id, building_id, tenant_id, description, priority, status, reported_date, resolved_date
                FROM problems
                WHERE building_id = @building_id AND status != 'Resolved'
                ORDER BY
                    CASE priority WHEN 3 THEN 0 WHEN 2 THEN 1 ELSE 2 END,
                    reported_date DESC;";

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@building_id", buildingId);

            var problems = new List<ProblemDTO>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                problems.Add(new ProblemDTO
                {
                    Id = reader.GetInt64(0),
                    BuildingId = reader.GetInt64(1),
                    TenantId = reader.GetInt64(2),
                    Description = reader.GetString(3),
                    Priority = reader.GetInt32(4),
                    Status = reader.GetString(5),
                    ReportedDate = reader.GetDateTime(6),
                    ResolvedDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }
            return problems;
        }
    }
}
