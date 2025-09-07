using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musaed.Core.Interfaces
{
    public interface ICacheService
    {
        void Save<T>(T data, string key);
        T Load<T>(string key);
    }
}
