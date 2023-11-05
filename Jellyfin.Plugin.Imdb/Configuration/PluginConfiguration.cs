#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Imdb
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool CastAndCrew { get; set; }
    }
}
