using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.RepositoryInterfaces
{
    public interface IUserRepository
    {
        User? GetByCredentials(string username, string password);
        long? GetIdByCredentials(string username, string password);
        User? GetById(long userId);
        User? GetWithPosts(long userId);
        string GetFullNameById(long userId);
    }
}
