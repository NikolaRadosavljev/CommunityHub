using CommunityHub.Application.Domain;

namespace CommunityHub.Ui.Security;

public static class UserSession
{
    public static User? CurrentUser { get; private set; }

    public static bool IsAuthenticated => CurrentUser != null;

    public static void SignIn(User user)
    {
        CurrentUser = user;
    }

    public static void SignOut()
    {
        CurrentUser = null;
    }

    public static bool IsInRole(UserRole role)
    {
        return CurrentUser?.Role == role;
    }
}
