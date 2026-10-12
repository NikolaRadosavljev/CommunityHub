using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IDonationRepository
{
    List<Donation> GetByCategoryId(long categoryId);
    void Create(Donation donation);
}