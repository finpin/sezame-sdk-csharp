using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sezame.Authentication
{
    class SezameAuthenticationStatusResponse : ISezameServiceResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }

        public Dictionary<int, string> ToDictionary()
        {
            return new Dictionary<int, string> 
            {
                { (int)SezameResultKey.Id, this.Id },
                { (int)SezameResultKey.AuthenticationStatus, this.Status }
            };
        }
    }
}
