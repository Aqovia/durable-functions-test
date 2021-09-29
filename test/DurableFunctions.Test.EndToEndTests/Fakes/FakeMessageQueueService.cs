using System.Collections.Generic;
using System.Threading.Tasks;
using SampleFunctionApp.Services;

namespace DurableFunctions.Test.EndToEndTests
{
    public class FakeMessageQueueService : IMessageQueueService
    {
        public List<string> Messages { get; set; }

        public FakeMessageQueueService()
        {
            Messages = new List<string>();
        }

        public Task PublishMessage(string message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
