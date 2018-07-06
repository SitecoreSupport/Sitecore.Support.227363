namespace Sitecore.Support.ContentSearch.Azure.Http
{
  using Sitecore.ContentSearch.Azure.Http;
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Runtime.InteropServices;
  using System.Threading.Tasks;

  internal class HttpClientWrapper : Sitecore.Support.ContentSearch.Azure.Http.IHttpClient, IDisposable
  {
    private readonly HttpClient httpClient;

    public HttpClientWrapper()
    {
      this.httpClient = new HttpClient();
    }

    public HttpClientWrapper(HttpMessageHandler retryHandler) : this(retryHandler, TimeSpan.FromSeconds(100.0))
    {
    }

    public HttpClientWrapper(HttpMessageHandler retryHandler, TimeSpan httpClientTimeout)
    {
      HttpClient client1 = new HttpClient(retryHandler)
      {
        Timeout = (httpClientTimeout == TimeSpan.Zero) ? TimeSpan.FromMilliseconds(-1.0) : httpClientTimeout
      };
      this.httpClient = client1;
    }

    public void AddDefaultHeader(string key, params string[] values)
    {
      this.httpClient.DefaultRequestHeaders.Add(key, values);
    }

    private void AddHeaders(HttpRequestMessage requestMessage, IEnumerable<KeyValuePair<string, string>> headers)
    {
      requestMessage.Headers.TryAddWithoutValidation("request-id", Guid.NewGuid().ToString());
      if (headers != null)
      {
        foreach (KeyValuePair<string, string> pair in headers)
        {
          requestMessage.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
        }
      }
    }

    //Sitecore.Support.227363: convert to async await
    public async Task<HttpResponseMessage> Delete(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri);
      this.AddHeaders(requestMessage, headers);
      return await this.httpClient.SendAsync(requestMessage);
    }

    public void Dispose()
    {
      this.httpClient.Dispose();
    }

    //Sitecore.Support.227363: for synchronous calls
    public HttpResponseMessage GetOriginal(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
      this.AddHeaders(requestMessage, headers);
      return this.httpClient.SendAsync(requestMessage).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    //Sitecore.Support.227363: convert to async await
    public async Task<HttpResponseMessage> Get(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
      this.AddHeaders(requestMessage, headers);
      return await this.httpClient.SendAsync(requestMessage);
    }

    //Sitecore.Support.227363: convert to async await
    public async Task<HttpResponseMessage> Post(string requestUri, HttpContent content, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
      {
        Content = content
      };
      this.AddHeaders(requestMessage, headers);
      return await this.httpClient.SendAsync(requestMessage);
    }

    //Sitecore.Support.227363: convert to async await
    public async Task<HttpResponseMessage> Put(string requestUri, HttpContent content, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
      HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri)
      {
        Content = content
      };
      this.AddHeaders(requestMessage, headers);
      return await this.httpClient.SendAsync(requestMessage);
    }

    public Uri BaseAddress
    {
      get
      {
        return this.httpClient.BaseAddress;
      }
      set
      {
        this.httpClient.BaseAddress = value;
      }
    }
  }
}
