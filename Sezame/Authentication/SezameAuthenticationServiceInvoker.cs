using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

using Sezame;
using Sezame.Exceptions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Sezame.Authentication
{
    public class SezameAuthenticationServiceInvoker : SezameServiceInvoker, ISezameAuthenticationServiceInvoker
    {
        public SezameAuthenticationServiceInvoker() : base() { }
        public SezameAuthenticationServiceInvoker(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler) { }

        public async Task<bool> IsLinkedAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username argument is required.", "username");
            }

            string requestdata = JsonConvert.SerializeObject(new { username = username });
            var response = await this.HttpClient.PostAsync("/client/link/status", new StringContent(requestdata));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode };
            }
            string content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<bool>(content);
        }

        public async Task<SezameResult> LinkAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username argument is required.", "username");
            }

            string requestdata = JsonConvert.SerializeObject(new { username = username });
            var response = await this.HttpClient.PostAsync("/client/link", new StringContent(requestdata));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { StatusCode = response.StatusCode, ReasonPhrase = response.ReasonPhrase };
            }
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SezameLinkResponse>(content);

            return new SezameResult(result.ToDictionary());
        }


        private string CreateAuthenticationRequestData(string username, string message, string type, int timeout, string callbackUrl, Dictionary<string, object> otherparams)
        {
            var requestdata = new JObject();
            requestdata.Add("username", username); // add username

            if (string.IsNullOrWhiteSpace(type)) 
            {
                type = "auth";
            }
            requestdata.Add("type", type); // add type

            if (!string.IsNullOrWhiteSpace(message)) 
            {
                requestdata.Add("message", message); // add message
            }
            
            if (timeout > 0) 
            {
                requestdata.Add("timeout", timeout); // add timeout
            }

            if (!string.IsNullOrWhiteSpace(callbackUrl)) 
            {
                requestdata.Add("callback", callbackUrl); // add callback
            }

            if (otherparams != null && otherparams.Count > 0) 
            {
                requestdata.Add("params", JsonConvert.SerializeObject(otherparams)); // add params
            }

            return JsonConvert.SerializeObject(requestdata);
        }
           
        public async Task<SezameResult> RequestAuthenticationAsync(string username, string message = null, string type = "auth", int timeout = 0, string callbackUrl = null, Dictionary<string, object> otherparams = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username argument is required.", "username");
            }

            string requestdata = this.CreateAuthenticationRequestData(username, message, type, timeout, callbackUrl, otherparams);

            var response = await this.HttpClient.PostAsync("/auth/login", new StringContent(requestdata));
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    var resp = new SezameAuthenticationStatusResponse();
                    resp.Status = "notlinked";
                    return new SezameResult(resp.ToDictionary());
                }
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode };
            }
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SezameAuthenticationStatusResponse>(content);

            return new SezameResult(result.ToDictionary());
        }

        public async Task<SezameResult> CheckAuthenticationStatusAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id argument is required.", "id");
            }

            var response = await this.HttpClient.GetAsync(string.Format("/auth/status/{0}", id));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode };
            }
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SezameAuthenticationStatusResponse>(content);

            return new SezameResult(result.ToDictionary());
        }
    }
}
