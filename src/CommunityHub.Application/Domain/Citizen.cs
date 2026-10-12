namespace CommunityHub.Application.Domain;

public class Citizen
{
    public User User { get; private set; }
    public string Address { get; private set; }

    public Citizen(User user, string address)
    {
        User = user;
        Address = address;
    }
}


