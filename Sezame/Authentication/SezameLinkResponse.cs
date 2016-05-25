using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sezame.Authentication
{
    class SezameLinkResponse : ISezameServiceResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("clientcode")]
        public string ClientCode { get; set; }

        public Dictionary<int, string> ToDictionary()
        {
            return new Dictionary<int, string> 
            {
                { (int)SezameResultKey.Id, this.Id },
                { (int)SezameResultKey.ClientCode, this.ClientCode }
            };
        }
    }
}
