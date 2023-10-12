using Microsoft.Extensions.Hosting;
using Sitko.Core.Storage.S3;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Metadata.Postgres.Tests;

public class BasePostgresStorageTestScope : BaseTestScope
{
    protected override IHostApplicationBuilder ConfigureApplication(IHostApplicationBuilder hostBuilder, string name) =>
        base.ConfigureApplication(hostBuilder, name)
            .AddS3Storage<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Bucket = name.ToLowerInvariant();
                moduleOptions.Prefix = "test";
            })
            .AddPostgresStorageMetadata<TestS3StorageSettings>(moduleOptions =>
            {
                moduleOptions.Database = name;
            });

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        var storage = GetService<IStorage<TestS3StorageSettings>>();
        await storage.DeleteAllAsync();
    }
}
