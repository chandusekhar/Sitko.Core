using System.Globalization;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.SerilogEnricher;
using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.ElasticStack;

public class ElasticStackModule : BaseApplicationModule<ElasticStackModuleOptions>,
    IHostBuilderModule<ElasticStackModuleOptions>, ILoggingModule<ElasticStackModuleOptions>,
    IWebApplicationModule<ElasticStackModuleOptions>
{
    public override string OptionsKey => "ElasticApm";

    public void ConfigureHostBuilder(IApplicationContext context, IHostApplicationBuilder hostBuilder,
        ElasticStackModuleOptions startupOptions)
    {
        if (startupOptions.ApmEnabled)
        {
            Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", context.Name);
            Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_VERSION", context.Version);
            Environment.SetEnvironmentVariable("ELASTIC_APM_TRANSACTION_SAMPLE_RATE",
                startupOptions.ApmTransactionSampleRate.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:TransactionMaxSpans",
                startupOptions.ApmTransactionMaxSpans.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:CentralConfig",
                startupOptions.ApmCentralConfig.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames",
                startupOptions.ApmCentralConfig.ToString(CultureInfo.InvariantCulture));
            if (startupOptions.ApmSanitizeFieldNames != null && startupOptions.ApmSanitizeFieldNames.Count != 0)
            {
                Environment.SetEnvironmentVariable("ElasticApm:SanitizeFieldNames",
                    string.Join(", ", startupOptions.ApmSanitizeFieldNames));
            }

            if (startupOptions.ApmGlobalLabels.Count != 0)
            {
                Environment.SetEnvironmentVariable("ElasticApm:GlobalLabels",
                    string.Join(",", startupOptions.ApmGlobalLabels.Select(kv => $"{kv.Key}={kv.Value}")));
            }

            Environment.SetEnvironmentVariable("ElasticApm:ServerUrls",
                string.Join(",", startupOptions.ApmServerUrls));

            Environment.SetEnvironmentVariable("ElasticApm:SecretToken", startupOptions.ApmSecretToken);
            Environment.SetEnvironmentVariable("ElasticApm:ApiKey", startupOptions.ApmApiKey);
            Environment.SetEnvironmentVariable("ElasticApm:VerifyServerCert",
                startupOptions.ApmVerifyServerCert.ToString());
            Environment.SetEnvironmentVariable("ElasticApm:FlushInterval",
                $"{TimeSpan.FromSeconds(startupOptions.ApmFlushIntervalInSeconds).TotalSeconds}s");
            Environment.SetEnvironmentVariable("ElasticApm:MaxBatchEventCount",
                startupOptions.ApmMaxBatchEventCount.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:MaxQueueEventCount",
                startupOptions.ApmMaxQueueEventCount.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:MetricsInterval",
                $"{TimeSpan.FromSeconds(startupOptions.ApmMetricsIntervalInSeconds).TotalSeconds}s");
            if (startupOptions.ApmDisableMetrics != null && startupOptions.ApmDisableMetrics.Count != 0)
            {
                Environment.SetEnvironmentVariable("ElasticApm:DisableMetrics",
                    string.Join(",", startupOptions.ApmDisableMetrics));
            }

            Environment.SetEnvironmentVariable("ElasticApm:CaptureBody", startupOptions.ApmCaptureBody);
            if (startupOptions.ApmCaptureBodyContentTypes != null &&
                startupOptions.ApmCaptureBodyContentTypes.Count != 0)
            {
                Environment.SetEnvironmentVariable("ElasticApm:CaptureBodyContentTypes",
                    string.Join(",", startupOptions.ApmCaptureBodyContentTypes));
            }

            Environment.SetEnvironmentVariable("ElasticApm:CaptureHeaders",
                startupOptions.ApmCaptureHeaders.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:UseElasticTraceparentHeader",
                startupOptions.ApmUseElasticTraceparentHeader.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:StackTraceLimit",
                startupOptions.ApmStackTraceLimit.ToString(CultureInfo.InvariantCulture));
            Environment.SetEnvironmentVariable("ElasticApm:SpanFramesMinDuration",
                $"{TimeSpan.FromSeconds(startupOptions.ApmSpanFramesMinDurationInSeconds).TotalMilliseconds}ms");
            Environment.SetEnvironmentVariable("ElasticApm:ApmLogLevel", startupOptions.ApmLogLevel);
        }
    }

    public LoggerConfiguration ConfigureLogging(IApplicationContext context, ElasticStackModuleOptions options,
        LoggerConfiguration loggerConfiguration)
    {
        if (options.LoggingEnabled)
        {
            var sinkOptions = new ElasticsearchSinkOptions(options.ElasticSearchUrls)
            {
                CustomFormatter = new EcsTextFormatter(),
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = options.LoggingTemplateVersion ?? AutoRegisterTemplateVersion.ESv7,
                NumberOfReplicas = options.LoggingNumberOfReplicas,
                IndexFormat =
                    options.LoggingIndexFormat ??
                    $"dotnet-logs-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                EmitEventFailure = options.EmitEventFailure,
                FailureCallback = options.FailureCallback,
                FailureSink = options.FailureSink,
                TypeName = options.LogIndexTypeName
            };
            if (!string.IsNullOrEmpty(options.LoggingLifeCycleName))
            {
                sinkOptions.TemplateCustomSettings = new Dictionary<string, string>
                {
                    { "lifecycle.name", options.LoggingLifeCycleName }
                };
            }

            if (!string.IsNullOrEmpty(options.LoggingRolloverAlias))
            {
                sinkOptions.TemplateName = options.LoggingRolloverAlias;
                sinkOptions.IndexAliases = new[] { options.LoggingRolloverAlias };
                sinkOptions.TemplateCustomSettings["lifecycle.rollover_alias"] = options.LoggingRolloverAlias;
            }

            loggerConfiguration = loggerConfiguration
                .Enrich.WithElasticApmCorrelationInfo()
                .WriteTo.Elasticsearch(sinkOptions)
                // meta for EcsTextFormatter
                .Enrich.WithProperty("ApplicationId", context.Id)
                .Enrich.WithProperty("ApplicationName", context.Name)
                .Enrich.WithProperty("ApplicationVersion", context.Version);
        }

        if (options.ApmEnabled)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override("Elastic.Apm", LogEventLevel.Error);
        }

        return loggerConfiguration;
    }

    public void ConfigureEndpoints(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder, IEndpointRouteBuilder endpoints)
    {
        var options = GetOptions(appBuilder.ApplicationServices);
        if (options.ApmEnabled)
        {
            appBuilder.UseAllElasticApm();
        }
    }
}
