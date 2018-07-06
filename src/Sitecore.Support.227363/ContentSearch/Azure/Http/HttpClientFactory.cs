namespace Sitecore.Support.ContentSearch.Azure.Http
{
  using Sitecore.ContentSearch.Azure.Http;
  using System;
  using System.Net.Http;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using System.Runtime.InteropServices;
  using System.Threading.Tasks;

  internal class HttpClientFactory : IHttpClientFactory
  {
    private IHttpClient client;

    //Sitecore.Support.227363: creates and returns our http client wrapper
    private IHttpClient CreateClient(string baseAddress, string apiKey, DelegatingHandler retryHandler = null)
    {
      if (this.client == null)
      {
        this.client = (retryHandler == null) ? new Sitecore.Support.ContentSearch.Azure.Http.HttpClientWrapper() : new Sitecore.Support.ContentSearch.Azure.Http.HttpClientWrapper(retryHandler, this.ClientTimeout);
        string[] textArray1 = new string[] { apiKey };
        this.client.AddDefaultHeader("api-key", textArray1);
        this.client.BaseAddress = new Uri(baseAddress);
      }
      return this.client;
    }

    public IHttpClient Get(string baseAddress, string apiKey, ICloudSearchRetryPolicy retryPolicy)
    {
      RetryDelegatingHandler retryHandler = new RetryDelegatingHandler(new HttpClientHandler(), retryPolicy);
      return this.CreateClient(baseAddress, apiKey, retryHandler);
    }

    //Sitecore.Support.227363: modified to use our factory
    public IHttpClient Get(string baseAddress, string apiKey, IHttpMessageObserver observer, ICloudSearchRetryPolicy retryPolicy)
    {
      //ObservableDelegatingHandler retryHandler = new ObservableDelegatingHandler(new RetryDelegatingHandler(new HttpClientHandler(), retryPolicy), observer);
      //ConstructorInfo constructor = Extensions.azureSearchAssembly.GetType("Sitecore.ContentSearch.Azure.Http.ObservableDelegatingHandler").GetConstructor(new Type[] { typeof(HttpMessageHandler), typeof(IHttpMessageObserver) })
      object[] initParams = new object[] { new RetryDelegatingHandler( new HttpClientHandler(), retryPolicy), observer };
      DelegatingHandler retryHandler = (DelegatingHandler)Activator.CreateInstance(Extensions.azureSearchAssembly.GetType("Sitecore.ContentSearch.Azure.Http.ObservableDelegatingHandler"), initParams);
      return this.CreateClient(baseAddress, apiKey, retryHandler);
    }

    public TimeSpan ClientTimeout { get; set; }
  }
}
