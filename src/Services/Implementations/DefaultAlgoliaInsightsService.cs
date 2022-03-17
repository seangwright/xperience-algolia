﻿using Algolia.Search.Clients;
using Algolia.Search.Models.Insights;
using Algolia.Search.Models.Search;

using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterImplementation(typeof(IAlgoliaInsightsService), typeof(DefaultAlgoliaInsightsService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaInsightsService"/> which logs
    /// Algolia Insights events using the <see cref="ContactInfo.ContactGUID"/>
    /// as the user's identifier.
    /// </summary>
    public class DefaultAlgoliaInsightsService : IAlgoliaInsightsService
    {
        private readonly IAlgoliaRegistrationService algoliaRegistrationService;
        private readonly IInsightsClient insightsClient;
        private const string parameterNameObjectId = "object";
        private const string parameterNameQueryId = "query";
        private const string parameterNamePosition = "pos";


        private string ContactGUID
        {
            get
            {
                var currentContact = ContactManagementContext.CurrentContact;
                if (currentContact == null)
                {
                    return string.Empty;
                }

                return currentContact.ContactGUID.ToString();
            }
        }


        private string ObjectId
        {
            get
            {
                return QueryHelper.GetString(parameterNameObjectId, "");
            }
        }


        private string QueryId
        {
            get
            {
                return QueryHelper.GetString(parameterNameQueryId, "");
            }
        }


        private uint Position
        {
            get
            {
                return (uint)QueryHelper.GetInteger(parameterNamePosition, 0);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaInsightsService"/> class.
        /// </summary>
        public DefaultAlgoliaInsightsService(IAlgoliaRegistrationService algoliaRegistrationService, IInsightsClient insightsClient)
        {
            this.algoliaRegistrationService = algoliaRegistrationService;
            this.insightsClient = insightsClient;
        }


        public async Task<InsightsResponse> LogSearchResultClicked(string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || Position <= 0)
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ClickedObjectIDsAfterSearchAsync(eventName, indexName, new string[] { ObjectId }, new uint[] { Position }, QueryId);
        }


        public async Task<InsightsResponse> LogSearchResultConversion(string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName))
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ConvertedObjectIDsAfterSearchAsync(conversionName, indexName, new string[] { ObjectId }, QueryId);
        }


        public async Task<InsightsResponse> LogPageViewed(int documentId, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || documentId <= 0)
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ViewedObjectIDsAsync(eventName, indexName, new string[] { documentId.ToString() });
        }


        public async Task<InsightsResponse> LogPageConversion(int documentId, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName) || documentId <= 0)
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ConvertedObjectIDsAsync(conversionName, indexName, new string[] { documentId.ToString() });
        }


        public async Task<InsightsResponse> LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || facets == null)
            {
                return null;
            }

            var viewedFacets = new List<string>();
            foreach(var facetedAttribute in facets)
            {
                viewedFacets.AddRange(facetedAttribute.Facets.Select(facet => $"{facet.Attribute}:{facet.Value}"));
            }

            if (viewedFacets.Count > 0)
            {
                return await insightsClient.User(ContactGUID).ViewedFiltersAsync(eventName, indexName, viewedFacets);
            }

            return null;
        }


        public async Task<InsightsResponse> LogFacetClicked(string facet, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(eventName) || String.IsNullOrEmpty(indexName))
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ClickedFiltersAsync(eventName, indexName, new string[] { facet });
        }


        public async Task<InsightsResponse> LogFacetConverted(string facet, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(conversionName) || String.IsNullOrEmpty(indexName))
            {
                return null;
            }

            return await insightsClient.User(ContactGUID).ConvertedFiltersAsync(conversionName, indexName, new string[] { facet });
        }


        public void SetInsightsUrls<TModel>(SearchResponse<TModel> searchResponse) where TModel : AlgoliaSearchModel
        {
            for (var i = 0; i < searchResponse.Hits.Count; i++)
            {
                var position = i + 1 + (searchResponse.HitsPerPage * searchResponse.Page);
                searchResponse.Hits[i].Url = GetInsightsUrl(searchResponse.Hits[i], position, searchResponse.QueryID);
            }
        }


        /// <summary>
        /// Gets the Algolia hit's absolute URL with the appropriate query string parameters
        /// populated to log search result click events.
        /// </summary>
        /// <typeparam name="TModel">The type of the Algolia search model.</typeparam>
        /// <param name="hit">The Aloglia hit to retrieve the URL for.</param>
        /// <param name="position">The position the <paramref name="hit"/> appeared in the
        /// search results.</param>
        /// <param name="queryId">The unique identifier of the Algolia query.</param>
        protected string GetInsightsUrl<TModel>(TModel hit, int position, string queryId) where TModel : AlgoliaSearchModel
        {
            var indexName = "";
            foreach (var index in algoliaRegistrationService.RegisteredIndexes)
            {
                if (index.Value == typeof(TModel))
                {
                    indexName = index.Key;
                }
            }

            if (string.IsNullOrEmpty(indexName))
            {
                return hit.Url;
            }

            var url = hit.Url;
            url = URLHelper.AddParameterToUrl(url, parameterNameObjectId, hit.ObjectID);
            url = URLHelper.AddParameterToUrl(url, parameterNamePosition, position.ToString());
            url = URLHelper.AddParameterToUrl(url, parameterNameQueryId, queryId);

            return url;
        }
    }
}
