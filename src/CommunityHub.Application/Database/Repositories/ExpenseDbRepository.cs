using System.Data;
using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class ExpenseDbRepository : IExpenseRepository
{
    public List<Expense> GetByCategoryId(long categoryId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, category_id, amount, description, expense_date
            FROM expenses
            WHERE category_id = @categoryId
            ORDER BY expense_date DESC";

        AddParameter(command, "@categoryId", categoryId);

        using IDataReader reader = command.ExecuteReader();
        var result = new List<Expense>();

        while (reader.Read())
        {
            var expenseDate = reader["expense_date"] is DateOnly d ? d :
                DateOnly.FromDateTime(Convert.ToDateTime(reader["expense_date"]));

            result.Add(new Expense(
                Convert.ToInt64(reader["id"]),
                Convert.ToInt64(reader["category_id"]),
                Convert.ToDecimal(reader["amount"]),
                reader["description"].ToString()!,
                expenseDate
            ));
        }

        return result;
    }

    public void Create(Expense expense)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();
        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO expenses (category_id, amount, description, expense_date)
            VALUES (@categoryId, @amount, @description, @expenseDate)";

        AddParameter(command, "@categoryId", expense.CategoryId);
        AddParameter(command, "@amount", expense.Amount);
        AddParameter(command, "@description", expense.Description);
        AddParameter(command, "@expenseDate", expense.ExpenseDate.ToDateTime(TimeOnly.MinValue));

        command.ExecuteNonQuery();
    }

    private void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }
}