using System;
using System.Collections.Generic;
using System.Text;

namespace BBDown
{
    interface IFetcher
    {
        BBDownVInfo Fetch(string id);
    }
}
