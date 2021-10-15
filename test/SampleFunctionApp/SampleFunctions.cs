using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using SampleFunctionApp.Models;

namespace SampleFunctionApp
{
    public class SampleFunctions
    {
        private readonly ILogger<SampleFunctions> _logger;

        public SampleFunctions(ILogger<SampleFunctions> logger)
        {
            _logger = logger;
        }

        [FunctionName("SampleOrchestration")]
        public async Task SampleOrchestration([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var testString = context.GetInput<string>();
            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(30), 5);

            try
            {
                await context.CallActivityWithRetryAsync("SampleActivityFunction", retryOptions, testString);
            }
            catch(Exception ex)
            {            
                string exMsg = (ex is AggregateException)
                    ? string.Join("\n", (ex as AggregateException).InnerExceptions.Select(_ => _.Message))
                    : ex.Message;

                _logger.LogError($"CallActivityWithRetryAsync('SampleActivityFunction') failed:{exMsg}");
            }
        }

        [FunctionName("HttpTriggerFunction")]
        public async Task<IActionResult> HttpTriggerFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, [DurableClient] IDurableOrchestrationClient starter)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string data = req.Query["data"];
            string id = req.Query["id"];

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(data))
                return new BadRequestObjectResult("id and data not present in query string params");

            var status = await starter.GetStatusAsync(id, default, default, default);
            if (status == null)
            {
                // start orchestrator
                await starter.StartNewAsync("SampleOrchestration", id, data);
                _logger.LogInformation($"Started orchestration with ID = '{id}'.");
            }

            return new OkObjectResult("Request processed");
        }

        [FunctionName("ServiceBusTriggerFunction")]
        public async Task ServiceBusTriggerFunction([ServiceBusTrigger("TOPIC_NAME","SUBSCRIPTION_NAME")] Message message, 
            [DurableClient] IDurableOrchestrationClient starter)
        {
            _logger.LogInformation("C# Service Bus trigger function processed a request.");

            var instanceId = message.MessageId;
            var input = JsonConvert.DeserializeObject<TestServiceBusModel>(Encoding.UTF8.GetString(message.Body));
            string data = input.Data;
               
            var status = await starter.GetStatusAsync(instanceId, default, default, default);
            if (status == null)
            {
                // start orchestrator
                await starter.StartNewAsync("SampleOrchestration", instanceId, data);
                _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            }
        }
    }
}
