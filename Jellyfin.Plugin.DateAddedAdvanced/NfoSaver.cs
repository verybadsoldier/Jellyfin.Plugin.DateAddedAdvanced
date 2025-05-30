using System.Diagnostics;
using System.Globalization;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;


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

        private static bool CheckXmlFile(string filePath, string rootElementName)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();

                // Load the XML file
                xmlDoc.Load(filePath);

                // Get the root node
                XmlNode? rootNode = xmlDoc.DocumentElement;
                if (rootNode == null || rootNode.Name != rootElementName)
                {
                    return false; // Root node is missing or does not match the expected name
                }
            }
            catch (Exception)
            {
                return false; // XML file is not well-formed
            }

            return true; // XML file is well-formed and has the correct root node
        }

        private static void UpdateXmlFile(string filePath, string rootElementName, string dateAdded)
        {
            XmlDocument xmlDoc = new XmlDocument();

            // Load the XML file
            xmlDoc.Load(filePath);

            // Get the root node
            XmlNode? rootNode = xmlDoc.DocumentElement;

            Debug.Assert(rootNode != null, "Root node should not be null at this point.");
            Debug.Assert(rootNode.Name == rootElementName, $"Root node name should be '{rootElementName}', but was '{rootNode.Name}'.");

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
                _logger.LogWarning("Could not resolve XML nfo path for item at path: {Item}. Type: {Type}", item.Name, item.ToString());
                return Task.CompletedTask;
            }

            bool fileExists = File.Exists(xmlPath);

            string? rootname = PathResolver.GetXmlRootNodeName(item);

            if (string.IsNullOrEmpty(rootname))
            {
                _logger.LogWarning("Could not get XML root node name item: {Item}", item.Name);
                return Task.CompletedTask;
            }

            if (fileExists)
            {
                _logger.LogDebug("Checking if file is misformed XML: {Xml}", xmlPath);
                if (!CheckXmlFile(xmlPath, rootname))
                {
                    if (!Plugin.Instance.Configuration.RenameExistingMisformedNfos)
                    {
                        _logger.LogWarning("File is misformed XML {Xml}. Due to config setting RenameExitingMisformedNfos, we will cancel", xmlPath);
                        return Task.CompletedTask;
                    }

                    // rename file to bak file
                    string bakPath = xmlPath + ".bak";
                    _logger.LogInformation("NfoSaver: Renaming misformed NFO file to backup file: {BakPath}", bakPath);
                    File.Move(xmlPath, bakPath);

                    fileExists = false;
                }
            }

            if (fileExists && !Plugin.Instance.Configuration.UpdateExistingNfos)
            {
                _logger.LogInformation("Not updating existing NFO due to config option: {Xml}", xmlPath);
                return Task.CompletedTask;
            }

            var newValue = item.DateCreated.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!fileExists)
            {
                _logger.LogInformation("Creating NFO file: {Path}", xmlPath);
                CreateXmlFile(xmlPath, rootname, newValue);
            }
            else
            {
                // we already checked if the file is misformed XML, so we can update it
                _logger.LogInformation("Updating NFO file: {Path} with date: {Date}", xmlPath, newValue);
                UpdateXmlFile(xmlPath, rootname, newValue);
            }

            return Task.CompletedTask;
        }
    }
}
