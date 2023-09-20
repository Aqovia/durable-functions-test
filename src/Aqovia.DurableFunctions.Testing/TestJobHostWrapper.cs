using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DurableTask.Core;
using DurableTask.Core.Query;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Aqovia.DurableFunctions.Testing
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
        private bool disposedValue;
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="testOutputHelper">Pass in this test output helper (XBehave) class - this utilises the test output helper of the test suite</param>
        /// <param name="functionTypes">Classes that contain the trigger/orchstestration/activity functions</param>
        /// <param name="serviceCollection">
        /// An externally defined list of service bindings which must be specified for the internal construction of the job host
        /// This list should reflect the bindings that the real function app has configured in Startup.cs:ConfigureServices
        /// Any scoped services can be defined as transient for the purposes of the testing
        /// </param>
        public TestJobHostWrapper(ITestOutputHelper testOutputHelper,
            Type[] functionTypes, ServiceCollection serviceCollection = null)
        {
            _loggerProvider = new TestLoggerProvider(testOutputHelper);
            _functionTypes = functionTypes;

            ServiceCollection = serviceCollection;
        }

       /// <summary>
       /// GetService
       /// Extract a service from the internal DI container
       /// Provides functionality to retrieve a fake service and interogate any results that have been cached
       /// </summary>
       /// <typeparam name="T"></typeparam>
       /// <returns>The required service</returns>
        public T GetService<T>()
        {
            return _innerHost.Services.GetRequiredService<T>();
        }

        /// <summary>
        /// Stop Async
        /// Stop the internal job host and dispose of it
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// StartAsync
        /// Start the internal job host after first checking service bindings have been provided
        /// Cache references to the durability provider factory and durability provider for later inspection
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// CallAsync
        /// When invoked passes a method info object to the job host to perform internal execution
        /// Methods passed in should be trigger functions that invoke internal orchestrations
        /// </summary>
        /// <param name="methodUnderTest">A reflective method info object with the details of the function to invoke</param>
        /// <param name="arguments">A dictionary of arguments to pass to the function when bindings are resolved
        /// IMPORTANT: members of this dictionary should match the argument names of the function you are attempting to call
        /// ie. if your function has arguments ie (Message message, int count) then this param would be
        /// new { message=new Message(), count=10 }
        /// </param>
        /// <returns></returns>
        public async Task CallAsync(MethodInfo methodUnderTest, object arguments = default)
        {
            //if the host is not initialised start it up at this point
            if(_innerHost == null)
            {
                await StartAsync();
            }

            await _jobHost.CallAsync(methodUnderTest, arguments);

        }

        /// <summary>
        /// WaitForOrchestrationAsync
        /// Finds a previously invoked orchestration by instanceid - and then blocks waiting for that orchestration to complete
        /// Used in the test code to wait for orchestration completion
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public async Task WaitForOrchestrationAsync(string instanceId)
        {
            var timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(30);

            var orchestrationState = (await _durabilityProvider.GetOrchestrationStateAsync(instanceId, allExecutions: false)).First();
            await _durabilityProvider.WaitForOrchestrationAsync(instanceId, orchestrationState.OrchestrationInstance.ExecutionId, timeout, CancellationToken.None);
        }

        /// <summary>
        /// GetOrchestrationStateWithHistoryAsync
        /// Extracts the required orchestration state and the history of the orchestration by instance Id
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns>A tuple of required orchestration state and the history of the orchestration</returns>
        public async Task<(OrchestrationState, string)> GetOrchestrationStateWithHistoryAsync(string instanceId)
        {
            var orchestrationState = (await _durabilityProvider.GetOrchestrationStateAsync(instanceId, allExecutions: false)).First();
            if (orchestrationState == null)
                return default;


            var history = await _durabilityProvider.GetOrchestrationHistoryAsync(orchestrationState.OrchestrationInstance.InstanceId, orchestrationState.OrchestrationInstance.ExecutionId);
            return (orchestrationState, history);
        }

        /// <summary>
        /// GetLoggerByCategoryName
        /// Retrieves a logger from the loggerProvider using a categrory name search
        /// This can be used to interrogate the logging of each function that has registered a logger with the loggerProvider
        /// </summary>
        /// <param name="categoryName">Search string to locate the required logger</param>
        /// <returns>A test logger</returns>
        public TestLogger GetLoggerByCategoryName(string categoryName)
        {
            return _loggerProvider.CreatedLoggers.SingleOrDefault(l => l.Category == categoryName);
        }

        /// <summary>
        /// Fetches a read-only list of OrchestrationState objects, optionally filtered by the provided query parameter.
        /// This method is especially useful when the instance ID(s) of the target orchestration(s) are unknown.
        /// Omitting the query argument will fetch all OrchestrationStates
        /// </summary>
        /// <param name="query">An optional parameter of type OrchestrationQuery to filter the results.</param>
        /// <returns>Returns a read-only collection containing OrchestrationState objects.</returns>
        public async Task<IReadOnlyCollection<OrchestrationState>> GetOrchestrationStatesAsync(OrchestrationQuery query = null)
        {
            return (await ((IOrchestrationServiceQueryClient)_durabilityProvider).GetOrchestrationWithQueryAsync(
                query ?? new OrchestrationQuery(),
                CancellationToken.None)).OrchestrationState;
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

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
