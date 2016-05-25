using System;
using System.Net;

namespace Sezame.Exceptions
{
    public class SezameResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }

        public SezameResponseException() : base("Server responded with an error code.") { }
        public SezameResponseException(string message) : base(message) { }
        public SezameResponseException(string message, Exception innerException) : base(message, innerException) { }
    }
}

