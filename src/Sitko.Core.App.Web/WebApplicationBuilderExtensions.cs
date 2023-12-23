﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sitko.Core.App.Web;

public static class WebApplicationBuilderExtensions
{
    public static ISitkoCoreWebApplicationBuilder AddSitkoCoreWeb(this WebApplicationBuilder builder) =>
        builder.AddSitkoCoreWeb(Array.Empty<string>());

    public static ISitkoCoreWebApplicationBuilder AddSitkoCoreWeb(this WebApplicationBuilder builder, string[] args)
    {
        builder.Services.TryAddTransient<IStartupFilter, SitkoCoreWebStartupFilter>();
        return ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreWebApplicationBuilder(applicationBuilder, args));
    }

    public static TBuilder ConfigureWeb<TBuilder>(this TBuilder builder, Action<SitkoCoreWebOptions> configure)
        where TBuilder : ISitkoCoreWebApplicationBuilder
    {
        configure(builder.WebOptions);
        return builder;
    }

    public static WebApplication MapSitkoCore(this WebApplication webApplication)
    {
        var applicationContext = webApplication.Services.GetRequiredService<IApplicationContext>();
        var applicationModuleRegistrations = webApplication.Services.GetServices<ApplicationModuleRegistration>();
        var webOptions = webApplication.Services.GetRequiredService<SitkoCoreWebOptions>();

        if (webOptions.AllowAllForwardedHeaders)
        {
            webApplication.UseForwardedHeaders();
        }

        if (applicationContext.IsDevelopment())
        {
            webApplication.UseDeveloperExceptionPage();
        }
        else
        {
            webApplication.UseExceptionHandler("/Error");
        }

        if (webOptions.EnableSameSiteCookiePolicy)
        {
            webApplication.UseCookiePolicy();
        }

        if (webOptions.EnableStaticFiles)
        {
            webApplication.UseStaticFiles();
        }

        webApplication.UseAntiforgery();

        if (webOptions.CorsPolicies.Count != 0)
        {
            var defaultPolicy = webOptions.CorsPolicies.Where(item => item.Value.isDefault).Select(item => item.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultPolicy))
            {
                webApplication.UseCors(defaultPolicy);
            }
        }

        if (webOptions.EnableMvc)
        {
            webApplication.MapControllers();
        }

        var webModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations)
                .Select(r => r.GetInstance())
                .OfType<IWebApplicationModule>()
                .ToList();
        foreach (var webModule in webModules)
        {
            webModule.ConfigureBeforeUseRouting(applicationContext, webApplication);
            webModule.ConfigureAfterUseRouting(applicationContext, webApplication);
            webModule.ConfigureAuthMiddleware(applicationContext, webApplication);
            webModule.ConfigureEndpoints(applicationContext, webApplication, webApplication);
        }

        return webApplication;
    }
}
