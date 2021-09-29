using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace SampleFunctionApp.Services
{
    public class MessageSerializer : IMessageSerializerSettingsFactory
    {
        public JsonSerializerSettings CreateJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
        }
    }
}