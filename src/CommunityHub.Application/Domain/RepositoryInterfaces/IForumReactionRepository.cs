using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IForumReactionRepository
{
    void Create(ForumReaction reaction);
    void Update(long commentId, long coordinatorId, bool isPositive);
    void Delete(long commentId, long coordinatorId);
    ForumReaction? GetByCommentAndCoordinator(long commentId, long coordinatorId);
}