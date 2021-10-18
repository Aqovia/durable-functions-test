using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;

namespace Aqovia.DurableFunctions.Testing
{
    public static class ServiceBusMessageHelper
    {
        public static Message CreateNewMessage(string messageId, object content)
        {
            var message = CreateNewMessage();
            message.MessageId = messageId;
            message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            }));

            return message;
        }

        public static Message CreateNewMessage()
        {
            var message = new Message();
            var systemProperties = new Message.SystemPropertiesCollection();

            var bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty;

            // Workaround for assigning EnqueuedTimeUtc which has private setter (i.e. systemProperties.EnqueuedTimeUtc = DateTime.UtcNow.AddMinutes(1))
            var value = DateTime.UtcNow.AddMinutes(1);
            systemProperties.GetType().InvokeMember("EnqueuedTimeUtc", bindings, Type.DefaultBinder, systemProperties, new object[] { value });

            // Workaround for preventing "ThrowIfNotReceived" by setting "SequenceNumber" value
            systemProperties.GetType().InvokeMember("SequenceNumber", bindings, Type.DefaultBinder, systemProperties, new object[] { 1 });

            // Workaround for assigning SystemProperties which has private setter (i.e. message.SystemProperties = systemProperties)
            message.GetType().InvokeMember("SystemProperties", bindings, Type.DefaultBinder, message, new object[] { systemProperties });

            return message;
        }
    }
}
