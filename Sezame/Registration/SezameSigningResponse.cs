using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sezame.Registration
{
    class SezameSigningResponse : ISezameServiceResponse
    {
        [JsonProperty("cert")]
        public string Certificate { get; set; }

        public Dictionary<int, string> ToDictionary()
        {
            return new Dictionary<int, string> 
            {
                { (int)SezameResultKey.Certificate, this.Certificate }
            };
        }
    }
}
