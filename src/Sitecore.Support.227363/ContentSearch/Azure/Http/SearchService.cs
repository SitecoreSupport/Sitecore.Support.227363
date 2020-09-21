using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Azure;
using Sitecore.ContentSearch.Azure.Http;
using Sitecore.ContentSearch.Azure.Http.Exceptions;
using Sitecore.ContentSearch.Azure.Models;
using Sitecore.ContentSearch.Azure.Schema;
using Sitecore.ContentSearch.Diagnostics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sitecore.Support.ContentSearch.Azure.Http
{
  internal class SearchService : ISearchService, IProvideAvailabilityManager, ISearchServiceConnectionInitializable, ISearchIndexInitializable, IDisposable, ISearchServiceSchemaSyncNotification
  {
    private CloudSearchProviderIndex searchIndex;

    private Timer timer;

    private ICloudSearchIndexSchema schema;

    [method: CompilerGenerated]
    [CompilerGenerated]
    public event EventHandler SchemaSynced;

    public string Name
    {
      get;
      private set;
    }

    public ISearchServiceAvailabilityManager AvailabilityManager
    {
      get;
      set;
    }

    public ISearchServiceDocumentOperationsProvider DocumentOperations
    {
      get;
      set;
    }

    public ISearchServiceSchemaSynchronizer SchemaSynchronizer
    {
      get;
      set;
    }

    public ICloudSearchIndexSchema Schema
    {
      get
      {
        return this.schema;
      }
      private set
      {
        this.schema = value;
      }
    }

    public SearchService(ISearchServiceAvailabilityManager availabilityManager, ISearchServiceDocumentOperationsProvider documentOperations, ISearchServiceSchemaSynchronizer schemaSynchronizer, string schemaUpdateInterval)
    {
      this.AvailabilityManager = availabilityManager;
      this.DocumentOperations = documentOperations;
      this.SchemaSynchronizer = schemaSynchronizer;
      this.timer = new Timer(new TimerCallback(this.SyncSchema), this, TimeSpan.FromSeconds(2.0), TimeSpan.Parse(schemaUpdateInterval));
    }

    private async void SyncSchema(object state)
    {
      try
      {
        var synchronizer = this.SchemaSynchronizer as Sitecore.Support.ContentSearch.Azure.Schema.SearchServiceSchemaSynchronizer;
        await synchronizer.RefreshLocalSchema();
        //Interlocked.Exchange<ICloudSearchIndexSchema>(ref this.schema, new CloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot.ToList<IndexedField>()));
        Interlocked.Exchange<ICloudSearchIndexSchema>(ref this.schema, Extensions.CreateCloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot.ToList<IndexedField>()));
        this.OnSchemaSynced(EventArgs.Empty);
      }
      catch (Exception exception)
      {
        SearchLog.Log.Info("Schema synchronization failed", exception);
      }
    }

    public IndexStatistics GetStatistics()
    {
      return this.SchemaSynchronizer.ManagmentOperations.GetIndexStatistics();
    }

    public void PostDocuments(ICloudBatch batch)
    {
      try
      {
        this.PostDocumentsImpl(batch);
      }
      //catch (NotFoundException)
      catch (Exception e)
      {
        if(!e.GetType().Equals("NotFoundException".CreateInternalAzureExceptionFromTypeName(e.Message, e).GetType()))
        {
          throw;
        }
        this.SchemaSynchronizer.RefreshLocalSchema();
        this.PostDocumentsImpl(batch);
      }
    }

    private void PostDocumentsImpl(ICloudBatch batch)
    {
      ICloudSearchIndexSchemaBuilder expr_0B = this.searchIndex.SchemaBuilder;
      ICloudSearchIndexSchema cloudSearchIndexSchema = (expr_0B != null) ? expr_0B.GetSchema() : null;
      if (cloudSearchIndexSchema != null)
      {
        this.SchemaSynchronizer.EnsureIsInSync(cloudSearchIndexSchema.AllFields);
        //this.Schema = new CloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot);
        this.Schema = Extensions.CreateCloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot);
      }
      if (!this.AvailabilityManager.CanWrite)
      {
        string message = string.Format("The service ${0} is not available for write operations", this.Name);
        //throw new SearchServiceIsUnavailableException(this.searchIndex.CloudIndexName, message, null);
        throw "SearchServiceIsUnavailableException".CreateInternalAzureExceptionFromTypeName(this.searchIndex.CloudIndexName, message, null);
      }
      string json = batch.GetJson();
      this.DocumentOperations.PostDocuments(json);
    }

    public string Search(string expression)
    {
      if (!this.AvailabilityManager.CanRead)
      {
        string message = string.Format("The service ${0} is not available for read operations", this.Name);
        //throw new SearchServiceIsUnavailableException(this.searchIndex.CloudIndexName, message, null);
        throw "SearchServiceIsUnavailableException".CreateInternalAzureExceptionFromTypeName(this.searchIndex.CloudIndexName, message, null);
      }
      return this.DocumentOperations.Search(expression);
    }

    public void Cleanup()
    {
      if (this.SchemaSynchronizer.ManagmentOperations.IndexExists())
      {
        this.SchemaSynchronizer.ManagmentOperations.DeleteIndex();
      }
      this.SchemaSynchronizer.CleaupLocalSchema();
    }

    public virtual void Initialize(ISearchIndex index)
    {
      this.searchIndex = (index as CloudSearchProviderIndex);
      if (this.searchIndex == null)
      {
        throw new NotSupportedException(string.Format("Only {0} is supported", typeof(CloudSearchProviderIndex).Name));
      }
      this.DocumentOperations.Observer = (this.AvailabilityManager as IHttpMessageObserver);
      this.SchemaSynchronizer.ManagmentOperations.Observer = (this.AvailabilityManager as IHttpMessageObserver);
    }

    public void Initialize(string indexName, string connectionString)
    {
      ISearchServiceConnectionInitializable expr_0B = this.DocumentOperations as ISearchServiceConnectionInitializable;
      if (expr_0B != null)
      {
        expr_0B.Initialize(indexName, connectionString);
      }
      ISearchServiceConnectionInitializable expr_23 = this.SchemaSynchronizer as ISearchServiceConnectionInitializable;
      if (expr_23 != null)
      {
        expr_23.Initialize(indexName, connectionString);
      }
      CloudSearchServiceSettings cloudSearchServiceSettings = new CloudSearchServiceSettings(connectionString);
      this.Name = cloudSearchServiceSettings.SearchService;
      this.SchemaSynchronizer.EnsureIsInitialized();
      //this.Schema = new CloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot);
      this.Schema = Extensions.CreateCloudSearchIndexSchema(this.SchemaSynchronizer.LocalSchemaSnapshot);
    }

    public void Dispose()
    {
      Timer expr_0C = Interlocked.Exchange<Timer>(ref this.timer, null);
      if (expr_0C != null)
      {
        expr_0C.Dispose();
      }
      if (this.SchemaSynced == null)
      {
        return;
      }
      Delegate[] invocationList = this.SchemaSynced.GetInvocationList();
      for (int i = 0; i < invocationList.Length; i++)
      {
        EventHandler value = (EventHandler)invocationList[i];
        this.SchemaSynced -= value;
      }
    }

    protected virtual void OnSchemaSynced(EventArgs args)
    {
      EventHandler expr_06 = this.SchemaSynced;
      if (expr_06 == null)
      {
        return;
      }
      expr_06(this, args);
    }
  }
}
