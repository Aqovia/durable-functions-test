using DurableTask.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SampleFunctionApp;
using SampleFunctionApp.Services;
using System;
using Xunit.Abstractions;
using Xbehave;

namespace DurableFunctions.Test.EndToEndTests
{
    public class SampleFunctionAppEndToEndTests
    {
        private readonly ITestOutputHelper _output;

        public SampleFunctionAppEndToEndTests(ITestOutputHelper output) 
        {
            _output = output;
        }

        [Scenario]
        public void SampleFunctionTest()
        {
            HttpRequest httpRequest = null;
            TestJobHostWrapper host = null;
            var instanceId = "1000";

            "GIVEN a request is sent to the SampleFunction http endpoint".x(() =>
            {
                var fakeUri = string.Format("https://fakehost.com/api/SampleHttpFunction?id={0}&data=hello", instanceId);
                httpRequest = HttpTestHelper.CreateHttpRequest("get", fakeUri);
            });

            "AND a job host to perform function execution".x(async () =>
            {
                host = CreateTestSampleAppHost(_output);
                await host.StartAsync().ConfigureAwait(false);
            })
            .Teardown(async () =>
            {
                await host.StopAsync().ConfigureAwait(false);
            });

            "WHEN the SampleFunction is completed successfully".x(async () =>
            {
                //get the method info of the function-under-test
                var fut = typeof(SampleHttpFunction).GetMethod("SampleFunction");

                //pass the fut to the jobs host for execution
                await host.CallAsync(instanceId, fut, new { req=httpRequest }).ConfigureAwait(false);
                await host.WaitForOrchestrationAsync(instanceId).ConfigureAwait(false);
            });

            "THEN the internal orchestration should complete successfully".x(async () =>
            {
                var (orchestrationState, _) = await host.GetLastOrchestrationStateWithHistoryAsync();
                orchestrationState.Should().NotBeNull();
                orchestrationState.OrchestrationStatus.Should().Be(OrchestrationStatus.Completed);
                orchestrationState.Name.Should().Be("SampleOrchestration");
            });

            "AND an message should be published".x(() =>
            {
                var messageQueueService = (FakeMessageQueueService)host.GetService<IMessageQueueService>();
                var messages = messageQueueService.Messages;     
                
                messages.Count.Should().Be(1);
                messages[0].Should().Be("hello");
            });
        }

        private TestJobHostWrapper CreateTestSampleAppHost(ITestOutputHelper output)
        {
            //DI bindings for singleton/transient services of the function app
            var serviceCollection = new ServiceCollection
            {
                //create a singleton for the output and caching of messages
                new ServiceDescriptor(typeof(IMessageQueueService), new FakeMessageQueueService())
            };

            //types that contain the required trigger/activity functions
            var functionTypes = new Type[]
            {
                    typeof(SampleHttpFunction),
                    typeof(ActivityFunctions)
            };

            return new TestJobHostWrapper(output, functionTypes, serviceCollection);
        }
    }
}
