using Hangfire.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using NLog;

namespace JobHost
{
    public class JobLogger : ILog
    {
        private ILogger Logger { get; set; }

        public JobLogger(string name)
        {
            Logger = LogManager.GetLogger(name);
        }

        public bool Log(Hangfire.Logging.LogLevel logLevel, Func<string> messageFunc, Exception exception = null)
        {
            var msg = messageFunc?.Invoke();
  
            switch (logLevel)
            {
                case Hangfire.Logging.LogLevel.Trace:
                    Logger.Trace(exception, msg);
                    break;
                case Hangfire.Logging.LogLevel.Debug:
                    Logger.Debug(exception, msg);
                    break;
                case Hangfire.Logging.LogLevel.Info:
                    Logger.Info(exception, msg);
                    break;
                case Hangfire.Logging.LogLevel.Warn:
                    Logger.Warn(exception, msg);
                    break;
                case Hangfire.Logging.LogLevel.Error:
                    Logger.Error(exception, msg);
                    break;
                case Hangfire.Logging.LogLevel.Fatal:
                    Logger.Fatal(exception, msg);
                    break;
            }
            return true;
        }
    }

    public class NLogProvider : ILogProvider
    {
        public ILog GetLogger(string name)
        {
           return new JobLogger(name);
        }
    }
}
