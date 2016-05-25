using System;
using System.Net.Http;

namespace Sezame
{
    public abstract class SezameServiceInvoker : IDisposable
    {
        private bool _disposed = false;
        protected HttpClient HttpClient { get; private set; }

        public SezameServiceInvoker() 
        {
            this.HttpClient = new HttpClient();
            this.HttpClient.BaseAddress = new Uri("https://hqfrontend-finprin.finprin.com/");
        }

        public SezameServiceInvoker(HttpMessageHandler handler, bool disposeHandler) 
        {
            this.HttpClient = new HttpClient(handler, disposeHandler);
            this.HttpClient.BaseAddress = new Uri("https://hqfrontend-finprin.finprin.com/");
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) 
        {
            if (this._disposed) 
            {
                return; 
            }
            if (disposing)
            {
                if (this.HttpClient != null)
                {
                    this.HttpClient.Dispose();
                    this.HttpClient = null;
                }
            }
            this._disposed = true;
        }
    }
}
