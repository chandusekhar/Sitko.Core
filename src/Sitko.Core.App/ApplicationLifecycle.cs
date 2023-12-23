using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Sitko.Core.App;

public class ApplicationLifecycle(
    IApplicationContext context,
    IServiceProvider provider,
    IEnumerable<ApplicationModuleRegistration> applicationModuleRegistrations,
    ILogger<ApplicationLifecycle> logger)
    : IApplicationLifecycle
{
    private readonly IReadOnlyList<ApplicationModuleRegistration> enabledModules =
        ModulesHelper.GetEnabledModuleRegistrations(context, applicationModuleRegistrations);

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = provider.CreateAsyncScope();

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue = await enabledModule.GetInstance()
                .OnBeforeRunAsync(context, scope.ServiceProvider);
            if (!shouldContinue)
            {
                Environment.Exit(0);
                return;
            }
        }

        logger.LogInformation("Check required modules");
        var modulesCheckSuccess = true;
        foreach (var registration in enabledModules)
        {
            var result =
                registration.CheckRequiredModules(context,
                    enabledModules.Select(r => r.Type).ToArray());
            if (!result.isSuccess)
            {
                foreach (var missingModule in result.missingModules)
                {
                    Log.Information("Required module {MissingModule} for module {Type} is not registered",
                        missingModule, registration.Type);
                }

                modulesCheckSuccess = false;
            }
        }

        if (!modulesCheckSuccess)
        {
            throw new InvalidOperationException("Check required modules failed");
        }

        logger.LogInformation("Init modules");

        foreach (var configurationModule in enabledModules.Select(module => module.GetInstance())
                     .OfType<IConfigurationModule>())
        {
            configurationModule.CheckConfiguration(context, scope.ServiceProvider);
        }

        foreach (var registration in enabledModules)
        {
            logger.LogInformation("Init module {Module}", registration.Type);
            await registration.InitAsync(context, scope.ServiceProvider);
        }

        foreach (var enabledModule in enabledModules)
        {
            var shouldContinue =
                await enabledModule.GetInstance().OnAfterRunAsync(context, scope.ServiceProvider);
            if (!shouldContinue)
            {
                Environment.Exit(0);
            }
        }
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStarted(context, provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application started hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStopping(context, provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopping hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        foreach (var moduleRegistration in enabledModules)
        {
            try
            {
                await moduleRegistration.ApplicationStopped(context, provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on application stopped hook in module {Module}: {ErrorText}",
                    moduleRegistration.Type,
                    ex.ToString());
            }
        }
    }
}
