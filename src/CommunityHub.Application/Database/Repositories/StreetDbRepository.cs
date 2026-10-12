using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;

namespace CommunityHub.Application.Database.Repositories;

public class StreetDbRepository : IStreetRepository
{
    public Street Create(Street street, long neighborhoodId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO streets (name, start_number, end_number, neighborhood_id)
            VALUES (@name, @startNumber, @endNumber, @neighborhoodId)
            RETURNING id";

        IDbDataParameter nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = street.Name;
        command.Parameters.Add(nameParam);

        IDbDataParameter startNumberParam = command.CreateParameter();
        startNumberParam.ParameterName = "@startNumber";
        startNumberParam.Value = street.StartNumber;
        command.Parameters.Add(startNumberParam);

        IDbDataParameter endNumberParam = command.CreateParameter();
        endNumberParam.ParameterName = "@endNumber";
        endNumberParam.Value = street.EndNumber;
        command.Parameters.Add(endNumberParam);

        IDbDataParameter neighborhoodIdParam = command.CreateParameter();
        neighborhoodIdParam.ParameterName = "@neighborhoodId";
        neighborhoodIdParam.Value = neighborhoodId;
        command.Parameters.Add(neighborhoodIdParam);

        long id = Convert.ToInt64(command.ExecuteScalar());

        return new Street(id, street.Name, street.StartNumber, street.EndNumber, neighborhoodId);
    }
}