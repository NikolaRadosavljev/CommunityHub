using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class ForumDbRepository : IForumRepository
{
    public long Create(Forum forum)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO forums (coordinator_id, title, description, is_closed)
            VALUES (@coordinatorId, @title, @description, @isClosed)
            RETURNING id";

        AddParameter(command, "@coordinatorId", forum.CoordinatorId);
        AddParameter(command, "@title", forum.Title);
        AddParameter(command, "@description", forum.Description);
        AddParameter(command, "@isClosed", forum.IsClosed);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    public List<Forum> GetAllVisible(long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, coordinator_id, title, description, is_closed
            FROM forums
            WHERE is_closed = FALSE OR coordinator_id = @coordinatorId
            ORDER BY id DESC";

        AddParameter(command, "@coordinatorId", coordinatorId);

        using IDataReader reader = command.ExecuteReader();
        return ReadForums(reader);
    }

    public List<Forum> Search(long coordinatorId, string? title, string? description)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, coordinator_id, title, description, is_closed
            FROM forums
            WHERE (is_closed = FALSE OR coordinator_id = @coordinatorId)"
            + (string.IsNullOrWhiteSpace(title) ? "" : " AND title ILIKE @title")
            + (string.IsNullOrWhiteSpace(description) ? "" : " AND description ILIKE @description")
            + " ORDER BY id DESC";

        AddParameter(command, "@coordinatorId", coordinatorId);

        if (!string.IsNullOrWhiteSpace(title))
            AddParameter(command, "@title", $"%{title}%");

        if (!string.IsNullOrWhiteSpace(description))
            AddParameter(command, "@description", $"%{description}%");

        using IDataReader reader = command.ExecuteReader();
        return ReadForums(reader);
    }

    public void Close(long forumId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE forums
            SET is_closed = TRUE
            WHERE id = @id";

        AddParameter(command, "@id", forumId);
        command.ExecuteNonQuery();
    }

    public Forum? GetById(long forumId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, coordinator_id, title, description, is_closed
            FROM forums
            WHERE id = @id";

        AddParameter(command, "@id", forumId);

        using IDataReader reader = command.ExecuteReader();
        var forums = ReadForums(reader);
        return forums.FirstOrDefault();
    }

    private List<Forum> ReadForums(IDataReader reader)
    {
        var forums = new List<Forum>();

        while (reader.Read())
        {
            forums.Add(new Forum(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["coordinator_id"]),
                reader["title"].ToString()!,
                reader["description"].ToString()!,
                Convert.ToBoolean(reader["is_closed"])
            ));
        }

        return forums;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}