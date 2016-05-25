using System;
using System.Threading.Tasks;

namespace Sezame.Registration
{
    public interface ISezameRegistrationServiceInvoker
    {
        Task<SezameResult> RegisterAsync(string email, string applicationName);
        Task<SezameResult> SignAsync(string certificationSigningRequest, string sharedSecret);
        Task CancelAsync();
    }
}
