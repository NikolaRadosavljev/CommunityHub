using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;
using Npgsql;

namespace CommunityHub.Application.Database.Repositories;

public class RentalRequestDbRepository
{
    public int Create(RentalRequest request)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO rental_requests (common_room_id, tenant_id, start_date, end_date, status, created_at, suggested_start_date, suggested_end_date)
            VALUES (@commonRoomId, @tenantId, @startDate, @endDate, @status, @createdAt, @suggestedStartDate, @suggestedEndDate)
            RETURNING id";

        AddParameter(command, "@commonRoomId", request.CommonRoomId);
        AddParameter(command, "@tenantId", request.TenantId);
        AddParameter(command, "@startDate", request.StartDate);
        AddParameter(command, "@endDate", request.EndDate);
        AddParameter(command, "@status", request.Status);
        AddParameter(command, "@createdAt", request.CreatedAt);
        AddParameter(command, "@suggestedStartDate", request.SuggestedStartDate.HasValue ? (object)request.SuggestedStartDate.Value : DBNull.Value);
        AddParameter(command, "@suggestedEndDate", request.SuggestedEndDate.HasValue ? (object)request.SuggestedEndDate.Value : DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<RentalRequest> GetAllByTenantId(long tenantId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, common_room_id, tenant_id, start_date, end_date, status, created_at, approved_date, suggested_start_date, suggested_end_date
            FROM rental_requests
            WHERE tenant_id = @tenantId
            ORDER BY created_at DESC";

        AddParameter(command, "@tenantId", tenantId);

        var requests = new List<RentalRequest>();
        using IDataReader reader = command.ExecuteReader();
        {
            while (reader.Read())
            {
                requests.Add(new RentalRequest(
                    id: Convert.ToInt32(reader["id"]),
                    commonRoomId: Convert.ToInt32(reader["common_room_id"]),
                    tenantId: Convert.ToInt64(reader["tenant_id"]),
                    startDate: Convert.ToDateTime(reader["start_date"]),
                    endDate: Convert.ToDateTime(reader["end_date"]),
                    status: reader["status"].ToString(),
                    createdAt: Convert.ToDateTime(reader["created_at"]),
                    approvedDate: reader["approved_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_date"]),
                    suggestedStartDate: reader["suggested_start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_start_date"]),
                    suggestedEndDate: reader["suggested_end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_end_date"])
                ));
            }
        }
        return requests;
    }

    public List<RentalRequest> GetAllByTenantIdAndBuilding(long tenantId, int buildingId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT rr.id, rr.common_room_id, rr.tenant_id, rr.start_date, rr.end_date, rr.status, rr.created_at, rr.approved_date, rr.suggested_start_date, rr.suggested_end_date
            FROM rental_requests rr
            INNER JOIN common_spaces cs ON rr.common_room_id = cs.id
            WHERE rr.tenant_id = @tenantId AND cs.building_id = @buildingId
            ORDER BY rr.created_at DESC";

        AddParameter(command, "@tenantId", tenantId);
        AddParameter(command, "@buildingId", buildingId);

        var requests = new List<RentalRequest>();
        using IDataReader reader = command.ExecuteReader();
        {
            while (reader.Read())
            {
                requests.Add(new RentalRequest(
                    id: Convert.ToInt32(reader["id"]),
                    commonRoomId: Convert.ToInt32(reader["common_room_id"]),
                    tenantId: Convert.ToInt64(reader["tenant_id"]),
                    startDate: Convert.ToDateTime(reader["start_date"]),
                    endDate: Convert.ToDateTime(reader["end_date"]),
                    status: reader["status"].ToString(),
                    createdAt: Convert.ToDateTime(reader["created_at"]),
                    approvedDate: reader["approved_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_date"]),
                    suggestedStartDate: reader["suggested_start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_start_date"]),
                    suggestedEndDate: reader["suggested_end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_end_date"])
                ));
            }
        }
        return requests;
    }

    public RentalRequest GetById(int id)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, common_room_id, tenant_id, start_date, end_date, status, created_at, approved_date, suggested_start_date, suggested_end_date
            FROM rental_requests
            WHERE id = @id";

        AddParameter(command, "@id", id);

        using IDataReader reader = command.ExecuteReader();
        {
            if (reader.Read())
            {
                return new RentalRequest(
                    id: Convert.ToInt32(reader["id"]),
                    commonRoomId: Convert.ToInt32(reader["common_room_id"]),
                    tenantId: Convert.ToInt64(reader["tenant_id"]),
                    startDate: Convert.ToDateTime(reader["start_date"]),
                    endDate: Convert.ToDateTime(reader["end_date"]),
                    status: reader["status"].ToString(),
                    createdAt: Convert.ToDateTime(reader["created_at"]),
                    approvedDate: reader["approved_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_date"]),
                    suggestedStartDate: reader["suggested_start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_start_date"]),
                    suggestedEndDate: reader["suggested_end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_end_date"])
                );
            }
        }
        return null;
    }

    public List<RentalRequest> GetAllByCommonRoomId(int commonRoomId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, common_room_id, tenant_id, start_date, end_date, status, created_at, approved_date, suggested_start_date, suggested_end_date
            FROM rental_requests
            WHERE common_room_id = @commonRoomId
            ORDER BY start_date";

        AddParameter(command, "@commonRoomId", commonRoomId);

        var requests = new List<RentalRequest>();
        using IDataReader reader = command.ExecuteReader();
        {
            while (reader.Read())
            {
                requests.Add(new RentalRequest(
                    id: Convert.ToInt32(reader["id"]),
                    commonRoomId: Convert.ToInt32(reader["common_room_id"]),
                    tenantId: Convert.ToInt64(reader["tenant_id"]),
                    startDate: Convert.ToDateTime(reader["start_date"]),
                    endDate: Convert.ToDateTime(reader["end_date"]),
                    status: reader["status"].ToString(),
                    createdAt: Convert.ToDateTime(reader["created_at"]),
                    approvedDate: reader["approved_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["approved_date"]),
                    suggestedStartDate: reader["suggested_start_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_start_date"]),
                    suggestedEndDate: reader["suggested_end_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["suggested_end_date"])
                ));
            }
        }
        return requests;
    }

    public void UpdateStatus(int requestId, string newStatus)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE rental_requests
            SET status = @status
            WHERE id = @id";

        AddParameter(command, "@status", newStatus);
        AddParameter(command, "@id", requestId);

        command.ExecuteNonQuery();
    }

    public void UpdateSuggestedDates(int requestId, DateTime suggestedStartDate, DateTime suggestedEndDate)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE rental_requests
            SET suggested_start_date = @startDate, suggested_end_date = @endDate
            WHERE id = @id";

        AddParameter(command, "@startDate", suggestedStartDate);
        AddParameter(command, "@endDate", suggestedEndDate);
        AddParameter(command, "@id", requestId);

        command.ExecuteNonQuery();
    }


    public void AcceptSuggestedDates(int requestId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE rental_requests
            SET start_date = suggested_start_date,
                end_date = suggested_end_date,
                status = 'Pending',
                suggested_start_date = NULL,
                suggested_end_date = NULL
            WHERE id = @id AND status = 'Pending - Term Change'";

        AddParameter(command, "@id", requestId);

        command.ExecuteNonQuery();
    }

    public void CancelRequest(int requestId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM rental_requests
            WHERE id = @id AND status IN ('Pending', 'Pending - Term Change')";

        AddParameter(command, "@id", requestId);

        command.ExecuteNonQuery();
    }

    private static void AddParameter(IDbCommand command, string parameterName, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
