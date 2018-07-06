namespace Sitecore.Support.ContentSearch.Azure.Http
{
  using Newtonsoft.Json;
  using Newtonsoft.Json.Serialization;
  using Sitecore.ContentSearch.Azure;
  using Sitecore.ContentSearch.Azure.Exceptions;
  using Sitecore.ContentSearch.Azure.Http;
  using Sitecore.ContentSearch.Azure.Models;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Diagnostics;
  using Sitecore.Exceptions;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Net;
  using System.Net.Http;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Threading;
  using Sitecore.ContentSearch.Azure.Http.Exceptions;
  using System.Reflection;
  using Newtonsoft.Json.Linq;
  using System.Linq;
  using System.Collections;
  using System.Threading.Tasks;

  public class SearchServiceClient : ISearchServiceConnectionInitializable, ISearchServiceDocumentOperationsProvider, ISearchServiceManagmentOperationsProvider
  {
    private string apiKey;
    private string apiVersion;
    private readonly IHttpClientFactory clientFactory;
    private IndexDefinition indexDefinition;
    private string indexEtag;
    private readonly AbstractLog log;
    private readonly ICloudSearchRetryPolicy retryPolicy;
    private string searchService;

    public SearchServiceClient(IHttpClientFactory clientFactory, ICloudSearchRetryPolicy retryPolicy) : this(clientFactory, retryPolicy, CrawlingLog.Log)
    {
    }

    internal SearchServiceClient(IHttpClientFactory clientFactory, ICloudSearchRetryPolicy retryPolicy, AbstractLog log)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(log, "log");
      this.clientFactory = clientFactory;
      this.retryPolicy = retryPolicy;
      this.log = log;
    }

    //Sitecore.Support.227363: convert to async await
    public async virtual void CreateIndex(IndexDefinition indexDefinition)
    {
      string str = this.SerializeIndexDefinition(indexDefinition);
      string requestUri = SearchUrl.PutIndex(this.IndexName, this.apiVersion);
      IHttpClient client = this.GetClient();
      KeyValuePair<string, string>[] headers = new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("If-None-Match", "*") };
      StringContent content = new StringContent(str, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await client.Put(requestUri, content, headers);
      if (response.StatusCode == HttpStatusCode.PreconditionFailed)
      {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
          Thread.Sleep(TimeSpan.FromMilliseconds(100.0));
          if (stopwatch.Elapsed > this.ManagementOperationsTimeout)
          {
            //throw new SearchIndexCreationException(this.IndexName, "Failed to create Search index " + this.IndexName, new AzureSearchServiceRESTCallException(this.IndexName, response.Content.ReadAsStringAsync().Result));
            //Sitecore.Support.227363: create internal exception object through reflection, same for similar comments below
            throw new SearchIndexCreationException(this.IndexName, "Failed to create Search index " + this.IndexName, "AzureSearchServiceRESTCallException".CreateInternalAzureExceptionFromTypeName(this.IndexName, response.Content.ReadAsStringAsync().Result));
          }
          content = new StringContent(str, Encoding.UTF8, "application/json");
          response = await client.Put(requestUri, content, headers);
        }
      }
      await this.EnsureSuccessStatusCode(response);
    }

    //Sitecore.Support.227363: convert to async await
    public async virtual void DeleteIndex()
    {
      string requestUri = SearchUrl.DeleteIndex(this.IndexName, this.apiVersion);
      IHttpClient client = this.GetClient();
      KeyValuePair<string, string>[] headers = new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("If-Match", "*") };
      HttpResponseMessage response = await client.Delete(requestUri, headers);
      if (response.StatusCode == HttpStatusCode.PreconditionFailed)
      {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
          Thread.Sleep(TimeSpan.FromMilliseconds(100.0));
          if (stopwatch.Elapsed > this.ManagementOperationsTimeout)
          {
            //throw new SearchIndexDeletionException(this.IndexName, "Failed to delete Search index " + this.IndexName, new AzureSearchServiceRESTCallException(this.IndexName, response.Content.ReadAsStringAsync().Result));
            throw new SearchIndexDeletionException(this.IndexName, "Failed to delete Search index " + this.IndexName, "AzureSearchServiceRESTCallException".CreateInternalAzureExceptionFromTypeName(this.IndexName, response.Content.ReadAsStringAsync().Result));
          }
          response = await client.Delete(requestUri, headers);
        }
      }
      await this.EnsureSuccessStatusCode(response);
    }

    protected virtual IndexDefinition DeserializeIndexDefinition(string serviceResponse) =>
        JsonConvert.DeserializeObject<IndexDefinition>(serviceResponse);

    //Sitecore.Support.227363: convert to async await
    private async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
      if (!response.IsSuccessStatusCode || (response.StatusCode == ((HttpStatusCode)0xcf)))
      {
        string message = string.Empty;
        if (response.Content != null)
        {
          message = await response.Content.ReadAsStringAsync();
        }
        //AzureSearchServiceRESTCallException innerException = new AzureSearchServiceRESTCallException(this.IndexName, message);
        Exception innerException = "AzureSearchServiceRESTCallException".CreateInternalAzureExceptionFromTypeName(this.IndexName, message);
        HttpStatusCode statusCode = response.StatusCode;
        if (statusCode <= HttpStatusCode.NotFound)
        {
          switch (statusCode)
          {
            case HttpStatusCode.BadRequest:
              //throw new BadRequestException(this.IndexName, "Error in the request URI, headers, or body", innerException);
              throw "BadRequestException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Error in the request URI, headers, or body", innerException);

            case HttpStatusCode.Forbidden:
              //throw new AuthorizationFailedException(this.IndexName, "Authorization failed, please check API key value in connectionstrings.config", innerException);
              throw "AuthorizationFailedException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Authorization failed, please check API key value in connectionstrings.config", innerException);

            case HttpStatusCode.NotFound:
              //throw new NotFoundException(this.IndexName, "Search service or index not found, please check service name in connectionstrings.config and if index exists in the portal", innerException);
              throw "NotFoundException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Search service or index not found, please check service name in connectionstrings.config and if index exists in the portal", innerException);

            case ((HttpStatusCode)0xcf):
              //throw new PostFailedForSomeDocumentsException(this.IndexName, "Partial success for insert or update. Some documents succeeded, but at least one failed.", this.GetFailedDocuments(response), innerException);
              throw "PostFailedForSomeDocumentsException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Partial success for insert or update. Some documents succeeded, but at least one failed.", this.GetFailedDocuments(response), innerException);
          }
        }
        else
        {
          if (statusCode == HttpStatusCode.RequestEntityTooLarge)
          {
            throw new RequestEntityTooLargeException(innerException);
          }
          if (statusCode == ((HttpStatusCode)0x1ad))
          {
            //throw new AzureQuotaExceededException(this.IndexName, "Quota for number of indexes of documents per index exeeded, consider updrgading search service to next service tier", innerException);
            throw "AzureQuotaExceededException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Quota for number of indexes of documents per index exeeded, consider updrgading search service to next service tier", innerException);
          }
        }
        //throw new AzureSearchServiceRESTCallException(this.IndexName, "Error while search service call, see details in message", innerException);
        throw "AzureSearchServiceRESTCallException".CreateInternalAzureExceptionFromTypeName(this.IndexName, "Error while search service call, see details in message", innerException);
      }
    }

    private IHttpClient GetClient()
    {
      if (this.Observer == null)
      {
        //throw new CloudSearchMissingImplementationException("Observer is null.", "IHttpMessageObserver");
        throw "CloudSearchMissingImplementationException".CreateInternalAzureExceptionFromTypeName("Observer is null.", "IHttpMessageObserver");
      }
      return this.clientFactory.Get(this.searchService, this.apiKey, this.Observer, this.retryPolicy);
    }
    private IEnumerable GetFailedDocuments(HttpResponseMessage response)
    {
      //Sitecore.Support.227363: reflection based workaround to filter based on property from internal class
      return (from d in response.GetMultiStatusDocuments()
              where !(bool)Extensions.statusProp.GetValue(d)
              select d);
              //where !d.Status
              //select d);
    }

    //Sitecore.Support.227363: convert to async await
    public async virtual Task<IndexDefinition> GetIndex()
    {
      string index = SearchUrl.GetIndex(this.IndexName, this.apiVersion);
      HttpResponseMessage response = await this.GetClient().Get(index, null);
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return null;
      }
      await this.EnsureSuccessStatusCode(response);
      if (this.indexEtag != response.Headers.ETag.Tag)
      {
        string result = await response.Content.ReadAsStringAsync();
        IndexDefinition definition = this.DeserializeIndexDefinition(result);
        if (definition == null)
        {
          this.log.Warn($"[Index={this.IndexName}] IndexDefinition is null for response: {result}", null);
          return this.indexDefinition;
        }
        this.indexEtag = response.Headers.ETag.Tag;
        this.indexDefinition = definition;
      }
      return this.indexDefinition;
    }

    public virtual IndexStatistics GetIndexStatistics()
    {
      string indexStatistics = SearchUrl.GetIndexStatistics(this.IndexName, this.apiVersion);
      HttpResponseMessage response = this.GetClient().GetOriginal(indexStatistics, null);
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return null;
      }
      this.EnsureSuccessStatusCode(response).ConfigureAwait(false).GetAwaiter().GetResult();
      return JsonConvert.DeserializeObject<IndexStatistics>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }


    public bool IndexExists()
    {
      string index = SearchUrl.GetIndex(this.IndexName, this.apiVersion);
      HttpResponseMessage response = this.GetClient().GetOriginal(index, null);
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return false;
      }
      this.EnsureSuccessStatusCode(response).ConfigureAwait(false).GetAwaiter().GetResult();
      return true;
    }


    public void Initialize(string indexName, string connectionString)
    {
      CloudSearchServiceSettings settings = new CloudSearchServiceSettings(connectionString);
      if (!settings.Valid)
      {
        throw new ConfigurationException($"The connection string for '{connectionString}' is incorrect.");
      }
      this.IndexName = indexName;
      this.searchService = settings.SearchService;
      this.apiKey = settings.ApiKey;
      this.apiVersion = settings.ApiVersion;
    }

    //Sitecore.Support.227363: convert to async await
    public async virtual void PostDocuments(string jsonString)
    {
      string requestUri = SearchUrl.PostDocuments(this.IndexName, this.apiVersion);
      StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await this.GetClient().Post(requestUri, content, null);
      await this.EnsureSuccessStatusCode(response);
    }

    public virtual string Search(string expression)
    {
      string requestUri = SearchUrl.GetDocuments(this.IndexName, this.apiVersion, expression);
      //Sitecore.Support.227363: still synchronous, added new method(http client wrapper) with original function, as Get is async now
      HttpResponseMessage response = this.GetClient().GetOriginal(requestUri, null);
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return null;
      }
      if (response.StatusCode == HttpStatusCode.BadRequest)
      {
        string result = response.Content?.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(result) && ((result.Contains("Illegal arguments in query request") && result.Contains("is not a searchable field")) || (result.Contains("Invalid expression: Could not find a property named") && (result.Contains("Parameter name: $filter") || result.Contains("Parameter name: $orderby")))))
        {
          //AzureSearchServiceRESTCallException exception = new AzureSearchServiceRESTCallException(this.IndexName, result);
          //SearchLog.Log.Warn("Search index doesn't not contain searched field", exception);
          SearchLog.Log.Warn("Search index doesn't not contain searched field", "AzureSearchServiceRESTCallException".CreateInternalAzureExceptionFromTypeName(this.IndexName, result));
          return null;
        }
      }
      this.EnsureSuccessStatusCode(response).ConfigureAwait(false).GetAwaiter().GetResult();
      HttpContent content = response.Content;
      if (content == null)
      {
        return null;
      }
      return content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    protected virtual string SerializeIndexDefinition(IndexDefinition indexDefinition)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
      };
      return JsonConvert.SerializeObject(indexDefinition, Formatting.Indented, settings);
    }

    //Sitecore.Support.227363: convert to async await
    public async virtual void UpdateIndex(IndexDefinition indexDefinition)
    {
      string str = this.SerializeIndexDefinition(indexDefinition);
      string requestUri = SearchUrl.PutIndex(this.IndexName, this.apiVersion);
      StringContent content = new StringContent(str, Encoding.UTF8, "application/json");
      HttpResponseMessage response = await this.GetClient().Put(requestUri, content, null);
      await this.EnsureSuccessStatusCode(response);
    }

    //Sitecore.Support.227363: synchronous GetIndex added for interface compatibillity as GetIndex is now async
    IndexDefinition ISearchServiceManagmentOperationsProvider.GetIndex()
    {
      return GetIndex().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public TimeSpan ClientTimeout { get; set; }

    public string IndexName { get; private set; }

    public TimeSpan ManagementOperationsTimeout { get; set; }

    public IHttpMessageObserver Observer { get; set; }
   
  }
  
}
