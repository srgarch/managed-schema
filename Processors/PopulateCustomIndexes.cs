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
            return new List<XElement>
            {
                XElement.Parse(@"<replace-field-type>
  <name>text_gen_sort</name>
  <class>solr.SortableTextField</class>
  <positionIncrementGap>100</positionIncrementGap>
  <multiValued>true</multiValued>

  <indexAnalyzer>
    <charFilters>
      <name>patternReplace</name>
      <pattern>[\p{Punct}]</pattern>
      <replacement></replacement>
    </charFilters>

    <tokenizer>
      <name>standard</name>
    </tokenizer>

    <filters>
      <name>stop</name>
      <words>stopwords.txt</words>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <name>lowercase</name>
    </filters>
  </indexAnalyzer>

  <queryAnalyzer>
    <charFilters>
      <name>patternReplace</name>
      <pattern>[\p{Punct}]</pattern>
      <replacement></replacement>
    </charFilters>

    <tokenizer>
      <name>standard</name>
    </tokenizer>

    <filters>
      <name>stop</name>
      <words>stopwords.txt</words>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <name>synonymGraph</name>
      <synonyms>synonyms.txt</synonyms>
      <expand>true</expand>
      <ignoreCase>true</ignoreCase>
    </filters>

    <filters>
      <name>lowercase</name>
    </filters>
  </queryAnalyzer>
</replace-field-type>"),
            };
        }
    }
}