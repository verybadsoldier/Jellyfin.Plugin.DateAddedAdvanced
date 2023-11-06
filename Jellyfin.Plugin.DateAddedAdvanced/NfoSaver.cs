using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using static MediaBrowser.Providers.Plugins.NfoCreateDate.Configuration.PluginConfiguration;

namespace Jellyfin.Plugin.DateAddedAdvanced
{
    public class NfoSaver : IMetadataFileSaver
    {
        private readonly IFileSystem _fileSystem;
        private readonly DateHelper _dateHelper;
        private readonly ILogger _logger;

        public NfoSaver(IFileSystem fileSystem, ILogger<NfoSaver> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _dateHelper = new DateHelper(fileSystem, logger);
        }

        public string Name => "NFO DateAdded Creator";

        public string GetSavePath(BaseItem item)
        {
            string xmlpath = PathResolver.GetXmlPathInfoForItem(item);
            if (string.IsNullOrEmpty(xmlpath))
            {
                throw new ArgumentException("Could not get path for item");
            }

            return xmlpath;
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType) => item is Movie || item is Series || item is Season || item is MusicAlbum || item is MusicArtist;

        private static void CreateXmlFile(string filePath, string rootElementName, string dateAdded)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Create a new XML file
            XmlElement rootElement = xmlDoc.CreateElement(rootElementName);
            xmlDoc.AppendChild(rootElement);

            XmlElement dateAddedElement = xmlDoc.CreateElement("dateadded");
            dateAddedElement.InnerText = dateAdded;
            rootElement.AppendChild(dateAddedElement);

            // Save the changes to the XML file
            xmlDoc.Save(filePath);
        }

        public Task SaveAsync(BaseItem item, CancellationToken cancellationToken)
        {
            string xmlPath = PathResolver.GetXmlPathInfoForItem(item);

            if (!string.IsNullOrEmpty(xmlPath) && !File.Exists(xmlPath))
            {
                DateTime? d = _dateHelper.ResolveDateCreatedFromFile(item);

                if (d == null)
                {
                    _logger.LogWarning("Could not get acquire date from file for item: {Item}", item.Name);
                    return Task.CompletedTask;
                }

                string? rootname = PathResolver.GetXmlRootNodeName(item);

                if (!string.IsNullOrEmpty(rootname))
                {
                    CreateXmlFile(xmlPath, rootname, d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                else
                {
                    _logger.LogWarning("Could not get XML root node name item: {Item}", item.Name);
                }
            }
            else
            {
                _logger.LogWarning("Could not resolve XML nfo path for item: {Item}", item.Name);
            }

            return Task.CompletedTask;
        }
    }
}
