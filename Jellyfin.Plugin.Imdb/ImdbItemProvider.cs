#nullable disable

#pragma warning disable CS1591, SA1300

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Imdb
{
    public class ImdbItemProvider : IRemoteMetadataProvider<Series, SeriesInfo>,
        IRemoteMetadataProvider<Movie, MovieInfo>, IRemoteMetadataProvider<Episode, EpisodeInfo>, IHasOrder
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ImdbItemProvider(
    IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public string Name => "The Internet Movie Database Ratings";

        // After primary option
        public int Order => 2;

        private async Task<float?> GetImdbRating(string imdbId)
        {
            var itemUrl = $"https://www.imdb.com/title/{imdbId}";

            using (var client = _httpClientFactory.CreateClient(NamedClient.Default))
            {
                HttpResponseMessage response = await client.GetAsync(itemUrl).ConfigureAwait(false);

                // Check if the request was successful.
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Error querying IMDb URL: {itemUrl}");
                }

                // Read the response content as a string.
                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Regex rx = new Regex("<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Compiled);

                MatchCollection matches = rx.Matches(responseContent);

                if (matches.Count == 0)
                {
                    throw new InvalidOperationException("Error parsing IMDb website");
                }

                var jsonData = matches[0].Groups[1].Value;
                var imdbData = JsonSerializer.Deserialize<ImdbData>(jsonData);

                return imdbData.aggregateRating.ratingValue;
            }
        }

        private Task<float?> GetRating(SeriesInfo searchInfo)
        {
            var imdbId = searchInfo.GetProviderId(MetadataProvider.Imdb);
            if (imdbId == null)
            {
                return null;
            }

            return GetImdbRating(imdbId);
        }

        private async Task<MetadataResult<T>> GetResult<T>(ItemLookupInfo info, CancellationToken cancellationToken)
                        where T : BaseItem, new()
        {
            var result = new MetadataResult<T>
            {
                QueriedById = true,
                Item = new T(),
                HasMetadata = true
            };

            var imdbId = info.GetProviderId(MetadataProvider.Imdb);
            float? rating = await GetImdbRating(imdbId).ConfigureAwait(false);
            result.Item.CommunityRating = rating;
            return result;
        }

        public Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            return GetResult<Series>(info, cancellationToken);
        }

        public Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return GetResult<Movie>(info, cancellationToken);
        }

        public Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            return GetResult<Episode>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResultsInternal(searchInfo, true, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResultsInternal(searchInfo, true, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            return GetSearchResultsInternal(searchInfo, true, cancellationToken);
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResultsInternal(ItemLookupInfo searchInfo, bool isSearch, CancellationToken cancellationToken)
        {
            await Task.Run(() => { }, cancellationToken).ConfigureAwait(false);

            return Enumerable.Empty<RemoteSearchResult>();
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
        }

        internal class ImdbRating
        {
            public float ratingValue { get; set; }
        }

        internal class ImdbData
        {
            public ImdbRating aggregateRating { get; set; }
        }
    }
}
