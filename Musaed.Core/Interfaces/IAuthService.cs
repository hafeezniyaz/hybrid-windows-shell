using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Gets a valid access token, handling the logic for different auth modes.
        /// This method will be responsible for managing the token's lifetime.
        /// </summary>
        /// <returns>A valid JWT access token.</returns>
        Task<string> GetAccessTokenAsync();
    }
}
