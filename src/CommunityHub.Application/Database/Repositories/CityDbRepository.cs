using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class CityDbRepository
{
    public City Create(City city, long countryId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO cities (name, postalcode, country_id)
            VALUES (@name, @postalcode, @countryId)
            RETURNING id";

        IDbDataParameter nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = city.Name;
        command.Parameters.Add(nameParam);

        IDbDataParameter postalCodeParam = command.CreateParameter();
        postalCodeParam.ParameterName = "@postalcode";
        postalCodeParam.Value = city.PostalCode;
        command.Parameters.Add(postalCodeParam);

        IDbDataParameter countryIdParam = command.CreateParameter();
        countryIdParam.ParameterName = "@countryId";
        countryIdParam.Value = countryId;
        command.Parameters.Add(countryIdParam);

        // ExecuteScalar vraća prvu kolonu prvog reda (id u ovom slučaju)
        long id = Convert.ToInt64(command.ExecuteScalar());

        return new City(id, city.Name, city.PostalCode);
    }

    public void Delete(long cityId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM cities WHERE id = @id";

        var param = command.CreateParameter();
        param.ParameterName = "@id";
        param.Value = cityId;
        command.Parameters.Add(param);

        command.ExecuteNonQuery();
    }

    public void Update(City city)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
        UPDATE cities
        SET name = @name,
            postalcode = @postalcode
        WHERE id = @id";

        var idParam = command.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = city.Id;
        command.Parameters.Add(idParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = city.Name;
        command.Parameters.Add(nameParam);

        var postalParam = command.CreateParameter();
        postalParam.ParameterName = "@postalcode";
        postalParam.Value = city.PostalCode;
        command.Parameters.Add(postalParam);

        command.ExecuteNonQuery();
    }


    public List<City> GetAll()
    {
        List<City> cities = new List<City>();

        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id, name, postalcode FROM cities";

        using IDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            long id = Convert.ToInt64(reader["id"]);
            string name = reader["name"].ToString()!;
            string postalCode = reader["postalcode"].ToString()!;

            City city = new City(id, name, postalCode);
            cities.Add(city);
        }

        return cities;
    }
}
