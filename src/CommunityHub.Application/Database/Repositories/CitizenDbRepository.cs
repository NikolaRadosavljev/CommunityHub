using CommunityHub.Application.Database;
﻿using System.Data;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class CitizenDbRepository
{
    public Citizen? GetByUserId(long userId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT u.id, u.username, u.password, u.name, u.surname, u.birthday, u.role,
                   c.address
            FROM citizens c
            JOIN users u ON u.id = c.user_id
            WHERE c.user_id = @userId";

        IDbDataParameter userIdParam = command.CreateParameter();
        userIdParam.ParameterName = "@userId";
        userIdParam.Value = userId;
        command.Parameters.Add(userIdParam);

        using IDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        User user = new User(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetDateTime(5),
            UserRole.Citizen
        );

        return new Citizen(user, reader.GetString(7));
    }
}
