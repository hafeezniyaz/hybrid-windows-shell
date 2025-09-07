using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public class CredentialDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // The Username will be used as the ClientId for the OAuth flow.
        public required string Username { get; set; }
        public Guid AppId { get; set; }

        /// <summary>
        /// The plain-text, decrypted secret.
        /// </summary>
        public required string Secret { get; set; }
    }

}
