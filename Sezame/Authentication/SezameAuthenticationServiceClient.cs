using Newtonsoft.Json;
using Sezame.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Sezame.Authentication
{
    public class SezameAuthenticationServiceClient : IDisposable 
    {
        private ISezameAuthenticationServiceInvoker _invoker;

        public SezameAuthenticationServiceClient(ISezameAuthenticationServiceInvoker invoker) 
        {
            this._invoker = invoker;
        }

        public Task<bool> IsUserLinkedAsync(string username) 
        {
            return this._invoker.IsLinkedAsync(username);
        }

        public async Task<string> GeneratePairingDataAsync(string username)
        {
            try
            {
                var response = await this._invoker.LinkAsync(username);
                var userid = response.GetParameter(SezameResultKey.Id);
                var clientcode = response.GetParameter(SezameResultKey.ClientCode);

                return JsonConvert.SerializeObject(new { username = username, id = userid, client = clientcode });
            }
            catch (SezameResponseException exception) 
            {
                if (exception.StatusCode == HttpStatusCode.Conflict)
                {
                    return null; // already linked
                }
                else 
                {
                    throw; // rethrow caught exception
                }
            }
        }

        public async Task<SezameAuthenticationResultKey> AuthenticateUserAsync(string username, string message, TimeSpan timeout)
        {
            SezameResult response = null;
            try
            {
                response = await this._invoker.RequestAuthenticationAsync(username, message, "auth", (int)Math.Ceiling(timeout.TotalMinutes)); 
            }
            catch (SezameResponseException exception) 
            {
                if (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    return SezameAuthenticationResultKey.NotPaired;
                }
                else 
                {
                    throw; // rethrow caught exception
                }
            }
            var userid = response.GetParameter(SezameResultKey.Id);
            var status = response.GetParameter(SezameResultKey.AuthenticationStatus);

            var result = SezameAuthenticationResultKey.Timedout;
            if (status == "initiated") 
            {
                result = await Task.Run<SezameAuthenticationResultKey>(async () => // poll the status in a new task
                {
                    int pollingtime = 1000; // poll every second
                    int loopPassCount = (int)Math.Ceiling(Math.Ceiling((double)timeout.TotalMilliseconds) / pollingtime);
                    while (loopPassCount > 0) 
                    {
                        response = await this._invoker.CheckAuthenticationStatusAsync(userid);
                        status = response.GetParameter(SezameResultKey.AuthenticationStatus);
                        if (status == "authorized")
                        {
                            return SezameAuthenticationResultKey.Authenticated;
                        }
                        else if(status == "denied")
                        {
                            return SezameAuthenticationResultKey.Denied;
                        }
                        loopPassCount--;
                        System.Threading.Thread.Sleep(pollingtime);
                    }
                    return SezameAuthenticationResultKey.Timedout; 
                });
            }

            return result;
        }

        public async Task<SezameAuthenticationResultKey> UserFraudAsync(string username, string message) 
        {
            try
            {
                await this._invoker.RequestAuthenticationAsync(username, message, "fraud", 24 * 60);
                return SezameAuthenticationResultKey.FraudWarned;
            }
            catch (SezameResponseException exception) 
            {
                if (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    return SezameAuthenticationResultKey.NotPaired;
                }
                else 
                {
                    throw; // rethrow caught exception
                }
            }
        }

        public void Dispose() 
        {
            if(this._invoker != null && (this._invoker as IDisposable) != null)
            {
                ((IDisposable)this._invoker).Dispose();
                this._invoker = null;
            }
        }
    }
}
