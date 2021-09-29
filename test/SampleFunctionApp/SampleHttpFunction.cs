using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SampleFunctionApp
{
    public class SampleHttpFunction
    {
        private readonly ILogger<SampleHttpFunction> _logger;

        public SampleHttpFunction(ILogger<SampleHttpFunction> logger)
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
            catch (AggregateException ex)
            {
                var failureReason = "Validation failed - Null or empty fields: " + string.Join("\n", ex.InnerExceptions.Select(_ => _.Message));
                _logger.LogError(failureReason);
            }
            catch (Exception ex)
            {
                var failureReason = $"Failed to provision EVC with error: {ex.Message}";
                _logger.LogError(ex, failureReason);
            }
        }


        [FunctionName("SampleFunction")]
        public async Task<IActionResult> SampleFunction(
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
    }
}
