using System.Linq;
using System.Xml;

namespace SenseNet.Tools.SnAdmin
{
    internal class PackageHelpInfo
    {
        public string Name { get; }
        public string Path { get; }
        public bool Compressed { get; }

        public PackageHelpInfo(string path, bool compressed)
        {
            Path = path;
            Compressed = compressed;
            Name = compressed ? System.IO.Path.GetFileNameWithoutExtension(path) : System.IO.Path.GetFileName(path);
        }

        private XmlDocument _manifest;

        private XmlDocument GetManifest()
        {
            if (_manifest == null)
            {
                var manifestPath = Disk.GetFiles(Path).FirstOrDefault();
                _manifest = Disk.LoadManifest(manifestPath);
            }
            return _manifest;
        }

        public string GetDescription()
        {
            if (Compressed)
                return "(compressed)";
            return GetManifest()?.SelectSingleNode("/Package/Description")?.InnerXml;
        }

        public PackageParameterHelpInfo[] GetParameters()
        {
            var manifest = GetManifest();
            if(manifest == null)
                return new PackageParameterHelpInfo[0];

            return manifest.SelectNodes("/Package/Parameters/Parameter")
                .Cast<XmlElement>()
                .Select(p => new PackageParameterHelpInfo
                {
                    Name = p.Attributes["name"].Value.Trim('@'),
                    DefaultValue = p.InnerXml,
                    Description = p.Attributes["description"]?.Value
                })
                .ToArray();
        }
    }

    internal class PackageParameterHelpInfo
    {
        public string Name;
        public string Description;
        public string DefaultValue;
    }
}