using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Domain.Interfaces;

public interface IExpenseRepository
{
    List<Expense> GetByCategoryId(long categoryId);
    void Create(Expense expense);
}