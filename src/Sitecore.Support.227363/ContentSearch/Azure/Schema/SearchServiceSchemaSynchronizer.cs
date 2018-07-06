using Sitecore.ContentSearch.Azure.Analyzers;
using Sitecore.ContentSearch.Azure.Http;
using Sitecore.ContentSearch.Azure.Models;
using Sitecore.ContentSearch.Azure.Utils.Retryer;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sitecore.Support.ContentSearch.Azure.Schema
{
  public class SearchServiceSchemaSynchronizer : Sitecore.ContentSearch.Azure.Schema.SearchServiceSchemaSynchronizer
  {
    public SearchServiceSchemaSynchronizer(ISearchServiceManagmentOperationsProvider managmentOperations, IRertyPolicy rertyPolicy, IAnalyzerRepository analyzerRepository) : base(managmentOperations, rertyPolicy, analyzerRepository)
    {

    }

    PropertyInfo indexDefinitionProperty = typeof(Sitecore.ContentSearch.Azure.Schema.SearchServiceSchemaSynchronizer).GetProperty("IndexDefinition", BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance);
    public async new Task RefreshLocalSchema()
    {
      //IndexDefinition index = this.ManagmentOperations.GetIndex();
      //this.IndexDefinition = index;

      //Sitecore.Support.227363: convert to async and set property via reflection
      Sitecore.Support.ContentSearch.Azure.Http.SearchServiceClient client = this.ManagmentOperations as Sitecore.Support.ContentSearch.Azure.Http.SearchServiceClient;
      IndexDefinition index = await client.GetIndex();
      indexDefinitionProperty.SetValue(this, index);
    }


  }
}
