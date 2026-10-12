namespace CommunityHub.Application.Domain;

public class Country
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public List<City>? Cities { get; private set; }

    public Country(long id, string name)
    {
        Id = id;
        Name = name;
        Cities = null;
    }

    public Country(string name)
    {
        Name = name;
        Cities = null;
    }

    public void AddCity(City city)
    {
        if (Cities == null)
        {
            Cities = new List<City>();
        }

        Cities.Add(city);
        city.Country = this;
    }
}
