using DurableTask.Core;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SampleFunctionApp;
using SampleFunctionApp.Models;
using SampleFunctionApp.Services;
using System;
using System.Linq;
using Xunit.Abstractions;
using Xbehave;

namespace Aqovia.DurableFunctions.Testing.EndToEndTests
{
    public class SampleFunctionAppEndToEndTests
    {
        private readonly ITestOutputHelper _output;

        public SampleFunctionAppEndToEndTests(ITestOutputHelper output) 
        {
            _output = output;
        }

        [Scenario]
        public void HttpTriggerFunctionTest()
        {
            HttpRequest httpRequest = null;
            TestJobHostWrapper host = null;
            var instanceId = "1000";

            "GIVEN a http request is ready for processing".x(() =>
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

            "WHEN the HttpTriggerFunction is invoked it completed successfully".x(async () =>
            {
                //get the method info of the function-under-test
                var fut = typeof(SampleFunctions).GetMethod("HttpTriggerFunction");

                //pass the fut to the jobs host for execution
                await host.CallAsync(fut, new { req=httpRequest }).ConfigureAwait(false);
                await host.WaitForOrchestrationAsync(instanceId).ConfigureAwait(false);
            });

            "THEN the internal orchestration should complete successfully".x(async () =>
            {
                var (orchestrationState, _) = await host.GetOrchestrationStateWithHistoryAsync(instanceId);
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

            "AND the log messages should be as expected".x(() =>
            {
                var logger = host.GetLoggerByCategoryName("SampleFunctionApp.SampleFunctions");

                logger.Should().NotBeNull();

                var expectedLogMessages = new[] {
                    ( LogLevel.Information, "C# HTTP trigger function 'HttpTriggerFunction' processed a request." ),
                    ( LogLevel.Information, $"Started orchestration with ID = '{instanceId}'.")
                };

                var actualLogMessages = logger.LogMessages.Select(m => (m.Level, m.FormattedMessage)).ToArray();
                actualLogMessages.Should().BeEquivalentTo(expectedLogMessages, options => options.WithStrictOrdering());
            });
        }

        [Scenario]
        public void ServiceBusTriggerFunctionTest()
        {
            Message serviceBusMessage = null;
            TestJobHostWrapper host = null;
            var instanceId = "1000";
         
            "GIVEN a service bus message is ready for processing".x(() =>
            {
                serviceBusMessage = ServiceBusMessageHelper.CreateNewMessage(instanceId, new TestServiceBusModel { Data="hello" });
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

            "WHEN the ServiceBusTriggerFunction is invoked it completed successfully".x(async () =>
            {
                //get the method info of the function-under-test
                var fut = typeof(SampleFunctions).GetMethod("ServiceBusTriggerFunction");

                //pass the fut to the jobs host for execution
                await host.CallAsync(fut, new { message = serviceBusMessage }).ConfigureAwait(false);
                await host.WaitForOrchestrationAsync(instanceId).ConfigureAwait(false);
            });

            "THEN the internal orchestration should complete successfully".x(async () =>
            {
                var (orchestrationState, _) = await host.GetOrchestrationStateWithHistoryAsync(instanceId);
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

            "AND the log messages should be as expected".x(() =>
            {
                var logger = host.GetLoggerByCategoryName("SampleFunctionApp.SampleFunctions");

                logger.Should().NotBeNull();

                var expectedLogMessages = new[] {
                    ( LogLevel.Information, "C# Service Bus trigger function processed a message." ),
                    ( LogLevel.Information, $"Started orchestration with ID = '{instanceId}'.")
                };

                var actualLogMessages = logger.LogMessages.Select(m => (m.Level, m.FormattedMessage)).ToArray();
                actualLogMessages.Should().BeEquivalentTo(expectedLogMessages, options => options.WithStrictOrdering());
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
                    typeof(SampleFunctions),
                    typeof(ActivityFunctions)
            };

            return new TestJobHostWrapper(output, functionTypes, serviceCollection);
        }
    }
}
