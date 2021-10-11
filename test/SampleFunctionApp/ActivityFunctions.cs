using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading.Tasks;

using SampleFunctionApp.Services;

namespace SampleFunctionApp
{   
    public class ActivityFunctions
    {
        private readonly IMessageQueueService _messageQueueService;
        public ActivityFunctions(IMessageQueueService messageQueueService)
        {
            _messageQueueService = messageQueueService;
        }

        [FunctionName("SampleActivityFunction")]
        public async Task PublishServiceBusMessageFunctionAsync([ActivityTrigger] string message)
        {
            await _messageQueueService.PublishMessage(message);
        }
    }
}
