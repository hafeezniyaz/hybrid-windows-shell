using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Helpers
{
    public static class ProcessShutdownHelper
    {

        public static void GracefullyShutdown(Process process, ILogger logger)
        {
            if (process == null || process.HasExited) return;
            try
            {
                if (process.CloseMainWindow() && process.WaitForExit(5000))
                {
                    logger.LogInformation($" process {process.ProcessName} " +
                        $"with PID {process.Id} was closed gracefully");
                }
                else
                {
                    logger.LogInformation($"Force stopping the process {process.ProcessName} with PID {process.Id}...");
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"An error has occured while trying to stop process," +
                    $"{process.ProcessName} with PID {process.Id}" +
                    " It may have already existed");
            }
        }

        /// <summary>
        /// Close multiple process with Gracefull shutdown 
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task CreateAndRunShutDownTask(IEnumerable<Process> processes, ILogger logger)
        {
            var shutdownTasks = processes.Select(async process =>
            {
                await Task.Run(() => GracefullyShutdown(process, logger));
            });

            await Task.WhenAll(shutdownTasks);

        }

        /// <summary>
        /// Close all server and other instances of the app except the current
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async static Task CloseAllOtherInstances(string serverName, ILogger logger)
        {

            var currentAppProcess = Process.GetCurrentProcess();
            var clientProcesses = Process.GetProcessesByName(currentAppProcess.ProcessName);


            var serverProcesses = Process.GetProcessesByName(serverName);

            if (clientProcesses.Length > 1)
            {
                var otherProcesses = clientProcesses.Where(p => p.Id != currentAppProcess.Id).ToList();

                if (!otherProcesses.Any()) return;

                await CreateAndRunShutDownTask(otherProcesses, logger);
            }

            //close all servers
            if (serverProcesses.Any())
                await CreateAndRunShutDownTask(serverProcesses, logger);


        }
    }

}
