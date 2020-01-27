using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Queue.Tests;
using Sitko.Core.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Sitko.Core.Queue.Nats.Tests
{
    public class OptionsTest : BaseTest<NatsQueueTestScopeWithOptions>
    {
        public OptionsTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task CheckOptions()
        {
            var scope = GetScope();

            var queue = scope.Get<IQueue>();
            var messageOptions = scope.Get<IEnumerable<IQueueMessageOptions>>();
            Assert.NotNull(messageOptions);
            Assert.NotEmpty(messageOptions);

            var testMessageOptions = messageOptions.FirstOrDefault(o => o is IQueueMessageOptions<TestMessage>);
            Assert.NotNull(testMessageOptions);

            var subResult = await queue.SubscribeAsync<TestMessage>((message, context) => Task.FromResult(true));
            Assert.True(subResult.IsSuccess);

            Assert.NotNull(subResult.Options);

            Assert.Equal(testMessageOptions, subResult.Options);
        }
    }

    public class NatsQueueTestScopeWithOptions : NatsQueueTestScope
    {
        protected override void ConfigureQueue(NatsQueueModuleConfig config, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureQueue(config, configuration, environment);
            config.ConfigureMessage(new NatsMessageOptions<TestMessage>
            {
                StartAt = TimeSpan.FromMinutes(30), ManualAck = true
            });
        }
    }
}
