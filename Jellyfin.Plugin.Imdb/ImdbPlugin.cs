#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Imdb
{
    public class ImdbPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public ImdbPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static ImdbPlugin Instance { get; private set; }

        public override Guid Id => new Guid("1c203ef2-16ae-4b83-a0ab-34f865216ec3");

        public override string Name => "IMDb";

        public override string Description => "Get metadata for movies and other video content from IMDb.";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}
