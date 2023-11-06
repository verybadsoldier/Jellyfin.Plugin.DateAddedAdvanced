using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using static MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration.PluginConfiguration;

namespace Jellyfin.Plugin.DateAddedAdvanced
{
    internal class DateHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public DateHelper(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        private DateTime GetDateCreatedFromFile(FileSystemMetadata metaData)
        {
            var write = _fileSystem.GetLastWriteTimeUtc(metaData);
            var create = _fileSystem.GetCreationTimeUtc(metaData);

            DateTime dt = DateTime.MinValue;
            switch (Plugin.Instance.Configuration.DateCreatedSource)
            {
                case DateSource.Modifed:
                    dt = write;
                    break;
                case DateSource.Created:
                    dt = create;
                    break;
                case DateSource.Newest:
                    dt = write > create ? write : create;
                    break;
                case DateSource.Oldest:
                    dt = write < create ? write : create;
                    break;
            }

            return dt;
        }

        public DateTime? ResolveDateCreatedFromFile(BaseItem item)
        {
            DateTime? t = null;
            if (item is Movie)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                return GetDateCreatedFromFile(meta);
            }
            else if (item is Series)
            {
                Series series = (Series)item;
                foreach (var d in series.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (t == null || childdate < t))
                    {
                        t = childdate;
                    }
                }
            }
            else if (item is Season)
            {
                Season season = (Season)item;
                foreach (var d in season.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (t == null || childdate < t))
                    {
                        t = childdate;
                    }
                }
            }
            else if (item is Episode)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                return GetDateCreatedFromFile(meta);
            }
            else if (item is MusicAlbum)
            {
                MusicAlbum album = (MusicAlbum)item;
                foreach (var d in album.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (t == null || childdate < t))
                    {
                        t = childdate;
                    }
                }
            }
            else if (item is Audio)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                t = GetDateCreatedFromFile(meta);
            }
            else if (item is MusicArtist)
            {
                MusicArtist artist = (MusicArtist)item;
                foreach (var d in artist.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (t == null || childdate < t))
                    {
                        t = childdate;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unsupported item type: {Type}", item.GetType().Name);
            }

            return t;
        }
    }
}
