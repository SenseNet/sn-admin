using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Tools.SnAdmin.Tests
{
    [TestClass]
    public class ExecutionTests : TestBase
    {
        // USE "LOGLEVEL:Console" ARGUMENT IN EVERY TEST TO AVOID DISK INTERACTIONS.

        [TestInitialize]
        public void InitializeTest()
        {
            Initialize();
        }

        [TestMethod]
        public void Execution_OnePhase()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var args = new[] { "Pkg1", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            var result = SnAdmin.Main(args);

            // ASSERT
            Assert.AreEqual(0, result);
            Assert.AreEqual(1, activator.ExePaths.Count);
            Assert.AreEqual(1, activator.Args.Count);
            Assert.IsTrue(activator.Args[0].Contains("Pkg1"));
            Assert.IsTrue(activator.Args[0].Contains("PHASE:0"));
        }
        [TestMethod]
        public void Execution_ThreePhases()
        {
            // ARRANGE
            var disk = new TestDisk(DefaultDirs, DefaultFiles, DefaultManifests);
            Disk.Instance = disk;
            var activator = new TestActivator(3);
            ProcessActivator.Instance = activator;
            var args = new[] { "Pkg1", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            var result = SnAdmin.Main(args);

            // ASSERT
            Assert.AreEqual(0, result);
            Assert.AreEqual(3, activator.ExePaths.Count);
            Assert.AreEqual(3, activator.Args.Count);
            Assert.IsTrue(activator.Args[0].Contains("Pkg1"));
            Assert.IsTrue(activator.Args[0].Contains("PHASE:0"));
            Assert.IsTrue(activator.Args[1].Contains("Pkg1"));
            Assert.IsTrue(activator.Args[1].Contains("PHASE:1"));
            Assert.IsTrue(activator.Args[2].Contains("Pkg1"));
            Assert.IsTrue(activator.Args[2].Contains("PHASE:2"));
        }

        [TestMethod]
        public void Execution_Unpack()
        {
            // ARRANGE
            var xml = new XmlDocument();
            //manifests.Add(@"Q:\WebApp1\Admin\Package1\manifest.xml", xml);
            xml.LoadXml(@"<Package type='Product' level='Tool'>
  <Name>Sense/Net ECM</Name>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Description>|package description|</Description>
  <Steps>
    <Trace>Original message</Trace>
  </Steps>
</Package>");

            var files = DefaultFiles.ToList();
            files.Add(@"Q:\WebApp1\Admin\Package1.zip");
            var manifests = DefaultManifests.ToDictionary(x => x.Key, x => x.Value);
            manifests.Add(@"Q:\WebApp1\Admin\Package1.zip\manifest.xml", xml);

            var disk = new TestDisk(DefaultDirs, files, manifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var unpacker = new TestUnpacker();
            Unpacker.Instance = unpacker;
            var args = new[] { "Package1", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            var result = SnAdmin.Main(args);

            // ASSERT
            var consoleText = console.GetStringBuilder().ToString();
            Assert.AreEqual(0, result);
            
            Assert.IsTrue(disk.Manifests.ContainsKey(@"Q:\WebApp1\Admin\Package1\manifest.xml"));
            Assert.IsTrue(consoleText.Contains("Extracting ..."));

            Assert.AreEqual(1, activator.ExePaths.Count);
            Assert.AreEqual(1, activator.Args.Count);
            Assert.IsTrue(activator.Args[0].Contains("Package1"));
            Assert.IsTrue(activator.Args[0].Contains("PHASE:0"));
        }
        [TestMethod]
        public void Execution_ForceUnpack()
        {
            // ARRANGE
            var xml0 = new XmlDocument();
            //manifests.Add(@"Q:\WebApp1\Admin\Package1\manifest.xml", xml);
            xml0.LoadXml(@"<Package type='Product' level='Tool'>
  <Name>Sense/Net ECM</Name>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Description>|package description|</Description>
  <Steps>
    <Trace>Original message</Trace>
  </Steps>
</Package>");
            var xml1 = new XmlDocument();
            //manifests.Add(@"Q:\WebApp1\Admin\Package1\manifest.xml", xml);
            xml1.LoadXml(@"<Package type='Product' level='Tool'>
  <Name>Sense/Net ECM</Name>
  <ReleaseDate>2016-12-21</ReleaseDate>
  <Description>|package description|</Description>
  <Steps>
    <Trace>Overridden message</Trace>
  </Steps>
</Package>");

            var dirs = DefaultDirs.ToList();
            dirs.Add(@"Q:\WebApp1\Admin\Package1");
            var files = DefaultFiles.ToList();
            files.Add(@"Q:\WebApp1\Admin\Package1.zip");
            files.Add(@"Q:\WebApp1\Admin\Package1\manifest.xml");
            var manifests = DefaultManifests.ToDictionary(x => x.Key, x => x.Value);
            manifests.Add(@"Q:\WebApp1\Admin\Package1.zip\manifest.xml", xml0);
            manifests.Add(@"Q:\WebApp1\Admin\Package1\manifest.xml", xml1);

            var disk = new TestDisk(dirs, files, manifests);
            Disk.Instance = disk;
            var activator = new TestActivator(1);
            ProcessActivator.Instance = activator;
            var unpacker = new TestUnpacker();
            Unpacker.Instance = unpacker;
            var args = new[] { "Package1", "LOGLEVEL:Console" };
            var console = new StringWriter();
            SnAdmin.Output = console;

            // ACT
            var result = SnAdmin.Main(args);

            // ASSERT
            var consoleText = console.GetStringBuilder().ToString();
            var traceMessage = disk.Manifests[@"Q:\WebApp1\Admin\Package1\manifest.xml"].SelectSingleNode("//Trace")?.InnerText ?? "[null]";

            Assert.AreEqual(0, result);
            Assert.AreEqual("Original message", traceMessage);

            Assert.IsTrue(consoleText.Contains("Old files and directories are deleted."));
            Assert.IsTrue(consoleText.Contains("Extracting ..."));

            Assert.AreEqual(1, activator.ExePaths.Count);
            Assert.AreEqual(1, activator.Args.Count);
            Assert.IsTrue(activator.Args[0].Contains("Package1"));
            Assert.IsTrue(activator.Args[0].Contains("PHASE:0"));
        }
    }
}
