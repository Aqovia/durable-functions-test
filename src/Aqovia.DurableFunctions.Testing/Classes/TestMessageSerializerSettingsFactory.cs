using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace Aqovia.DurableFunctions.Testing
{
    internal class TestMessageSerializerSettingsFactory : IMessageSerializerSettingsFactory
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
