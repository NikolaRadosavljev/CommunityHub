using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class ForumReactionDbRepository : IForumReactionRepository
{
    public void Create(ForumReaction reaction)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO forum_reactions (comment_id, coordinator_id, is_positive)
            VALUES (@commentId, @coordinatorId, @isPositive)";

        AddParameter(command, "@commentId", reaction.CommentId);
        AddParameter(command, "@coordinatorId", reaction.CoordinatorId);
        AddParameter(command, "@isPositive", reaction.IsPositive);

        command.ExecuteNonQuery();
    }

    public void Update(long commentId, long coordinatorId, bool isPositive)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE forum_reactions
            SET is_positive = @isPositive
            WHERE comment_id = @commentId AND coordinator_id = @coordinatorId";

        AddParameter(command, "@isPositive", isPositive);
        AddParameter(command, "@commentId", commentId);
        AddParameter(command, "@coordinatorId", coordinatorId);

        command.ExecuteNonQuery();
    }

    public void Delete(long commentId, long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM forum_reactions
            WHERE comment_id = @commentId AND coordinator_id = @coordinatorId";

        AddParameter(command, "@commentId", commentId);
        AddParameter(command, "@coordinatorId", coordinatorId);

        command.ExecuteNonQuery();
    }

    public ForumReaction? GetByCommentAndCoordinator(long commentId, long coordinatorId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, comment_id, coordinator_id, is_positive
            FROM forum_reactions
            WHERE comment_id = @commentId AND coordinator_id = @coordinatorId";

        AddParameter(command, "@commentId", commentId);
        AddParameter(command, "@coordinatorId", coordinatorId);

        using IDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new ForumReaction(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["comment_id"]),
                Convert.ToInt64(reader["coordinator_id"]),
                Convert.ToBoolean(reader["is_positive"])
            );
        }

        return null;
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}