using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public  class PackageDetailsDto
    {
        public Guid Id { get; set; }
        public Guid AppId { get; set; }
        public required string Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime UploadedDate { get; set; }
        public required string FilePath { get; set; }
        public required string Md5Checksum { get; set; }
    }
}
