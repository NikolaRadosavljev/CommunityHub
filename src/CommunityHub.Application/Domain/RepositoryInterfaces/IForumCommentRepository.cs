using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IForumCommentRepository
{
    long Create(ForumComment comment);
    List<ForumComment> GetByForumId(long forumId);
}