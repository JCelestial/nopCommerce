﻿using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Factories;
using Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Services;

namespace Nop.Plugin.MultiFactorAuth.GoogleAuthenticator.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //register custom services
            builder.RegisterType<GoogleAuthenticatorService>().AsSelf().InstancePerLifetimeScope();

            //register custom factories
            builder.RegisterType<AuthorizationModelFactory>().AsSelf().InstancePerLifetimeScope();
        }

        public int Order => 1;
    }
}
