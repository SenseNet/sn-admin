using System;
using System.Xml;

namespace SenseNet.Tools.SnAdmin.Tests
{
    internal class TestUnpacker : IUnpacker
    {
        public void Unpack(string packagePath, string targetDirectory)
        {
            if(!(Disk.Instance is TestDisk disk))
                throw new NotSupportedException("Associated disk is not supported. Only a TestDisk instance is allowed.");
            if(!packagePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                throw new NotSupportedException("Package is not a .zip file.");

            disk.Manifests.TryGetValue(packagePath + "\\manifest.xml", out XmlDocument manifest);
            if (manifest == null)
                return;

            disk.Manifests.Add($@"{targetDirectory}\manifest.xml", manifest);
            disk.Files.Add($@"{targetDirectory}\manifest.xml");
        }
    }
}
