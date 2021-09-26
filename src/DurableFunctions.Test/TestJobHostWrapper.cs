using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DurableTask.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;


namespace DurableFunctions.Test
{
    public class TestJobHostWrapper : IDisposable
    {
        public ServiceCollection ServiceCollection { get; set; }

        private IHost _innerHost;        
        private Type[] _functionTypes;
        private TestLoggerProvider _loggerProvider;

        private IDurabilityProviderFactory _durabilityProviderFactory;
        private DurabilityProvider _durabilityProvider;
        private JobHost _jobHost;
        private OrchestrationState _orchestrationState;
        private bool disposedValue;

        public TestJobHostWrapper(ITestOutputHelper testOutputHelper,
            Type[] functionTypes, ServiceCollection serviceCollection = null)
        {
            _loggerProvider = new TestLoggerProvider(testOutputHelper);
            _functionTypes = functionTypes;

            ServiceCollection = serviceCollection;
        }
       
        public T GetService<T>()
        {
            return _innerHost.Services.GetRequiredService<T>();
        }

        public async Task StopAsync()
        {
            try
            {
                await _innerHost.StopAsync();                
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            finally
            {
                _innerHost.Dispose();
            }            
        }

        public async Task StartAsync()
        {
            if(ServiceCollection == null || !ServiceCollection.Any())
            {
                throw new Exception("No services are configured for test job host");
            }

            if (_innerHost == null)
            {
                _innerHost = DurableFuncV2Helper.CreateJobHost(
                    _loggerProvider,
                    ServiceCollection,
                    _functionTypes
                );

                await _innerHost.StartAsync();

                _jobHost = (JobHost)_innerHost.Services.GetService<IJobHost>();
                _durabilityProviderFactory = _innerHost.Services.GetService<IDurabilityProviderFactory>();
                _durabilityProvider = _durabilityProviderFactory.GetDurabilityProvider();
            }
        }

        public async Task CallAsync(string instanceId, MethodInfo methodUnderTest, object arguments = default)
        {
            //if the host is not initialised start it up at this point
            if(_innerHost == null)
            {
                await StartAsync();
            }

            await _jobHost.CallAsync(methodUnderTest, arguments);

        }

        public async Task WaitForOrchestrationAsync(string instanceId)
        {
            var timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(30);

            _orchestrationState = (await _durabilityProvider.GetOrchestrationStateAsync(instanceId, allExecutions: false)).First();
            _orchestrationState = await _durabilityProvider.WaitForOrchestrationAsync(instanceId, _orchestrationState.OrchestrationInstance.ExecutionId, timeout, CancellationToken.None);
        }

        public async Task<(OrchestrationState, string)> GetLastOrchestrationStateWithHistoryAsync()
        {
            if(_orchestrationState == null)
            {
                throw new Exception("Orchestration state is null");
            }

            var history = await _durabilityProvider.GetOrchestrationHistoryAsync(_orchestrationState.OrchestrationInstance.InstanceId, _orchestrationState.OrchestrationInstance.ExecutionId);
            return (_orchestrationState, history);
        }
        

        public TestLogger GetLoggerByCategoryName(string categoryName)
        {
            return _loggerProvider.CreatedLoggers.SingleOrDefault(l => l.Category == categoryName);
        }

        public TestLogger GetLoggerByFunctionName(string functionName)
        {
            var categoryName = $"Function.{functionName}.User";
            return _loggerProvider.CreatedLoggers.SingleOrDefault(l => l.Category == categoryName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopAsync().Wait();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
