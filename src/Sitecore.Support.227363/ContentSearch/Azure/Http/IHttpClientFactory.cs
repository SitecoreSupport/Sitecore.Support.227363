namespace Sitecore.Support.ContentSearch.Azure.Http
{
  using Sitecore.ContentSearch.Azure.Http;

  public interface IHttpClientFactory
  {
    //Sitecore.Support.227363: modified signature to return our interface
    Sitecore.Support.ContentSearch.Azure.Http.IHttpClient Get(string baseAddress, string apiKey, IHttpMessageObserver observer, ICloudSearchRetryPolicy retryPolicy);
  }
}
