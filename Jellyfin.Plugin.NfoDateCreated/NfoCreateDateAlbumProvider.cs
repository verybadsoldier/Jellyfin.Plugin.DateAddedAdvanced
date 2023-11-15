#nullable disable

#pragma warning disable CA1002, CS1591, SA1300

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.NfoDateCreated;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.NfoCreateDate
{

    public class NfoCreateDateAlbumProvider : ICustomMetadataProvider<MusicAlbum>,
        ICustomMetadataProvider<Movie>, ICustomMetadataProvider<Series>, ICustomMetadataProvider<Season>, ICustomMetadataProvider<Episode>, ICustomMetadataProvider<MusicArtist>, ICustomMetadataProvider<Audio>
    {
        private readonly ILogger _logger;

        public NfoCreateDateAlbumProvider(ILibraryManager libraryManager, ILogger<NfoCreateDateAlbumProvider> logger)
        {
            _logger = logger;
        }

        public string Name => "NFO Create Date";

        private Task<ItemUpdateType> FetchAsyncInternal(string xmlpath, BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (!File.Exists(xmlpath))
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            _logger.LogInformation($"Found xml file: {xmlpath}");


            string? dateadded = NfoReader.ReadDateAdded(xmlpath);

            if (dateadded == null)
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            DateTime newDate;
            if (!DateTime.TryParse(dateadded, out newDate))
            {
                _logger.LogError($"Error parsing createddata: {dateadded}");
                return Task.FromResult(ItemUpdateType.None);
            }

            if (item.DateCreated != newDate)
            {
                item.DateCreated = newDate;
                return Task.FromResult(ItemUpdateType.MetadataEdit);
            }
            else
            {
                return Task.FromResult(ItemUpdateType.None);
            }
        }

        public Task<ItemUpdateType> FetchAsync(MusicAlbum item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(item.Path, "album.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(item.Path, "movie.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(item.Path, "tvshow.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string filename;
            if (Plugin.Instance.Configuration.UseSeasonDateForEpisodes)
            {
                filename = Path.Combine(Path.GetDirectoryName(item.Path), "season.nfo");
            }
            else
            {
                filename = Path.Combine(Path.GetFileName(item.Path), "nfo");
            }

            return FetchAsyncInternal(filename, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(MusicArtist item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(item.Path, "artist.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Audio item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(Path.GetDirectoryName(item.Path) , "album.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Season item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            string xmlpath = Path.Combine(item.Path, "season.nfo");
            return FetchAsyncInternal(xmlpath, item, options, cancellationToken);
        }
    }

}
