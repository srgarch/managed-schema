using Sitecore.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Sitecore.ContentSearch.SolrNetExtension;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.Pipelines.PopulateSolrSchema;
using Sitecore.ContentSearch.SolrProvider.SolrNetIntegration;
using Sitecore.StringExtensions;

namespace SergMedium.ManagedSchemaExample.Processors
{
    public class PopulateCustomIndexes : PopulateManagedSchemaProcessor
    {
        public override void Process(PopulateManagedSchemaArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.IsNotNull(args.Indexes, "Indexes");
            var solrIndexCoreMap = args.SolrIndexCoreMap;
            if (solrIndexCoreMap != null)
            {
                PopulateSolrCores(solrIndexCoreMap);
            }
        }

        private void PopulateSolrCores(IReadOnlyDictionary<SolrSearchIndex, string[]> indexCoreMap)
        {
            if (indexCoreMap.Count == 0)
            {
                return;
            }

            var processedCores = new ConcurrentDictionary<string, Exception>();
            var key = indexCoreMap.Keys.FirstOrDefault(x => x.Name == "sitecore_marketingdefinitions_master");

            if (key == null) return;

            var solrCores = indexCoreMap[key];
            if (solrCores == null || solrCores.Length == 0) return;

            var solrOperationsFactory = CommonServiceLocator.ServiceLocator.Current.GetInstance<BaseSolrOperationsFactory<Dictionary<string, object>>>();
            ExecuteSafe(() =>
            {
                foreach (var text in solrCores)
                {
                    if (text.IsNullOrEmpty() || !processedCores.TryAdd(text, null)) continue;
                    PopulateCore(solrOperationsFactory.GetSolrOperations(text));
                }
            }, out _);
        }

        public void ExecuteSafe(Action action, out Exception exception)
        {
            Assert.ArgumentNotNull(action, "action");
            try
            {
                action();
                exception = null;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        private void PopulateCore(ISolrSchemaProvider schemaOperations)
        {
            var fieldList = GetAllFields();
            if (fieldList.Any())
            {
                schemaOperations.UpdateSchema(fieldList);
            }
        }

        private List<XElement> GetAllFields()
        {
            var text = @"<replace-field-type xmlns:json=""http://james.newtonking.com/projects/json"">
  <name>text_general</name>
  <class>solr.TextField</class>
  <positionIncrementGap>100</positionIncrementGap>
  <multiValued>false</multiValued>

  <indexAnalyzer>
    <charFilters json:Array=""true"">
      <class>solr.PatternReplaceCharFilterFactory</class>
      <pattern>[\p{Punct}]</pattern>
      <replacement></replacement>
    </charFilters>

    <tokenizer>
      <class>solr.StandardTokenizerFactory</class>
    </tokenizer>

    <filters>
      <class>solr.StopFilterFactory</class>
      <words>stopwords.txt</words>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <class>solr.LowerCaseFilterFactory</class>
    </filters>
  </indexAnalyzer>

  <queryAnalyzer>
    <charFilters json:Array=""true"">
      <class>solr.PatternReplaceCharFilterFactory</class>
      <pattern>[\p{Punct}]</pattern>
      <replacement></replacement>
    </charFilters>

    <tokenizer>
      <class>solr.StandardTokenizerFactory</class>
    </tokenizer>

    <filters>
      <class>solr.StopFilterFactory</class>
      <words>stopwords.txt</words>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <class>solr.SynonymGraphFilterFactory</class>
      <synonyms>synonyms.txt</synonyms>
      <expand>true</expand>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <class>solr.LowerCaseFilterFactory</class>
    </filters>
  </queryAnalyzer>
</replace-field-type>";
            return new List<XElement>
            {
                XElement.Parse(text),
            };
        }
    }
}