using System.Threading.Tasks;

namespace SampleFunctionApp.Services
{
    public interface IMessageQueueService
    {
        Task PublishMessage(string message);
    }
}
