using CommunityHub.Application.Domain;
using CommunityHub.Application.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using CommunityHub.Application.Domain.Interfaces;   

namespace CommunityHub.Application.Database.Repositories
{
    public class NeighborhoodDbRepository : INeighborhoodRepository
    {
        public long Create(Neighborhood neighborhood)
        {
            if (neighborhood.City == null)
                throw new ArgumentException("Neighborhood must have a city.");

            using IDbConnection connection = PostgresConnection.CreateConnection();

            IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO neighborhoods (coordinator_id, name, description, budget, city_id, images)
                VALUES (@coordinatorId, @name, @description, @budget, @cityId, @images)
                RETURNING id";

            IDbDataParameter coordinatorIdParam = command.CreateParameter();
            coordinatorIdParam.ParameterName = "@coordinatorId";
            coordinatorIdParam.Value = neighborhood.CoordinatorId;
            command.Parameters.Add(coordinatorIdParam);

            IDbDataParameter nameParam = command.CreateParameter();
            nameParam.ParameterName = "@name";
            nameParam.Value = neighborhood.Name;
            command.Parameters.Add(nameParam);

            IDbDataParameter descriptionParam = command.CreateParameter();
            descriptionParam.ParameterName = "@description";
            descriptionParam.Value = neighborhood.Description;
            command.Parameters.Add(descriptionParam);

            IDbDataParameter budgetParam = command.CreateParameter();
            budgetParam.ParameterName = "@budget";
            budgetParam.Value = neighborhood.Budget;
            command.Parameters.Add(budgetParam);

            IDbDataParameter cityIdParam = command.CreateParameter();
            cityIdParam.ParameterName = "@cityId";
            cityIdParam.Value = neighborhood.City.Id;
            command.Parameters.Add(cityIdParam);

            IDbDataParameter imagesParam = command.CreateParameter();
            imagesParam.ParameterName = "@images";
            imagesParam.Value = neighborhood.Images.ToArray();
            command.Parameters.Add(imagesParam);

            long id = Convert.ToInt64(command.ExecuteScalar());
            return id;
        }

        public List<Neighborhood> Search(string? name, string? address, string? city, string? country)
        {
            using IDbConnection connection = PostgresConnection.CreateConnection();
            IDbCommand command = connection.CreateCommand();
            command.CommandText = BuildSearchQuery(name, address, city, country);
            AddSearchParameters(command, name, address, city, country);

            using IDataReader reader = command.ExecuteReader();
            return ReadNeighborhoods(reader);
        }

        private string BuildSearchQuery(string? name, string? address, string? city, string? country)
        {
            var (streetName, hasNumber) = ParseAddress(address);

            return @"
               SELECT n.id, n.coordinator_id, n.name, n.description, n.images,
               s.id AS street_id, s.name AS street_name, s.start_number, s.end_number,
               c.id AS city_id, c.name AS city_name, c.postalcode,
               co.id AS country_id, co.name AS country_name
               FROM neighborhoods n
               LEFT JOIN streets s ON s.neighborhood_id = n.id
               LEFT JOIN cities c ON c.id = n.city_id
               LEFT JOIN countries co ON co.id = c.country_id
               WHERE 1=1"
               + (string.IsNullOrWhiteSpace(name) ? "" : " AND n.name ILIKE @name")
               + (streetName == null ? "" : hasNumber
               ? " AND s.name ILIKE @streetName AND s.start_number <= @streetNumber AND s.end_number >= @streetNumber"
               : " AND s.name ILIKE @streetName")
               + (string.IsNullOrWhiteSpace(city) ? "" : " AND c.name ILIKE @city")
               + (string.IsNullOrWhiteSpace(country) ? "" : " AND co.name ILIKE @country");
        }

        private void AddSearchParameters(IDbCommand command, string? name, string? address, string? city, string? country)
        {
            if (!string.IsNullOrWhiteSpace(name))
                AddParameter(command, "@name", $"%{name}%");

            var (streetName, hasNumber) = ParseAddress(address);
            if (streetName != null)
            {
                AddParameter(command, "@streetName", $"%{streetName}%");
                if (hasNumber)
                {
                    var parts = address!.Trim().Split(' ');
                    AddParameter(command, "@streetNumber", int.Parse(parts.Last()));
                }
            }

            if (!string.IsNullOrWhiteSpace(city))
                AddParameter(command, "@city", $"%{city}%");

            if (!string.IsNullOrWhiteSpace(country))
                AddParameter(command, "@country", $"%{country}%");
        }

        private (string? streetName, bool hasNumber) ParseAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, false);

            var parts = address.Trim().Split(' ');
            var hasNumber = int.TryParse(parts.Last(), out _);
            var streetName = hasNumber
                ? string.Join(" ", parts.Take(parts.Length - 1))
                : address;

            return (streetName, hasNumber);
        }

        private void AddParameter(IDbCommand command, string name, object value)
        {
            IDbDataParameter param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }

        private List<Neighborhood> ReadNeighborhoods(IDataReader reader)
        {
            var neighborhoods = new Dictionary<long, Neighborhood>();

            while (reader.Read())
            {
                long id = reader.GetInt64(0);

                if (!neighborhoods.TryGetValue(id, out var neighborhood))
                {
                    neighborhood = new Neighborhood(
                        id,
                        reader.GetInt64(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    );

                    var images = (reader["images"] as string[])?.ToList() ?? new List<string>();
                    neighborhood.Images.AddRange(images);

                    if (!reader.IsDBNull(9))
                        neighborhood.City = ReadCity(reader);

                    neighborhoods[id] = neighborhood;
                }

                if (!reader.IsDBNull(5))
                    neighborhood.AddStreet(ReadStreet(reader, id));
            }

            return neighborhoods.Values.ToList();
        }

        private City ReadCity(IDataReader reader)
        {
            City city = new City(reader.GetInt64(9), reader.GetString(10), reader.GetString(11));

            if (!reader.IsDBNull(12))
            {
                Country country = new Country(reader.GetInt64(12), reader.GetString(13));
                country.AddCity(city);
            }

            return city;
        }

        private Street ReadStreet(IDataReader reader, long neighborhoodId)
        {
            return new Street(
                reader.GetInt64(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                neighborhoodId
            );
        }
    }
}