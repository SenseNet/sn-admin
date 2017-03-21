using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Tools.SnAdmin.Tests
{
    [TestClass]
    public class ArgumentTests
    {
        [TestMethod]
        public void CmdArgument_Empty()
        {
            // ARRANGE
            var args = new string[0];
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_Help()
        {
            // ARRANGE
            var args = new[] {"-help"};
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsTrue(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_Schema()
        {
            // ARRANGE
            var args = new[] { "-schema" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsTrue(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_Wait()
        {
            // ARRANGE
            var args = new[] { "-wait" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsTrue(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_PackagePath()
        {
            // ARRANGE
            var args = new[] { "PackageName" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.AreEqual("PackageName", arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_TargetDirectory()
        {
            // ARRANGE
            var args = new[] { "targetdirectory:T:\\TargetDir" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.AreEqual("T:\\TargetDir", arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_Log()
        {
            // ARRANGE
            var args = new[] { "log:\"L:\\LogDir\\SpecialLog.log\"" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.AreEqual("\"L:\\LogDir\\SpecialLog.log\"", arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_LogLevel_Default()
        {
            // ARRANGE
            var args = new[] { "loglevel:default" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_LogLevel_File()
        {
            // ARRANGE
            var args = new[] { "loglevel:file" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.File, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_LogLevel_Console()
        {
            // ARRANGE
            var args = new[] { "loglevel:console" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Console, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_LogLevel_Silent()
        {
            // ARRANGE
            var args = new[] { "loglevel:silent" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.IsNull(arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Silent, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(0, arguments.Parameters.Length);
        }
        [TestMethod]
        public void CmdArgument_PackageParameters_One()
        {
            // ARRANGE
            var args = new[] { "PackageName", "Param1:Value1" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.AreEqual("PackageName", arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(1, arguments.Parameters.Length);
            Assert.AreEqual("Param1:\"Value1\"", arguments.Parameters[0]);
        }
        [TestMethod]
        public void CmdArgument_PackageParameters_Two()
        {
            // ARRANGE
            var args = new[] { "PackageName", "Param1:Value1", "param2:value2" };
            var arguments = new Arguments();

            // ACT
            arguments.Parse(args);

            // ASSERT
            Assert.AreEqual("PackageName", arguments.PackagePath);
            Assert.IsNull(arguments.TargetDirectory);
            Assert.IsNull(arguments.LogFilePath);
            Assert.AreEqual(LogLevel.Default, arguments.LogLevel);
            Assert.IsFalse(arguments.Help);
            Assert.IsFalse(arguments.Schema);
            Assert.IsFalse(arguments.Wait);
            Assert.IsNotNull(arguments.Parameters);
            Assert.AreEqual(2, arguments.Parameters.Length);
            Assert.AreEqual("Param1:\"Value1\"", arguments.Parameters[0]);
            Assert.AreEqual("param2:\"value2\"", arguments.Parameters[1]);
        }
    }
}
