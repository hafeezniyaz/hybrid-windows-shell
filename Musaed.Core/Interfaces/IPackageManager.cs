using Musaed.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Interfaces
{
    public interface IPackageManager
    {
        string ServerExePath { get; }   
        Task EnsurePackageIsReadyAsync(Guid appId, ServerSettingsDto serverSettings);
    }
}
