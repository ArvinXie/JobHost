using Hangfire;
using Hangfire.Logging;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace JobHost 
{
    public class Host
    {
        private BackgroundJobServer _server;
        private Logger log = LogManager.GetCurrentClassLogger();

        private string _hostName = "host";
        private string[] _jobQueues;
        private int _workerCount = 10;
        private Action _onStarted = null;

        public Host(string hostName, string[] queues, int workerCount = 10, Action onStarted = null)
        {
            _hostName = hostName;
            _jobQueues = queues;
            _workerCount = workerCount;
            _onStarted = onStarted;
            LogProvider.SetCurrentLogProvider(new NLogProvider());
        }

        public async Task Run(string[] args)
        {
            if (args.Contains("--run-as-service"))
            {
                using (var windowsService = new WindowsService(this))
                {
                    ServiceBase.Run(windowsService);
                    return;
                }
            }

            Console.Title = _hostName;

            var tcs = new TaskCompletionSource<object>();
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; tcs.SetResult(null); };

            await Start();
            await Console.Out.WriteLineAsync("Press Ctrl+C to exit...");
            await tcs.Task;
            await Stop();
        }


        public async Task Start()
        {
            try
            {
                await Task.Run(() =>
                {
                    var serverOptions = new BackgroundJobServerOptions()
                    {
                        ServerName = string.Format("{0}-{1}", Environment.MachineName, _hostName),
                        Queues = _jobQueues,
                        WorkerCount = _workerCount
                    };
                    _server = new BackgroundJobServer(serverOptions);
                    _onStarted?.Invoke();
                });
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to start.");
            }
        }

        public async Task Stop()
        {
            try
            {
                await Task.Run(() => _server.Dispose());
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to stop correctly.");
            }
        }

        public static IConfiguration GetConfiguration()
        {
            var fileName = "appsettings.json";
            var directory = AppContext.BaseDirectory;
            directory = directory.Replace("\\", "/");
            var filePath = $"{directory}/{fileName}";
            if (!File.Exists(filePath))
            {
                var length = directory.IndexOf("/bin");
                filePath = $"{directory.Substring(0, length)}/{fileName}";
            }
            var builder = new ConfigurationBuilder().AddJsonFile(filePath, false, true);
            return builder.Build();
        }

    }
}
