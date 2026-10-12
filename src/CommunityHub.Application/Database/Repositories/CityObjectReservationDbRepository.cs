using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class CityObjectReservationDbRepository : ICityObjectReservationRepository
{
    public long Create(CityObjectReservation reservation)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO city_object_reservations 
            (city_object_id, neighborhood_id, start_datetime, end_datetime, duration_minutes)
            VALUES (@cityObjectId, @neighborhoodId, @startDatetime, @endDatetime, @durationMinutes)
            RETURNING id";

        AddParameter(command, "@cityObjectId", reservation.CityObjectId);
        AddParameter(command, "@neighborhoodId", reservation.NeighborhoodId);
        AddParameter(command, "@startDatetime", reservation.StartDatetime);
        AddParameter(command, "@endDatetime", reservation.EndDatetime);
        AddParameter(command, "@durationMinutes", reservation.DurationMinutes);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    public List<CityObjectReservation> GetByCityObjectId(long cityObjectId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, city_object_id, neighborhood_id, start_datetime, end_datetime, duration_minutes
            FROM city_object_reservations
            WHERE city_object_id = @cityObjectId
            ORDER BY start_datetime";

        AddParameter(command, "@cityObjectId", cityObjectId);

        using IDataReader reader = command.ExecuteReader();
        return ReadReservations(reader);
    }

    public List<CityObjectReservation> GetByNeighborhoodId(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, city_object_id, neighborhood_id, start_datetime, end_datetime, duration_minutes
            FROM city_object_reservations
            WHERE neighborhood_id = @neighborhoodId
            ORDER BY start_datetime";

        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        return ReadReservations(reader);
    }

    private List<CityObjectReservation> ReadReservations(IDataReader reader)
    {
        var result = new List<CityObjectReservation>();
        while (reader.Read())
        {
            result.Add(new CityObjectReservation(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["city_object_id"]),
                Convert.ToInt64(reader["neighborhood_id"]),
                Convert.ToDateTime(reader["start_datetime"]),
                Convert.ToDateTime(reader["end_datetime"]),
                Convert.ToInt32(reader["duration_minutes"])
            ));
        }
        return result;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}