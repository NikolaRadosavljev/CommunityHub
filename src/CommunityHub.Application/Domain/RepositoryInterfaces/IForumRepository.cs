using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IForumRepository
{
    long Create(Forum forum);
    List<Forum> GetAllVisible(long coordinatorId);
    List<Forum> Search(long coordinatorId, string? title, string? description);
    Forum? GetById(long forumId);
    void Close(long forumId);
}