using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BBDown.Core
{
    public interface IFetcher
    {
        Task<Entity.VInfo> FetchAsync(string id);
    }
}
