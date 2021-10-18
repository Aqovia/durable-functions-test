using System.Net.Http;

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Aqovia.DurableFunctions.Testing
{
    internal class DurableHttpMessageHandlerFactory : IDurableHttpMessageHandlerFactory
    {
        private HttpMessageHandler httpClientHandler;

        public DurableHttpMessageHandlerFactory()
        {
        }

        internal DurableHttpMessageHandlerFactory(HttpMessageHandler handler)
        {
            this.httpClientHandler = handler;
        }

        public HttpMessageHandler CreateHttpMessageHandler()
        {
            if (this.httpClientHandler == null)
            {
                this.httpClientHandler = new HttpClientHandler();
            }

            return this.httpClientHandler;
        }
    }
}
