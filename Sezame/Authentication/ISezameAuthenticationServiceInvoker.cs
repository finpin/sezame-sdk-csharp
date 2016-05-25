using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sezame.Authentication
{
    public interface ISezameAuthenticationServiceInvoker
    {
        Task<SezameResult> CheckAuthenticationStatusAsync(string id);
        Task<SezameResult> RequestAuthenticationAsync(string username, string message = null, string type = "auth", int timeout = 0, string callbackUrl = null, Dictionary<string, object> otherparams = null);
        Task<bool> IsLinkedAsync(string username);
        Task<SezameResult> LinkAsync(string username);
    }
}
