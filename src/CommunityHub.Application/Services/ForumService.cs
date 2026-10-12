using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Services;

public class ForumService
{
    private readonly IForumRepository _forumRepository;
    private readonly IForumCommentRepository _commentRepository;
    private readonly IForumReactionRepository _reactionRepository;
    private readonly INeighborhoodRepository _neighborhoodRepository;

    public ForumService(IForumRepository forumRepository,
                        IForumCommentRepository commentRepository,
                        IForumReactionRepository reactionRepository,
                        INeighborhoodRepository neighborhoodRepository)
    {
        _forumRepository = forumRepository;
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _neighborhoodRepository = neighborhoodRepository;
    }

    public long CreateForum(long coordinatorId, string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Naslov foruma je obavezan.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Opis foruma je obavezan.");

        Forum forum = new Forum(0, coordinatorId, title, description);
        return _forumRepository.Create(forum);
    }

    public List<Forum> GetVisibleForums(long coordinatorId, string? title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            return _forumRepository.GetAllVisible(coordinatorId);

        return _forumRepository.Search(coordinatorId, title, description);
    }

    public void CloseForum(long forumId, long coordinatorId)
    {
        Forum? forum = _forumRepository.GetById(forumId);

        if (forum == null)
            throw new ArgumentException("Forum nije pronađen.");

        if (forum.CoordinatorId != coordinatorId)
            throw new InvalidOperationException("Samo kreator foruma može zatvoriti forum.");

        forum.Close();
        _forumRepository.Close(forumId);
    }

    public long AddComment(long forumId, long coordinatorId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Komentar ne može biti prazan.");

        Forum? forum = _forumRepository.GetById(forumId);
        if (forum == null)
            throw new ArgumentException("Forum nije pronađen.");

        if (forum.IsClosed)
            throw new InvalidOperationException("Ne možete komentarisati zatvoreni forum.");

        List<Neighborhood> neighborhoods = _neighborhoodRepository
            .Search(null, null, null, null)
            .Where(n => n.CoordinatorId == coordinatorId)
            .ToList();

        string neighborhoodName = neighborhoods.Count > 0
            ? neighborhoods.First().Name
            : "Nepoznat kvart";

        ForumComment comment = new ForumComment(
            0, forumId, coordinatorId, "", neighborhoodName, content, DateTime.UtcNow
        );

        return _commentRepository.Create(comment);
    }

    public List<ForumComment> GetComments(long forumId)
    {
        return _commentRepository.GetByForumId(forumId);
    }

    public void ReactToComment(long commentId, long coordinatorId, bool isPositive)
    {
        ForumReaction? existing = _reactionRepository
            .GetByCommentAndCoordinator(commentId, coordinatorId);

        if (existing == null)
        {
            ForumReaction reaction = new ForumReaction(0, commentId, coordinatorId, isPositive);
            _reactionRepository.Create(reaction);
        }
        else if (existing.IsPositive == isPositive)
        {
            _reactionRepository.Delete(commentId, coordinatorId);
        }
        else
        {
            _reactionRepository.Update(commentId, coordinatorId, isPositive);
        }
    }
}