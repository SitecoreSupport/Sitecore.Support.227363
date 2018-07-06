using Newtonsoft.Json.Linq;
using Sitecore.ContentSearch.Azure.Http.Exceptions;
using Sitecore.ContentSearch.Azure.Models;
using Sitecore.ContentSearch.Azure.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web;

namespace Sitecore.Support.ContentSearch.Azure.Http
{

  //Sitecore.Support.227363: reflection based workarounds to create instances of internal classes
  internal static class Extensions
  {
    private const string exceptionNamespacePrefix = "Sitecore.ContentSearch.Azure.Http.Exceptions.";
    public static Assembly azureSearchAssembly = typeof(SearchIndexCreationException).Assembly;

    private static Type multiStatusResponseDocumentType = azureSearchAssembly.GetType(exceptionNamespacePrefix + "MultiStatusResponseDocument");
    private static Type cloudSearchIndexSchemaType = azureSearchAssembly.GetType("Sitecore.ContentSearch.Azure.Schema.CloudSearchIndexSchema");

    public static PropertyInfo statusProp = multiStatusResponseDocumentType.GetProperty("Status", BindingFlags.NonPublic | BindingFlags.Instance);

    private static PropertyInfo keyProp = multiStatusResponseDocumentType.GetProperty("Key", BindingFlags.NonPublic | BindingFlags.Instance);
    private static PropertyInfo messageProp = multiStatusResponseDocumentType.GetProperty("Message", BindingFlags.NonPublic | BindingFlags.Instance);
    private static PropertyInfo statusCodeProp = multiStatusResponseDocumentType.GetProperty("StatusCode", BindingFlags.NonPublic | BindingFlags.Instance);


    public static Exception CreateInternalAzureExceptionFromTypeName(this string type, params object[] parameters)
    {
      Type exceptionType = azureSearchAssembly.GetType(exceptionNamespacePrefix + type);
      return Activator.CreateInstance(exceptionType, parameters) as Exception;
    }

    public static IEnumerable<object> GetMultiStatusDocuments(this HttpResponseMessage response)
    {
      if (response.StatusCode != ((HttpStatusCode)0xcf))
      {
        throw new ArgumentException("Response is not a Multi-Status");
      }
      return (from t in JObject.Parse(response.Content.ReadAsStringAsync().Result).SelectToken("value")
              select CreateMultiStatusResponseDocument(t));
      //select new MultiStatusResponseDocument
      //{
      //  Key = t["key"].Value<string>(),
      //  Message = t["errorMessage"].Value<string>(),
      //  StatusCode = t["statusCode"].Value<int>(),
      //  Status = t["status"].Value<bool>()
      //});
    }

    private static object CreateMultiStatusResponseDocument(JToken token)
    {
      object document = Activator.CreateInstance(multiStatusResponseDocumentType, true);
      keyProp.SetValue(document, token["key"].Value<string>());
      messageProp.SetValue(document, token["errorMessage"].Value<string>());
      statusCodeProp.SetValue(document, token["statusCode"].Value<int>());
      statusProp.SetValue(document, token["status"].Value<bool>());
      return document;
    }

    public static ICloudSearchIndexSchema CreateCloudSearchIndexSchema(IEnumerable<IndexedField> fields)
    {
      return Activator.CreateInstance(cloudSearchIndexSchemaType, new object[] { fields }) as ICloudSearchIndexSchema;
    }
  }
}