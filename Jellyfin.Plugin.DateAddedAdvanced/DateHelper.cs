using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTimeStamp);
        }

        private static DateTime? GetLinuxCreationTime(string filepath)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "stat",
                Arguments = $"--format=%W \"{filepath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    if (long.TryParse(output, out long birthTimeUnix))
                    {
                        if (birthTimeUnix == 0)
                        {
                            return null;
                        }

                        return DateTimeOffset.FromUnixTimeSeconds(birthTimeUnix).DateTime;
                    }
                }
            }

            throw new InvalidOperationException("Failed to retrieve birth time");
        }

        private DateTime GetDateCreatedFromFile(FileSystemMetadata metaData, BaseItem item)
        {
            var write = _fileSystem.GetLastWriteTimeUtc(metaData);
            DateTime create = default;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Jellyfin does report wrong created timestamps on Linux as reported here: https://github.com/jellyfin/jellyfin/issues/10655
                // So we use Linux' command line tool "stat" to get proper created timestamps - not all filesystems support created timestamps tho
                DateTime? linuxCreate = GetLinuxCreationTime(metaData.FullName);
                if (linuxCreate.HasValue)
                {
                    create = linuxCreate.Value;
                }
                else
                {
                    _logger.LogError("Could not acquire creation time of filepath: {Filepath}. Defaulting to 000", metaData.FullName);
                }
            }
            else
            {
                create = _fileSystem.GetCreationTimeUtc(metaData);
            }

            DateTime dt = DateTime.MinValue;
            DateSource source = item is Video ? Plugin.Instance.Configuration.DateAddedSourceVideo : Plugin.Instance.Configuration.DateAddedSourceAudio;
            switch (source)
            {
                case DateSource.Modified:
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

            _logger.LogInformation("DateSource: {Source} - Value: {Value}. Path: {Path}", source, dt, item.Path);

            return dt;
        }

        public DateTime? ResolveDateCreatedFromFile(BaseItem item)
        {
            DateTime? datetime = null;
            if (item is Movie)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                return GetDateCreatedFromFile(meta, item);
            }
            else if (item is Series)
            {
                Series series = (Series)item;
                foreach (var d in series.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (datetime == null || childdate < datetime))
                    {
                        datetime = childdate;
                    }
                }
            }
            else if (item is Season)
            {
                Season season = (Season)item;
                foreach (var d in season.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (datetime == null || childdate < datetime))
                    {
                        datetime = childdate;
                    }
                }
            }
            else if (item is Episode)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                return GetDateCreatedFromFile(meta, item);
            }
            else if (item is MusicAlbum)
            {
                MusicAlbum album = (MusicAlbum)item;
                foreach (var d in album.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (datetime == null || childdate < datetime))
                    {
                        datetime = childdate;
                    }
                }
            }
            else if (item is Folder)
            {
                Folder folder = (Folder)item;
                foreach (var d in folder.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (datetime == null || childdate < datetime))
                    {
                        datetime = childdate;
                    }
                }
            }
            else if (item is Audio)
            {
                var meta = _fileSystem.GetFileInfo(item.Path);
                datetime = GetDateCreatedFromFile(meta, item);
            }
            else if (item is MusicArtist)
            {
                MusicArtist artist = (MusicArtist)item;
                foreach (var d in artist.Children)
                {
                    var childdate = ResolveDateCreatedFromFile(d);
                    if (childdate != null && (datetime == null || childdate < datetime))
                    {
                        datetime = childdate;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Unsupported item type: {Type}", item.GetType().Name);
            }

            return datetime;
        }
    }
}
