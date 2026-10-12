using System;
using System.Collections.Generic;
using CommunityHub.Application.Domain.RepositoryInterfaces;
using CommunityHub.Application.Database.Repositories;
using CommunityHub.Application.Services;

namespace CommunityHub.Ui.Infrastructure
{
    /// <summary>
    /// Dependency Injector for Manager UI components.
    /// Centralized location for instantiating services with their dependencies.
    /// </summary>
    public class ManagerDependencyInjector
    {
        private static readonly Dictionary<Type, Func<object>> _services = new()
        {
            // Register CommonSpace services and repositories
            { typeof(ICommonSpaceRepository), () => new CommonSpaceDbRepository() },
            { typeof(ICommonSpaceService), () => new CommonSpaceService(
                (ICommonSpaceRepository)CreateInstance(typeof(ICommonSpaceRepository)),
                new BuildingDbRepository()
            )},

            // Register Building services and repositories
            { typeof(IBuildingService), () => new BuildingService((IBuildingRepository)CreateInstance(typeof(IBuildingRepository))) },
            { typeof(IBuildingRepository), () => new BuildingDbRepository() },

            // Register Building Request repository (not an interface yet - complex DTO)
            { typeof(BuildingRequestDbRepository), () => new BuildingRequestDbRepository() },

            // Register Rental Request services
            { typeof(ICommonSpaceRentalRequestService), () => new CommonSpaceRentalRequestService() },

            // Homeowners meeting repository (tenant repo) - required by manager services
            { typeof(IHomeownersMeetingRepository), () => new HomeownersMeetingDbRepository() },

            // Manager assemblies service
            { typeof(IAssemblyService), () => new AssemblyService((IHomeownersMeetingRepository)CreateInstance(typeof(IHomeownersMeetingRepository))) },

            // Problem repository
            { typeof(IProblemRepository), () => new ProblemDbRepository() },
        };

        /// <summary>
        /// Creates an instance of the requested service type.
        /// </summary>
        public static T CreateInstance<T>() where T : class
        {
            return (T)CreateInstance(typeof(T));
        }

        /// <summary>
        /// Creates an instance of the requested service type.
        /// </summary>
        private static object CreateInstance(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var factory))
            {
                return factory();
            }

            throw new ArgumentException($"No implementation found for service type {serviceType.Name}");
        }
    }
}
