using CommunityHub.Application.Database;
using System.Data;
using CommunityHub.Application.Domain;

namespace CommunityHub.Application.Database.Repositories;

public class CountryDbRepository
{
    public Country Create(Country country)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO countries (name)
            VALUES (@name)
            RETURNING id";

        IDbDataParameter nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = country.Name;
        command.Parameters.Add(nameParam);

        // ExecuteScalar vraća prvu kolonu prvog reda (id u ovom slučaju)
        long id = Convert.ToInt64(command.ExecuteScalar());

        return new Country(id, country.Name);
    }
    public long? GetIdByName(string name)
    {
        // using blok automatski zatvara konekciju na kraju bloka u kom je pozvan
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM countries WHERE name = @name";

        // Parametrizovani upiti sprečavaju SQL injection napade
        IDbDataParameter nameParam = command.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = name;
        command.Parameters.Add(nameParam);

        // ExecuteScalar vraća prvu kolonu prvog reda (id u ovom slučaju)
        object? result = command.ExecuteScalar();

        if (result != null)
        {
            return Convert.ToInt64(result);
        }

        return null;
    }

    public Country? GetWithCities(long countryId)
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        // LEFT JOIN vraća zemlju čak i ako nema gradove
        // Rezultat: ako zemlja ima 3 grada, dobijamo 3 reda sa istom zemljom
        command.CommandText = @"
            SELECT country.id, country.name,
                   city.id AS city_id, city.name, city.postalcode
            FROM countries country
            LEFT JOIN cities city ON country.id = city.country_id
            WHERE country.id = @countryId";

        IDbDataParameter countryIdParam = command.CreateParameter();
        countryIdParam.ParameterName = "@countryId";
        countryIdParam.Value = countryId;
        command.Parameters.Add(countryIdParam);

        // ExecuteReader vraća IDataReader za čitanje više redova
        using IDataReader reader = command.ExecuteReader();

        Country? country = null;

        // Vise detalja za kod koji sledi pogledaj u metodi GetWithPosts u UserDbRepository,
        // gde je u komentarima objasnjeno sta sve ovaj kod radi
        while (reader.Read())
        {
            if (country == null)
            {
                long id = reader.GetInt64(0);
                string countryName = reader.GetString(1);

                country = new Country(id, countryName);
            }

            if (reader.IsDBNull(2)) continue;

            long cityId = reader.GetInt64(2);
            string name = reader.GetString(3);
            string postalCode = reader.GetString(4);

            City city = new City(cityId, name, postalCode);
            country.AddCity(city);
        }

        return country;
    }

    public List<Country> GetAll()
    {
        using IDbConnection connection = PostgresConnection.CreateConnection();

        IDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id, name FROM countries ORDER BY name";

        using IDataReader reader = command.ExecuteReader();

        List<Country> countries = new List<Country>();

        while (reader.Read())
        {
            long id = reader.GetInt64(0);
            string name = reader.GetString(1);

            Country country = new Country(id, name);
            countries.Add(country);
        }

        return countries;
    }
}
