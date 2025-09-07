using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public class GetAppsResponseDto
    {
        public int TotalCount { get; set; }
        public IEnumerable<AppDto>? Items { get; set; }
    }
}
