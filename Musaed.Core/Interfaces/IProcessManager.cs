using Musaed.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Interfaces
{
    public interface IProcessManager: IDisposable
    {
        /// <summary>
        /// Starts the server process and waits for it to become healthy.
        /// </summary>
        Task StartServerAsync(AppSettingsDto appSettings, Guid appId);

        /// <summary>
        /// Stops the server process if it is running.
        /// </summary>
        Task StopServer(bool? closeAllInstancedAndServer = false);
    }
}
