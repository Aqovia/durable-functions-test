using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace DurableFunctions.Test
{
    public static class DurableFuncV2Helper
    {
        public const string VersionSuffix = "V2";
        public const string TestCategory = "Functions" + VersionSuffix;

        #region Private classes (support classes)

        private class ExplicitTypeLocator : ITypeLocator
        {
            private readonly IReadOnlyList<Type> types;

            public ExplicitTypeLocator(params Type[] types)
            {
                this.types = types.ToList().AsReadOnly();
            }

            public IReadOnlyList<Type> GetTypes()
            {
                return this.types;
            }
        }

        private class TestNameResolver : INameResolver
        {
            private static readonly Dictionary<string, string> DefaultAppSettings = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                { "TestTaskHub", string.Empty },
            };

            private readonly INameResolver innerResolver;

            public TestNameResolver(INameResolver innerResolver)
            {
                // null is okay
                this.innerResolver = innerResolver;
            }

            public string Resolve(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                string value = this.innerResolver?.Resolve(name);
                if (value == null)
                {
                    DefaultAppSettings.TryGetValue(name, out value);
                }

                if (value == null)
                {
                    value = Environment.GetEnvironmentVariable(name);
                }

                return value;
            }
        }

        #endregion

        #region Private methods (support methods)

        private static ITypeLocator GetTypeLocator(Type[] functionTypes)
        {
            ITypeLocator typeLocator = new ExplicitTypeLocator(functionTypes);
            return typeLocator;
        }

        private static INameResolver GetTestNameResolver()
        {
            return new TestNameResolver(null);
        }

        // Create a valid task hub from the test name, and add a random suffix to avoid conflicts
        private static string GetTaskHubNameFromTestName(string testName /*, bool enableExtendedSessions*/)
        {
            string strippedTestName = testName.Replace("_", "");
            string truncatedTestName = strippedTestName.Substring(0, Math.Min(35, strippedTestName.Length));
            string testPropertiesSuffix = /*(enableExtendedSessions ? "EX" : "") +*/ DurableFuncV2Helper.VersionSuffix;
            string randomSuffix = Guid.NewGuid().ToString().Substring(0, 4);
            return truncatedTestName + testPropertiesSuffix + randomSuffix;
        }

        private static IOptions<DurableTaskOptions> GetDurableTaskOptions()
        {
            var options = new DurableTaskOptions();

            options.HubName = GetTaskHubNameFromTestName("TEST_HUB");

            options.Tracing.TraceInputsAndOutputs = true;
            options.Tracing.TraceReplayEvents = true;
            options.Tracing.AllowVerboseLinuxTelemetry = false;

            options.Notifications = new NotificationOptions()
            {
                EventGrid = new EventGridNotificationOptions()
                {
                    KeySettingName = null,
                    TopicEndpoint = null,
                    PublishEventTypes = null,
                },
            };
            options.HttpSettings = new HttpOptions()
            {
                DefaultAsyncRequestSleepTimeMilliseconds = 500,
            };

            options.ExtendedSessionsEnabled = false;
            options.MaxConcurrentOrchestratorFunctions = 200;
            options.MaxConcurrentActivityFunctions = 200;

            options.LocalRpcEndpointEnabled = false;
            options.RollbackEntityOperationsOnExceptions = true;
            options.EntityMessageReorderWindowInMinutes = 30;

            return new OptionsWrapper<DurableTaskOptions>(options);
        }

        #endregion

        public static IHost CreateJobHost(
            ILoggerProvider loggerProvider,
            ServiceCollection serviceCollectionEx,
            Type[] functionTypes)
        {
            Environment.SetEnvironmentVariable("AzureWebJobsStorage", "UseDevelopmentStorage=true");

            var options = GetDurableTaskOptions();

            // Unless otherwise specified, use legacy partition management for tests as it makes the task hubs start up faster.
            // These tests run on a single task hub workers, so they don't test partition management anyways, and that is tested
            // in the DTFx repo.
            if (!options.Value.StorageProvider.ContainsKey(nameof(AzureStorageOptions.UseLegacyPartitionManagement)))
            {
                options.Value.StorageProvider.Add(nameof(AzureStorageOptions.UseLegacyPartitionManagement), true);
            }

            IHost host = new HostBuilder()
                .ConfigureLogging(
                    loggingBuilder =>
                    {
                        loggingBuilder.AddProvider(loggerProvider);
                    })
                .ConfigureWebJobs(
                    webJobsBuilder =>
                    {
                        webJobsBuilder.Services.AddSingleton<IDurabilityProviderFactory, EmulatorDurabilityProviderFactory>();
                        webJobsBuilder.AddDurableTask(options);
                        webJobsBuilder.AddAzureStorage();
                    })
                .ConfigureServices(
                    serviceCollection =>
                    {
                        ITypeLocator typeLocator = GetTypeLocator(functionTypes);
                        serviceCollection.AddSingleton(typeLocator);

                        INameResolver nameResolver = GetTestNameResolver();
                        serviceCollection.AddSingleton(nameResolver);

                        IDurableHttpMessageHandlerFactory durableHttpMessageHandlerFactory = new DurableHttpMessageHandlerFactory();
                        serviceCollection.AddSingleton(durableHttpMessageHandlerFactory);

                        IMessageSerializerSettingsFactory messageSerializerSettingsFactory = new TestMessageSerializerSettingsFactory();
                        serviceCollection.AddSingleton(messageSerializerSettingsFactory);

                        //Add the externally created service collection to the host service collection
                        foreach(var sd in serviceCollectionEx)
                        {
                            serviceCollection.Add(sd);
                        }
                    })
                .Build();

            return host;
        }
    }
}
