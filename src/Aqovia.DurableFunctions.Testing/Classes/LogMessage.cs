using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace Aqovia.DurableFunctions.Testing
{
    public class LogMessage
    {
        public LogLevel Level { get; set; }

        public EventId EventId { get; set; }

        public IEnumerable<KeyValuePair<string, object>> State { get; set; }

        public Exception Exception { get; set; }

        public string FormattedMessage { get; set; }

        public string Category { get; set; }

        public override string ToString()
        {
            return this.FormattedMessage;
        }
    }
}
