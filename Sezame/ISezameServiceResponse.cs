using System.Collections.Generic;

namespace Sezame
{
    interface ISezameServiceResponse
    {
        Dictionary<int, string> ToDictionary();
    }
}