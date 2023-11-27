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
using System.Xml;
using Jellyfin.Extensions.Json;
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

namespace Jellyfin.Plugin.DateAddedAdvanced
{
    public class NfoCreateDateProvider : ICustomMetadataProvider<MusicAlbum>,
        ICustomMetadataProvider<Movie>, ICustomMetadataProvider<Series>, ICustomMetadataProvider<Season>, ICustomMetadataProvider<Episode>, ICustomMetadataProvider<MusicArtist>, ICustomMetadataProvider<Audio>
    {
        private readonly ILogger _logger;
        private readonly DateHelper _dateHelper;

        public NfoCreateDateProvider(ILibraryManager libraryManager, ILogger<NfoCreateDateProvider> logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _dateHelper = new DateHelper(fileSystem, logger);
        }

        public string Name => "NFO Create Date";

        public static string ReadDateAdded(string filepath, string rootname)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            XmlNode node = doc.DocumentElement.SelectSingleNode($"/{rootname}/dateadded");
            if (node == null)
            {
                return null;
            }

            return node.InnerText;
        }

        private Task<ItemUpdateType> FetchAsyncInternal(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (item.Path == null)
            {
                string additionalInfo = string.Join(",", item.PhysicalLocations);
                if (item is Season)
                {
                    additionalInfo = ((Season) item).SeriesName;
                }
                else if (item is Series)
                {
                    additionalInfo = ((Series)item).OriginalTitle;
                }

                _logger.LogError("Item.Path is null. This is suspicious. Name: {Name} - Item: {Item}. More Info: {Info}", item.Name, item.ToString(), additionalInfo);
                return Task.FromResult(ItemUpdateType.None);
            }

            string xmlpath = PathResolver.GetXmlPathInfoForItem(item, true);

            if (string.IsNullOrEmpty(xmlpath))
            {
                _logger.LogWarning("Could not get xml path for item: {Item}", item.GetType().Name);
                return Task.FromResult(ItemUpdateType.None);
            }

            DateTime? newDate;

            if (string.IsNullOrEmpty(xmlpath) || !File.Exists(xmlpath))
            {
                _logger.LogInformation("No XML file available: {XmlPath}. Using DateCreated from file attributes according to config", xmlpath);

                newDate = _dateHelper.ResolveDateCreatedFromFile(item);
            }
            else
            {
                _logger.LogInformation("Found xml file: {XmlPath}", xmlpath);

                string rootname = PathResolver.GetXmlRootNodeName(item);

                if (string.IsNullOrEmpty(rootname))
                {
                    _logger.LogWarning("Could not get xml root node namme for item: {Item}", item.GetType().Name);
                    return Task.FromResult(ItemUpdateType.None);
                }

                string dateadded = ReadDateAdded(xmlpath, rootname);

                if (dateadded == null)
                {
                    return Task.FromResult(ItemUpdateType.None);
                }

                DateTime parsedDate;
                if (!DateTime.TryParse(dateadded, out parsedDate))
                {
                    _logger.LogError("Error parsing createddata: {DateAdded}", dateadded);
                    return Task.FromResult(ItemUpdateType.None);
                }

                newDate = parsedDate;
            }

            if (item.DateCreated != newDate && newDate != null)
            {
                item.DateCreated = newDate.Value;
                return Task.FromResult(ItemUpdateType.MetadataEdit);
            }
            else
            {
                return Task.FromResult(ItemUpdateType.None);
            }
        }

        public Task<ItemUpdateType> FetchAsync(MusicAlbum item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(MusicArtist item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Audio item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }

        public Task<ItemUpdateType> FetchAsync(Season item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            return FetchAsyncInternal(item, options, cancellationToken);
        }
    }
}
