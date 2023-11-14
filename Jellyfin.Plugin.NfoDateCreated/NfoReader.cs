using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Jellyfin.Plugin.NfoDateCreated
{
    internal class NfoReader
    {
        public string ReadDateAdded(string filepath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            XmlNode node = doc.DocumentElement.SelectSingleNode("/album/dateadded");
            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                throw new Exception($"Could not load XML file: {filepath}");
            }
        }
    }
}
