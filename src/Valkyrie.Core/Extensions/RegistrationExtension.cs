﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Valkyrie.Core.Abstractions;
using Valkyrie.Core.Attributes;

namespace Valkyrie.Core.Extensions
{
   internal static class RegistrationExtension
    {
        
        public static void RegisterAttributes(this IServiceCollection serviceCollection)
        {

            serviceCollection.Scan(
                selector =>
                    selector.FromApplicationDependencies().AddClasses(
                            x => x.WithAttribute<SingletonAttribute>()
                        )
                        .AsSelfWithInterfaces()
                        .WithSingletonLifetime()
            );

            serviceCollection.Scan(
                selector =>
                    selector.FromApplicationDependencies().AddClasses(
                            x => x.WithAttribute<ScopedAttribute>()
                        )
                        .AsSelfWithInterfaces()
                        .WithScopedLifetime()
            );



            serviceCollection.Scan(
                selector =>
                    selector.FromApplicationDependencies().AddClasses(
                            x => x.WithAttribute<TransientAttribute>()
                        )
                        .AsSelfWithInterfaces()
                        .WithTransientLifetime()
            );

        }

        public static void RegisterSchedulerJobAttributes(this IServiceCollection serviceCollection)
        {

            serviceCollection.Scan(
                selector =>
                    selector.FromApplicationDependencies().AddClasses(
                            x => x.AssignableTo<ISchedulerJob>()
                        )
                        .AsSelfWithInterfaces()
                        .WithSingletonLifetime()
            );
        }


        public static void RegisterConfigurationAttributes(this IServiceCollection serviceCollection)
        {

            var configurationTypes = serviceCollection.Scan(
                selector =>
                    selector.FromApplicationDependencies().AddClasses(
                        x => x.WithAttribute<ConfigurationAttribute>()
                    )
            ).Select(s => s.ServiceType).ToList();


            foreach (var configurationType in configurationTypes)
            {
                var attribute = configurationType.GetCustomAttribute<ConfigurationAttribute>();
                if (attribute != null)
                {
                    serviceCollection.AddSingleton(configurationType, provider =>
                    {
                        var configuration = provider.GetService<IConfiguration>();

                        var instance = Activator.CreateInstance(configurationType);

                        configuration.Bind(attribute.Section, instance);

                        return instance;
                    });
                }

            }
        }

    }
    }
