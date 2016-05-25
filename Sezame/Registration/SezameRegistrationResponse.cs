using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sezame.Registration
{
    class SezameRegistrationResponse : ISezameServiceResponse
    {
        [JsonProperty("clientcode")]
        public string ClientCode { get; set; }
        [JsonProperty("sharedsecret")]
        public string SharedSecret { get; set; }

        public System.Collections.Generic.Dictionary<int, string> ToDictionary()
        {
            return new Dictionary<int, string> 
            {
                { (int)SezameResultKey.ClientCode, this.ClientCode },
                { (int)SezameResultKey.SharedSecret, this.SharedSecret }
            };
        }
    }
}
