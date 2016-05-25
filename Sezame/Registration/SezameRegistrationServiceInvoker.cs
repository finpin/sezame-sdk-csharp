using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sezame.Exceptions;

namespace Sezame.Registration
{
    public class SezameRegistrationServiceInvoker : SezameServiceInvoker, ISezameRegistrationServiceInvoker
    {
        public SezameRegistrationServiceInvoker() : base() { }
        public SezameRegistrationServiceInvoker(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler) { }

        public async Task<SezameResult> RegisterAsync(string email, string applicationName)
        {
            if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"))
            {
                throw new ArgumentException("Email argument is invalid.", "email");
            }
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                applicationName = email;
            }

            string requestdata = JsonConvert.SerializeObject(new { email = email, name = applicationName });
            var response = await this.HttpClient.PostAsync("/client/register", new StringContent(requestdata));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode };
            }
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SezameRegistrationResponse>(content);

            return new SezameResult(result.ToDictionary());
        }

        public async Task<SezameResult> SignAsync(string certificationSigningRequest, string sharedSecret)
        {
            if (string.IsNullOrWhiteSpace(certificationSigningRequest))
            {
                throw new ArgumentException("CertificateSigningRequest argument is required.", "certificateSigningRequest");
            }
            if (string.IsNullOrWhiteSpace(sharedSecret))
            {
                throw new ArgumentException("SharedSecret argument is required.", "sharedSecret");
            }

            string requestdata = JsonConvert.SerializeObject(new { sharedsecret = sharedSecret, csr = certificationSigningRequest });
            var response = await this.HttpClient.PostAsync("/client/sign", new StringContent(requestdata));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode };
            }
            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SezameSigningResponse>(content);

            return new SezameResult(result.ToDictionary());
        }

        public async Task CancelAsync()
        {
            var response = await this.HttpClient.PostAsync("/client/cancel", new StringContent(""));
            if (!response.IsSuccessStatusCode)
            {
                throw new SezameResponseException(string.Format("Server responded with an error code. Status code: {0}, reason phrase: {1}", response.StatusCode, response.ReasonPhrase)) { StatusCode = response.StatusCode, ReasonPhrase = response.ReasonPhrase };
            }
        }
    }
}
