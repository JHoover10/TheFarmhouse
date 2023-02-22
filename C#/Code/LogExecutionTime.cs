using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace C_Sharp;

public class LogExecutionTime : IDisposable
{
    private readonly ILogger logger;
    private readonly LogLevel logLevel;
    private readonly int? threshold;
    private readonly string methodName;
    private readonly Stopwatch stopwatch;

    public LogExecutionTime(ILogger logger, LogLevel logLevel = LogLevel.Debug, int? threshold = null, [CallerMemberName] string methodName = null)
    {
        this.logger = logger;
        this.logLevel = logLevel;
        this.threshold = threshold;
        this.methodName = methodName;
        stopwatch = Stopwatch.StartNew();

        logger.Log(logLevel, "Beginning method {0} execution.", methodName);
    }

    public void Dispose()
    {
        stopwatch.Stop();
        var timeSpent = stopwatch.ElapsedMilliseconds;

        if (threshold != null && timeSpent > threshold) 
        {
            logger.Log(LogLevel.Warning, "Method {0} was expected to finsih within {1} milliseconds, but took {2}.", methodName, threshold, timeSpent);
        }
        else
        {
            logger.Log(logLevel, "Method {0} took {1} milliseconds to execute", methodName, timeSpent);
        }
    }
}