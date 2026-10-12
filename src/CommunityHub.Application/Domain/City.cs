namespace CommunityHub.Application.Domain;

public class City
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public string PostalCode { get; private set; }
    public Country? Country { get; internal set; }

    public City(long id, string name, string postalCode)
    {
        Id = id;
        Name = name;
        PostalCode = postalCode;
        Country = null;
    }

    public City(string name, string postalCode)
    {
        Id = 0;
        Name = name;
        PostalCode = postalCode;
        Country = null;
    }
}
