﻿using DurableTask.Emulator;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableFunctions.Test
{
    internal class EmulatorDurabilityProviderFactory : IDurabilityProviderFactory
    {
        private readonly DurabilityProvider provider;

        public EmulatorDurabilityProviderFactory()
        {
            var service = new LocalOrchestrationService();
            this.provider = new DurabilityProvider("emulator", service, service, "emulator");
        }

        public bool SupportsEntities => false;

        public virtual string Name => "Emulator";

        public virtual DurabilityProvider GetDurabilityProvider(DurableClientAttribute attribute)
        {
            return this.provider;
        }

        public virtual DurabilityProvider GetDurabilityProvider()
        {
            return this.provider;
        }
    }
}
