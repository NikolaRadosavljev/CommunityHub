using CommunityHub.Application.Database;
﻿using System.Data;
using CommunityHub.Application.Domain.Interfaces;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class EventDbRepository : IEventRepository
{
    public List<Event> GetByNeighborhood(long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = BuildGetByNeighborhoodQuery();
        AddParameter(command, "@neighborhoodId", neighborhoodId);

        using IDataReader reader = command.ExecuteReader();
        return ReadEvents(reader);
    }

    public bool IsAlreadyRegistered(long eventId, long citizenId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
        SELECT COUNT(*) FROM event_volunteers
        WHERE event_id = @eventId AND citizen_id = @citizenId";
        AddParameter(command, "@eventId", eventId);
        AddParameter(command, "@citizenId", citizenId);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    public void Create(Event ev, List<string> itemNames)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        long eventId = InsertEvent(connection, ev);
        foreach (var itemName in itemNames)
            InsertEventItem(connection, eventId, itemName);
    }

    public void UpdateStatus(long eventId, EventStatus status)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE events SET status = @status WHERE id = @id";
        AddParameter(command, "@status", status.ToString());
        AddParameter(command, "@id", eventId);
        command.ExecuteNonQuery();
    }

    public long CreateVolunteer(long eventId, long citizenId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO event_volunteers (event_id, citizen_id, attended)
            VALUES (@eventId, @citizenId, NULL)
            RETURNING id";
        AddParameter(command, "@eventId", eventId);
        AddParameter(command, "@citizenId", citizenId);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void AddVolunteerItem(long volunteerId, long itemId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO event_volunteer_items (volunteer_id, item_id)
            VALUES (@volunteerId, @itemId)";
        AddParameter(command, "@volunteerId", volunteerId);
        AddParameter(command, "@itemId", itemId);
        command.ExecuteNonQuery();
    }

    public void RecordAttendance(long volunteerId, bool attended)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE event_volunteers SET attended = @attended WHERE id = @id";
        AddParameter(command, "@attended", attended);
        AddParameter(command, "@id", volunteerId);
        command.ExecuteNonQuery();
    }

    public List<Event> GetAllPendingStatusUpdate()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = BuildGetByNeighborhoodQuery().Replace(
            "WHERE e.neighborhood_id = @neighborhoodId",
            "WHERE e.status IN ('Preparation', 'Scheduled')"
        );

        using IDataReader reader = command.ExecuteReader();
        return ReadEvents(reader);
    }

    private string BuildGetByNeighborhoodQuery()
    {
        return @"
            SELECT e.id, e.neighborhood_id, e.organizer_id, e.name, e.description,
                   e.date, e.start_time, e.duration_minutes, e.min_volunteers, e.status,
                   i.id AS item_id, i.name AS item_name,
                   vi.volunteer_id AS item_covered_by,
                   v.id AS volunteer_id, v.citizen_id, v.attended
            FROM events e
            LEFT JOIN event_items i ON i.event_id = e.id
            LEFT JOIN event_volunteer_items vi ON vi.item_id = i.id
            LEFT JOIN event_volunteers v ON v.event_id = e.id
            WHERE e.neighborhood_id = @neighborhoodId
            ORDER BY e.date, e.start_time";
    }

    private List<Event> ReadEvents(IDataReader reader)
    {
        var events = new Dictionary<long, Event>();
        var items = new Dictionary<long, EventItem>();
        var volunteers = new Dictionary<long, EventVolunteer>();

        while (reader.Read())
        {
            long eventId = reader.GetInt64(0);
            if (!events.ContainsKey(eventId))
                events[eventId] = ReadEvent(reader);

            if (!reader.IsDBNull(10))
                ReadItem(reader, events[eventId], items);

            if (!reader.IsDBNull(13))
                ReadVolunteer(reader, events[eventId], volunteers);
        }

        return events.Values.ToList();
    }

    private Event ReadEvent(IDataReader reader)
    {
        var timeValue = reader.GetValue(6);
        TimeOnly startTime = timeValue is TimeOnly t ? t : TimeOnly.FromTimeSpan((TimeSpan)timeValue);
        return new Event(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetString(3),
            reader.GetString(4),
            DateOnly.FromDateTime(reader.GetDateTime(5)),
            startTime,
            reader.GetInt32(7),
            reader.GetInt32(8),
            Enum.Parse<EventStatus>(reader.GetString(9))
        );
    }

    private void ReadItem(IDataReader reader, Event ev, Dictionary<long, EventItem> items)
    {
        long itemId = reader.GetInt64(10);
        if (items.ContainsKey(itemId)) return;

        bool isCovered = !reader.IsDBNull(12);
        var item = new EventItem(itemId, ev.Id, reader.GetString(11), isCovered);
        items[itemId] = item;
        ev.AddItem(item);
    }

    private void ReadVolunteer(IDataReader reader, Event ev, Dictionary<long, EventVolunteer> volunteers)
    {
        long volunteerId = reader.GetInt64(13);
        if (volunteers.ContainsKey(volunteerId)) return;

        bool? attended = reader.IsDBNull(15) ? null : reader.GetBoolean(15);
        var volunteer = new EventVolunteer(volunteerId, ev.Id, reader.GetInt64(14), attended);
        volunteers[volunteerId] = volunteer;
        ev.AddVolunteer(volunteer);
    }

    private long InsertEvent(IDbConnection connection, Event ev)
    {
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO events (neighborhood_id, organizer_id, name, description,
                date, start_time, duration_minutes, min_volunteers, status)
            VALUES (@neighborhoodId, @organizerId, @name, @description,
                @date, @startTime, @durationMinutes, @minVolunteers, 'Preparation')
            RETURNING id";

        AddParameter(command, "@neighborhoodId", ev.NeighborhoodId);
        AddParameter(command, "@organizerId", ev.OrganizerId);
        AddParameter(command, "@name", ev.Name);
        AddParameter(command, "@description", ev.Description);
        AddParameter(command, "@date", ev.Date.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@startTime", ev.StartTime.ToTimeSpan());
        AddParameter(command, "@durationMinutes", ev.DurationMinutes);
        AddParameter(command, "@minVolunteers", ev.MinVolunteers);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void InsertEventItem(IDbConnection connection, long eventId, string itemName)
    {
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO event_items (event_id, name) VALUES (@eventId, @name)";
        AddParameter(command, "@eventId", eventId);
        AddParameter(command, "@name", itemName);
        command.ExecuteNonQuery();
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}