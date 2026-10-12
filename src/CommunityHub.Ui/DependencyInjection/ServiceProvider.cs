using System;
using CommunityHub.Application;
using CommunityHub.Application.Database;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Database.Repositories.HelpAds;
using CommunityHub.Application.Domain.RepositoryInterfaces.HelpAds;
using CommunityHub.Application.Services.HelpAds;
using CommunityHub.Application.Services;
using CommunityHub.Ui.Views.TenantViews;

namespace CommunityHub.Ui.DependencyInjection;

/// <summary>
/// Konfiguracija Dependency Injection
/// Prikazuje kako koristiti interfejse umesto konkretnih klasa
///
/// NAPOMENA: Za kompletan DI container, trebate instalirati:
/// dotnet add package Microsoft.Extensions.DependencyInjection
/// </summary>
public static class DIConfiguration
{
    private static readonly string ConnectionString = CommunityHub.Application.Database.PostgresConnection.GetConnectionString();

    /// <summary>
    /// Kreira interfejse umesto direktnih klasa - omogućava testiranje sa mock objektima
    /// </summary>
    public static class Factory
    {
        public static IUserRepository CreateUserRepository()
        {
            return new UserDbRepository();
        }

        public static IBuildingRepository CreateBuildingRepository()
        {
            return new BuildingDbRepository();
        }

        public static IBuildingMembershipRepository CreateBuildingMembershipRepository()
        {
            return new BuildingMembershipDbRepository();
        }

        public static IHelpAdRepository CreateHelpAdRepository()
        {
            return new HelpAdDbRepository(ConnectionString);
        }

        public static IHelpAdService CreateHelpAdService()
        {
            return new HelpAdService(CreateHelpAdRepository(), CreateHelpAdNotificationRepository());
        }

        public static HelpAdMatchingService CreateHelpAdMatchingService()
        {
            return new HelpAdMatchingService(
                CreateHelpAdService(),
                CreateHelpAdBookingRepository(),
                CreateHelpAdNotificationRepository()
            );
        }

        public static HelpAdBookingRepository CreateHelpAdBookingRepository()
        {
            return new HelpAdBookingRepository(ConnectionString);
        }

        public static HelpAdNotificationRepository CreateHelpAdNotificationRepository()
        {
            return new HelpAdNotificationRepository(ConnectionString);
        }

        public static ProblemService CreateProblemService()
        {
            var problemRepository = new ProblemDbRepository(ConnectionString);
            return new ProblemService(problemRepository);
        }

        public static HomeownersMeetingService CreateHomeownersMeetingService()
        {
            var meetingRepository = new HomeownersMeetingDbRepository(ConnectionString);
            var userRepository = CreateUserRepository();
            var buildingRepository = CreateBuildingRepository();
            return new HomeownersMeetingService(meetingRepository, userRepository, buildingRepository);
        }
    }

    /// <summary>
    /// Primer kako koristiti interfejse sa mock objektima za testiranje
    /// </summary>
    public static class MockFactory
    {
        public static IUserRepository CreateMockUserRepository()
        {
            return new MockUserRepository();
        }

        public static IBuildingRepository CreateMockBuildingRepository()
        {
            return new MockBuildingRepository();
        }

        public static IBuildingMembershipRepository CreateMockBuildingMembershipRepository()
        {
            return new MockBuildingMembershipRepository();
        }
    }
}

/// <summary>
/// Helper klasa za lifetime management
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// Kreira se nova instanca svakog puta
    /// </summary>
    Transient = 0,

    /// <summary>
    /// Kreira se nova instanca po akciji
    /// </summary>
    Scoped = 1,

    /// <summary>
    /// Kreira se samo jedna instanca
    /// </summary>
    Singleton = 2
}
