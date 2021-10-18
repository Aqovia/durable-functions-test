using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;


namespace Aqovia.DurableFunctions.Testing
{
    public class TestLogger : ILogger
    {
        private readonly ITestOutputHelper testOutput;
        private readonly Func<string, LogLevel, bool> filter;

        public TestLogger(ITestOutputHelper testOutput, string category, Func<string, LogLevel, bool> filter = null)
        {
            this.testOutput = testOutput;
            this.Category = category;
            this.filter = filter;
        }

        public string Category { get; private set; }

        public IList<LogMessage> LogMessages { get; } = new List<LogMessage>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.filter?.Invoke(this.Category, logLevel) ?? true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
            {
                return;
            }

            string formattedMessage = formatter(state, exception);
            this.LogMessages.Add(new LogMessage
            {
                Level = logLevel,
                EventId = eventId,
                State = state as IEnumerable<KeyValuePair<string, object>>,
                Exception = exception,
                FormattedMessage = formattedMessage,
                Category = this.Category,
            });

            try
            {
                this.testOutput.WriteLine($"    {DateTime.Now:o}: {formattedMessage}");
            }
            catch (InvalidOperationException)
            {
                //just have to discard output in this scenario - happens during teardown and attempting to log messages
                //known issue: https://github.com/adamralph/xbehave.net/issues/565
            }
        }
    }
}
