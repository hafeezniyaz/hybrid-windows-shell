using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public class LogEntryDto
    {
        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Source { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Level { get; set; }

        [Required]
        public required string Message { get; set; }

        [MaxLength(100)]
        public string? Hostname { get; set; }
    }
}
