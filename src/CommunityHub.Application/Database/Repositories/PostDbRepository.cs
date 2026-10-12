using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class PostDbRepository
{
    public Post Create(Post post, long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO posts (title, content, created_at, user_id)
            VALUES (@title, @content, @createdAt, @userId)
            RETURNING id";

        IDbDataParameter titleParam = command.CreateParameter();
        titleParam.ParameterName = "@title";
        titleParam.Value = post.Title;
        command.Parameters.Add(titleParam);

        IDbDataParameter contentParam = command.CreateParameter();
        contentParam.ParameterName = "@content";
        contentParam.Value = post.Content;
        command.Parameters.Add(contentParam);

        IDbDataParameter createdAtParam = command.CreateParameter();
        createdAtParam.ParameterName = "@createdAt";
        createdAtParam.Value = post.CreatedAt;
        command.Parameters.Add(createdAtParam);

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        // ExecuteScalar vraća prvu kolonu prvog reda (id u ovom slučaju)
        long id = Convert.ToInt64(command.ExecuteScalar());

        return new Post(id, post.Title, post.Content, post.CreatedAt);
    }
}
