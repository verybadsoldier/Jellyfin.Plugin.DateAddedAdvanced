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
        }

        /// <summary>
        /// Gets or sets a value indicating whether createdate for seasons should be read from season.nfo
        /// </summary>
        public bool UseSeasonDateForEpisodes { get; set; }
    }
}
