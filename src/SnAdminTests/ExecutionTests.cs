using System.IO;
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
            var consoleText = console.GetStringBuilder().ToString();
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
            var consoleText = console.GetStringBuilder().ToString();
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
    }
}
