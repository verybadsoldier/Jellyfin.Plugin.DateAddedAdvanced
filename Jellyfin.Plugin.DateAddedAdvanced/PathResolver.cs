using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.DateAddedAdvanced
{
    public class PathResolver
    {
        private static string GetMovieSavePath(ItemInfo item)
        {
            if (item.VideoType == VideoType.Dvd && !item.IsPlaceHolder)
            {
                var path = item.ContainingFolderPath;

                return Path.Combine(path, "VIDEO_TS", "VIDEO_TS.nfo");
            }

            if (!item.IsPlaceHolder && (item.VideoType == VideoType.Dvd || item.VideoType == VideoType.BluRay))
            {
                var path = item.ContainingFolderPath;

                return Path.Combine(path, Path.GetFileName(path) + ".nfo");
            }
            else
            {
                // only allow movie object to read movie.nfo, not owned videos (which will be itemtype video, not movie)
                if (!item.IsInMixedFolder && item.ItemType == typeof(Movie))
                {
                    return Path.Combine(item.ContainingFolderPath, "movie.nfo");
                }

                return Path.ChangeExtension(item.Path, ".nfo");
            }
        }

        public static string? GetXmlRootNodeName(BaseItem item)
        {
            if (item is Movie)
            {
                return "movie";
            }
            else if (item is Series)
            {
                return "tvshow";
            }
            else if (item is Season)
            {
                return "season";
            }
            else if (item is Episode)
            {
                if (Plugin.Instance.Configuration.UseSeasonDateForEpisodes)
                {
                    return "season";
                }
                else
                {
                    return "episodedetails";
                }
            }
            else if (item is MusicAlbum)
            {
                return "album";
            }
            else if (item is MusicArtist)
            {
                return "artist";
            }
            else if (item is Audio)
            {
                return "album";
            }
            else
            {
                return null;
            }
        }

        public static string GetXmlPathInfoForItem(BaseItem item, bool modeRead)
        {
            string xmlFilePath = string.Empty;
            if (item is Movie)
            {
                xmlFilePath = GetMovieSavePath(new ItemInfo(item));
            }
            else if (item is Series)
            {
                xmlFilePath = Path.Combine(item.Path, "tvshow.nfo");
            }
            else if (item is Season)
            {
                xmlFilePath = Path.Combine(item.Path, "season.nfo");
            }
            else if (item is Episode)
            {
                if (modeRead && Plugin.Instance.Configuration.UseSeasonDateForEpisodes)
                {
                    var dir = Path.GetDirectoryName(item.Path);
                    if (dir == null)
                    {
                        throw new ArgumentException("Episode Path did not contain a path: {Path}", item.Path);
                    }

                    xmlFilePath = Path.Combine(dir, "season.nfo");
                }
                else
                {
                    xmlFilePath = Path.ChangeExtension(item.Path, "nfo");
                }
            }
            else if (item is MusicAlbum)
            {
                xmlFilePath = Path.Combine(item.Path, "album.nfo");
            }
            else if (item is MusicArtist)
            {
                xmlFilePath = Path.Combine(item.Path, "artist.nfo");
            }
            else if (item is Audio)
            {
                var dir = Path.GetDirectoryName(item.Path);
                if (dir == null)
                {
                    throw new ArgumentException("Episode Path did not contain a path: {Path}", item.Path);
                }

                xmlFilePath = Path.Combine(dir, "album.nfo");
            }

            return xmlFilePath;
        }
    }
}
