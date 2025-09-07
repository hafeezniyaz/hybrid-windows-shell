using Musaed.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Interfaces
{
    public interface IBackendApiClient
    {
 /// <summary>
        /// Get remote settings for the app, config and key defined in config
        /// </summary>
        /// <returns></returns>
        Task<AppSettingsDto> GetRemoteSettingsAsync(Guid appId, string configName, string assetName);

        /// <summary>
        /// Get the app id by name
        /// </summary>
        /// <param name="appName"> name of the app</param>
        /// <returns></returns>
        Task<Guid> GetAppIdAsync(string appName);

        /// <summary>
        /// Get credentails by credential name
        /// </summary>
        /// <param name="appId">app id </param>
        /// <param name="credentialName">name of the credentail</param>
        /// <returns></returns>
        Task<CredentialDto> GetCredentialAsync(Guid appId, string credentialName);

        /// <summary>
        /// Get Package metadata for a given app and version
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<PackageDetailsDto> GetPackageMetadataAsync(Guid appId, string version);

        /// <summary>
        /// Download the package using app id and version
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        Task<Byte[]> DownloadPackageAsync(Guid appId, string version, CancellationToken cancellationToken);

    }
}

