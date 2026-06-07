using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Sitecore.ContentSearch.SolrProvider.Abstractions;
using Sitecore.ContentSearch.SolrProvider.Pipelines.PopulateSolrSchema;
using SolrNet.Schema;

namespace SergMedium.ManagedSchemaExample.Processors
{
    public class CustomPopulateHelperFactory : IPopulateHelperFactory
    {
        public ISchemaPopulateHelper GetPopulateHelper(SolrSchema solrSchema)
        {
            return new CustomPopulateHelper(solrSchema);
        }
    }

    public class CustomPopulateHelper : SchemaPopulateHelper
    {
        public CustomPopulateHelper(SolrSchema solrSchema) : base(solrSchema)
        {
        }

        public override IEnumerable<XElement> GetAllFieldTypes()
        {
            var types = base.GetAllFieldTypes().ToArray();
            var resultingTypes = new List<XElement>();

            foreach (var type in types)
            {
                // Skip field types with single charFilter, as they are processed incorrectly by XML to JSON conversion, and most probably it is a custom case that is handled manually.
                var skipType = type.Descendants("charFilters").Any(charFilter =>
                    charFilter.Parent?.Elements(charFilter.Name).Count() == 1);

                if (skipType)
                {
                    continue;
                }

                resultingTypes.Add(type);
            }

            return resultingTypes;
        }
    }
}