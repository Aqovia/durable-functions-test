using System.Threading.Tasks;

namespace SampleFunctionApp.Services
{
    public class MessageQueueService : IMessageQueueService
    {
        public async Task PublishMessage(string message)
        {
            //simulate the publication of a message to a queue
            await Task.Delay(0);
        }
    }
}
