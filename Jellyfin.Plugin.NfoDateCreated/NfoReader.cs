using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.NfoDateCreated
{
    internal class NfoReader
    {
        public static string? ReadDateAdded(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/album/dateadded");
            if (node == null)
            {
                return null;
            }

            return node.InnerText;
        }
    }
}
