namespace Sitecore.Support.ContentSearch.Azure.Http
{
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Threading.Tasks;

  public interface IHttpClient : IDisposable
  {

    void AddDefaultHeader(string key, params string[] value);

    //Sitecore.Support.227363: modified signatures for async await pattern
    Task<HttpResponseMessage> Delete(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null);
    Task<HttpResponseMessage> Get(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null);
    Task<HttpResponseMessage> Post(string requestUri, HttpContent content, IEnumerable<KeyValuePair<string, string>> headers = null);
    Task<HttpResponseMessage> Put(string requestUri, HttpContent content, IEnumerable<KeyValuePair<string, string>> headers = null);

    //Sitecore.Support.227363: added to be used as synchronous call for UI search requests
    HttpResponseMessage GetOriginal(string requestUri, IEnumerable<KeyValuePair<string, string>> headers = null);

    Uri BaseAddress { get; set; }
  }
}
