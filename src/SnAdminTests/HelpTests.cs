using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Tools.SnAdmin.Tests
{
    [TestClass]
    public class HelpTests : TestBase
    {
        [TestInitialize]
        public void InitializeTest()
        {
            base.Initialize();
        }

        [TestMethod]
        public void Help_PackageList()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var args = new[] { "-help" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            SnAdmin.Main(args);

            // ASSERT
            var lines = new List<string>();
            var consoleText = console.GetStringBuilder().ToString();
            using (var reader = new StringReader(consoleText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            Assert.IsTrue(lines.Any(l => l.StartsWith("Usage:")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Available packages")));
            Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("Pkg1")));
            Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("Pkg2")));
            Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("Pkg3")));
            Assert.AreEqual(0, activator.ExePaths.Count);
        }

        [TestMethod]
        public void Help_PackageDetails()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var args = new[] { "Pkg1", "-help" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            SnAdmin.Main(args);

            // ASSERT
            var lines = new List<string>();
            var consoleText = console.GetStringBuilder().ToString();
            using (var reader = new StringReader(consoleText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            Assert.IsTrue(lines.Any(l => l.StartsWith("|package description|")));
            Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("traceMessage") && l.Contains("|parameter description|")));
            Assert.IsTrue(lines.Any(l => l.TrimStart().StartsWith("Default: |default value|")));
            Assert.AreEqual(0, activator.ExePaths.Count);
        }

        [TestMethod]
        public void Help_MissingPackage()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var args = new[] { "MissingPackage", "-help" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            SnAdmin.Main(args);

            // ASSERT
            var lines = new List<string>();
            var consoleText = console.GetStringBuilder().ToString();
            using (var reader = new StringReader(consoleText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    lines.Add(line);
            }
            Assert.IsTrue(lines.Any(l => l.StartsWith("Package does not exist: MissingPackage")));
            Assert.AreEqual(0, activator.ExePaths.Count);
        }
    }
}
