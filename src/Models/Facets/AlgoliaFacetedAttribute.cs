﻿namespace Kentico.Xperience.AlgoliaSearch.Models.Facets
{
    /// <summary>
    /// Represents an Algolia faceted attribute.
    /// </summary>
    public class AlgoliaFacetedAttribute
    {
        /// <summary>
        /// The camel-cased code name of the faceted attribute.
        /// </summary>
        public string Attribute { get; set; }


        /// <summary>
        /// The display name of the faceted attribute.
        /// </summary>
        public string DisplayName { get; set; }


        /// <summary>
        /// Available facets of the faceted attibute.
        /// </summary>
        public AlgoliaFacet[] Facets { get; set; }
    }
}
