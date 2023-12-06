﻿using FluentValidation;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;

namespace Sitko.Core.Configuration.Vault;

public class VaultConfigurationModule : BaseApplicationModule<VaultConfigurationModuleOptions>,
    IConfigurationModule<VaultConfigurationModuleOptions>
{
    public override string OptionsKey => "Vault";

    public void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder,
        VaultConfigurationModuleOptions startupOptions)
    {
        foreach (var secret in startupOptions.Secrets)
        {
            configurationBuilder.Add(new VaultConfigurationSource(startupOptions, secret));
        }
    }

    public void CheckConfiguration(IApplicationContext context, IServiceProvider serviceProvider)
    {
        var root = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var providers = root.Providers.OfType<VaultConfigurationProvider>().ToList();
        if (!providers.Any())
        {
            throw new InvalidOperationException("No Vault providers on configuration");
        }

        var emptySecrets = new List<string>();
        foreach (var provider in providers)
        {
            var keys = provider.GetChildKeys(Array.Empty<string>(), null).ToArray();
            serviceProvider.GetRequiredService<ILogger<VaultConfigurationModule>>()
                .LogInformation("Loaded {KeysCount} keys from secret {Secret}", keys.Length, provider.ConfigurationSource.BasePath);
            if (!keys.Any())
            {
                emptySecrets.Add(provider.ConfigurationSource.BasePath);
            }
        }

        var options = serviceProvider.GetRequiredService<IOptions<VaultConfigurationModuleOptions>>();
        if (emptySecrets.Any() && options.Value.ThrowOnEmptySecrets)
        {
            var names = string.Join(", ", emptySecrets);
            throw new OptionsValidationException(names, GetType(),
                new[] { $"No data loaded from Vault secrets {names}" });
        }
    }

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        VaultConfigurationModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        if (startupOptions.ReloadOnChange)
        {
            services.TryAddSingleton((IConfigurationRoot)applicationContext.Configuration);
            services.AddHostedService<VaultChangeWatcher>();
        }

        if (startupOptions is { AuthType: VaultAuthType.Token, RenewToken: true })
        {
            services.AddHostedService<VaultTokenRenewService>();
        }
    }
}

public class VaultConfigurationModuleOptions : BaseModuleOptions
{
    public List<string> Secrets { get; set; } = new();
    public string Uri { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string MountPoint { get; set; } = "secret";
    public string? VaultSecret { get; [UsedImplicitly] set; }
    public string? VaultRoleId { get; [UsedImplicitly] set; }
    public bool ReloadOnChange { get; set; } = true;
    public int ReloadCheckIntervalSeconds { get; set; } = 60;
    public bool OmitVaultKeyName { get; [UsedImplicitly] set; }
    public bool RenewToken { get; set; } = true;
    public int TokenRenewIntervalMinutes { get; set; } = 60;
    public bool ThrowOnEmptySecrets { get; set; } = true;
    public VaultAuthType AuthType { get; set; } = VaultAuthType.Token;

    public IEnumerable<char>? AdditionalCharactersForConfigurationPath { get; [UsedImplicitly] set; }

    public override void Configure(IApplicationContext applicationContext)
    {
        if (!Secrets.Any())
        {
            Secrets.Add(applicationContext.Name);
        }
    }
}

public enum VaultAuthType
{
    Token,
    RoleApp
}

public class VaultConfigurationOptionsValidator : AbstractValidator<VaultConfigurationModuleOptions>
{
    public VaultConfigurationOptionsValidator()
    {
        RuleFor(o => o.Uri).NotEmpty().WithMessage("Vault url is empty");
        RuleFor(o => o.Token).NotEmpty().When(o => o.AuthType == VaultAuthType.Token)
            .WithMessage("Vault token is empty");
        RuleFor(o => o.VaultRoleId).NotEmpty().When(o => o.AuthType == VaultAuthType.RoleApp)
            .WithMessage("Vault role id is empty");
        RuleFor(o => o.VaultSecret).NotEmpty().When(o => o.AuthType == VaultAuthType.RoleApp)
            .WithMessage("Vault secret is empty");
        RuleFor(o => o.MountPoint).NotEmpty().WithMessage("Vault mount point is empty");
        RuleFor(o => o.Secrets).NotEmpty().WithMessage("Vault secrets list is empty");
    }
}
