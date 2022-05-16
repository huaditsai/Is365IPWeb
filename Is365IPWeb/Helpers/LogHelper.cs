using Microsoft.Extensions.Logging;

namespace Is365IPWeb.Helpers
{
    public class LogHelper
    {
        internal static class AppLogging
        {
            internal static ILoggerFactory LoggerFactory { get; set; }// = new LoggerFactory();
            internal static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
            internal static ILogger CreateLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);

        }
    }
}
