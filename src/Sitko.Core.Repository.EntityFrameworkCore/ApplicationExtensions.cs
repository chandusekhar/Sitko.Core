﻿using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Repository.EntityFrameworkCore;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddEFRepositories(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddEFRepositories(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddEFRepositories(this IHostApplicationBuilder hostApplicationBuilder,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddEFRepositories(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddEFRepositories<TAssembly>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddEFRepositories<TAssembly>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddEFRepositories<TAssembly>(
        this IHostApplicationBuilder hostApplicationBuilder,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddEFRepositories<TAssembly>(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddEFRepositories(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddEFRepositories(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddEFRepositories<TAssembly>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, EFRepositoriesModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(
            (applicationContext, moduleOptions) =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure(applicationContext, moduleOptions);
            },
            optionsKey);

    public static SitkoCoreApplicationBuilder AddEFRepositories<TAssembly>(
        this SitkoCoreApplicationBuilder applicationBuilder,
        Action<EFRepositoriesModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<EFRepositoriesModule, EFRepositoriesModuleOptions>(moduleOptions =>
            {
                moduleOptions.AddRepositoriesFromAssemblyOf<TAssembly>();
                configure?.Invoke(moduleOptions);
            },
            optionsKey);
}
