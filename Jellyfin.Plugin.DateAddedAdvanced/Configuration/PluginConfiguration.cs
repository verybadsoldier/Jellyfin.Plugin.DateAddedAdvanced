using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Options;

namespace MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration
{
    /// <summary>
    /// Plugin configuration class for the studio image provider.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            UseSeasonDateForEpisodes = true;
            DateCreatedSource = DateSource.Oldest;
            UpdateExistingNfos = false;
            WriteArtistNfo = false;
            WriteAlbumNfo = true;
            WriteSeasonNfo = true;
            WriteTvShowNfo = false;
            WriteEpisodeNfo = false;
            WriteMovieNfo = true;
        }

        public enum DateSource
        {
            Modifed,
            Created,
            Oldest,
            Newest,
        }

        /// <summary>
        /// Gets or sets a value indicating whether createdate for seasons should be read from season.nfo.
        /// </summary>
        public bool UseSeasonDateForEpisodes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether existing nfo files should be updated.
        /// </summary>
        public bool UpdateExistingNfos { get; set; }

        public DateSource DateCreatedSource { get; set; }

        public bool WriteArtistNfo { get; set; }

        public bool WriteAlbumNfo { get; set; }

        public bool WriteSeasonNfo { get; set; }

        public bool WriteTvShowNfo { get; set; }

        public bool WriteEpisodeNfo { get; set; }

        public bool WriteMovieNfo { get; set; }
    }
}
