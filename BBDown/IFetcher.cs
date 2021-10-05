using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BBDown
{
    interface IFetcher
    {
        Task<BBDownVInfo> FetchAsync(string id);
    }
}
