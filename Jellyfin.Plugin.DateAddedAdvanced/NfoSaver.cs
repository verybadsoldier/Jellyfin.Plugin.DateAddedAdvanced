using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
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
            string xmlpath = PathResolver.GetXmlPathInfoForItem(item, false);
            if (string.IsNullOrEmpty(xmlpath))
            {
                throw new ArgumentException("Could not get path for item");
            }

            return xmlpath;
        }

        public bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            var conf = Plugin.Instance.Configuration;
            return (item is MusicArtist && conf.WriteArtistNfo) || (item is MusicAlbum) ||
                 (item is Season && conf.WriteSeasonNfo) || (item is Series && conf.WriteTvShowNfo) || (item is Episode && conf.WriteEpisodeNfo) ||
                 (item is Movie && conf.WriteMovieNfo);
        }

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

        private static void UpdateXmlFile(string filePath, string rootElementName, string dateAdded)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Load the XML file
            xmlDoc.Load(filePath);

            // Get the root node
            XmlNode? rootNode = xmlDoc.DocumentElement;

            if (rootNode == null )
            {
                rootNode = xmlDoc.CreateElement(rootElementName);

                xmlDoc.AppendChild(rootNode);
            }

            // Check if the "dateadded" node already exists
            XmlNode? dateAddedNode = rootNode.SelectSingleNode("dateadded");

            if (dateAddedNode == null)
            {
                // If it doesn't exist, create a new "dateadded" node
                dateAddedNode = xmlDoc.CreateElement("dateadded");

                // Add the new node to the root
                rootNode.AppendChild(dateAddedNode);
            }

            // Set the value of the "dateadded" node
            dateAddedNode.InnerText = dateAdded;

            // Save the updated XML file
            xmlDoc.Save(filePath);
        }

        public Task SaveAsync(BaseItem item, CancellationToken cancellationToken)
        {
            string xmlPath = PathResolver.GetXmlPathInfoForItem(item, false);

            if (string.IsNullOrEmpty(xmlPath))
            {
                _logger.LogWarning("Saving NFO: Could not resolve XML nfo path for item at path: {Item}. Type: {Type}", item.Name, item.ToString());
                return Task.CompletedTask;
            }

            bool fileExists = File.Exists(xmlPath);
            if (fileExists && !Plugin.Instance.Configuration.UpdateExistingNfos)
            {
                _logger.LogInformation("Saving NFO: Not updating existing NFO due to config option: {Xml}", xmlPath);
                return Task.CompletedTask;
            }

            DateTime? d = _dateHelper.ResolveDateCreatedFromFile(item);

            if (d == null)
            {
                _logger.LogWarning("Could not get acquire date from file for item: {Item}", item.Name);
                return Task.CompletedTask;
            }

            string? rootname = PathResolver.GetXmlRootNodeName(item);

            if (string.IsNullOrEmpty(rootname))
            {
                _logger.LogWarning("Saving NFO: Could not get XML root node name item: {Item}", item.Name);
                return Task.CompletedTask;
            }

            var newValue = d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!fileExists)
            {
                _logger.LogWarning("Creating NFO file: {Path}", xmlPath);
                CreateXmlFile(xmlPath, rootname, newValue);
            }
            else
            {
                _logger.LogWarning("Updating NFO file: {Path}", xmlPath);
                UpdateXmlFile(xmlPath, rootname, newValue);
            }

            return Task.CompletedTask;
        }
    }
}
