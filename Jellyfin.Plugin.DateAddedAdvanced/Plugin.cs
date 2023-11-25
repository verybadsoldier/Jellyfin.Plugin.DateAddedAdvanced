#nullable disable

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DateAddedAdvanced
{
    /// <summary>
    /// Artwork Plugin class.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">application paths.</param>
        /// <param name="xmlSerializer">xml serializer.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the instance of Artwork plugin.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <inheritdoc/>
        public override Guid Id => new Guid("c31ce313-d3d3-4a93-ad6d-8b235a9c2078");

        /// <inheritdoc/>
        public override string Name => "DateAdded Advanced";

        /// <inheritdoc/>
        public override string Description => "Sets DateAdded from different file attributes and read and writes it to NFO files";

        /// <inheritdoc/>
        public override string ConfigurationFileName => "Jellyfin.Plugin.DateAddedAdvanced.xml";

        /// <inheritdoc/>
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
