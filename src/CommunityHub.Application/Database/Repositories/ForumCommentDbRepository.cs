using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;   

namespace CommunityHub.Application.Database.Repositories;

public class ForumCommentDbRepository : IForumCommentRepository
{
    public long Create(ForumComment comment)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO forum_comments (forum_id, coordinator_id, neighborhood_name, content, created_at)
            VALUES (@forumId, @coordinatorId, @neighborhoodName, @content, @createdAt)
            RETURNING id";

        AddParameter(command, "@forumId", comment.ForumId);
        AddParameter(command, "@coordinatorId", comment.CoordinatorId);
        AddParameter(command, "@neighborhoodName", comment.NeighborhoodName);
        AddParameter(command, "@content", comment.Content);
        AddParameter(command, "@createdAt", comment.CreatedAt);

        return Convert.ToInt64(command.ExecuteScalar());
    }

    public List<ForumComment> GetByForumId(long forumId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                fc.id, fc.forum_id, fc.coordinator_id, fc.neighborhood_name,
                fc.content, fc.created_at,
                u.name, u.surname,
                COALESCE(SUM(CASE WHEN fr.is_positive = TRUE THEN 1 ELSE 0 END), 0) AS likes,
                COALESCE(SUM(CASE WHEN fr.is_positive = FALSE THEN 1 ELSE 0 END), 0) AS dislikes
            FROM forum_comments fc
            JOIN users u ON u.id = fc.coordinator_id
            LEFT JOIN forum_reactions fr ON fr.comment_id = fc.id
            WHERE fc.forum_id = @forumId
            GROUP BY fc.id, fc.forum_id, fc.coordinator_id, fc.neighborhood_name,
                     fc.content, fc.created_at, u.name, u.surname
            ORDER BY fc.created_at";

        AddParameter(command, "@forumId", forumId);

        using IDataReader reader = command.ExecuteReader();
        var comments = new List<ForumComment>();

        while (reader.Read())
        {
            comments.Add(new ForumComment(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["forum_id"]),
                Convert.ToInt64(reader["coordinator_id"]),
                $"{reader["name"]} {reader["surname"]}",
                reader["neighborhood_name"].ToString()!,
                reader["content"].ToString()!,
                Convert.ToDateTime(reader["created_at"]),
                Convert.ToInt32(reader["likes"]),
                Convert.ToInt32(reader["dislikes"])
            ));
        }

        return comments;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}