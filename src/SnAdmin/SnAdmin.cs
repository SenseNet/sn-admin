using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;

namespace SenseNet.Tools.SnAdmin
{
    internal class SnAdmin
    {
        #region Constants
        private const string RUNTIMEEXENAME = "SnAdminRuntime.exe";
        private const string SANDBOXDIRECTORYNAME = "run";
        private static string ToolTitle = "Sense/Net Admin ";
        private const string ToolName = "SnAdmin";

        private const string PackagePreconditionExceptionTypeName = "PackagePreconditionException";
        private const string InvalidPackageExceptionTypeName = "InvalidPackageException";

        private static readonly string CR = Environment.NewLine;
        private static readonly string UsageScreen = string.Concat(
            //         1         2         3         4         5         6         7         8
            //12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
            CR,
            "Usage:", CR,
            ToolName, " <package> [<target>]", CR,
            CR,
            "Parameters:", CR,
            "  <package>: File contains a package (*.zip or directory).", CR,
            "  <target>: Directory contains web folder of a stopped SenseNet instance.", CR,
            CR,
            "Help about an existing package:", CR,
            ToolName, " <package> -help", CR
        );
        #endregion

        internal static TextWriter Output { get; set; } = Console.Out;

        internal static int Main(string[] args)
        {
            ToolTitle += Assembly.GetExecutingAssembly().GetName().Version;
            if (args.FirstOrDefault(a => a.ToUpper() == "-WAIT") != null)
            {
                Output.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                Output.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }

            var arguments = new Arguments();
            if (!arguments.Parse(args))
                return -1;

            var packagePath = arguments.PackagePath;
            var help = arguments.Help;
            var parameters = arguments.Parameters;

            var targetDirectory = arguments.TargetDirectory ?? Disk.SearchTargetDirectory();
            if (!CheckTargetDirectory(targetDirectory))
                return -1;

            if (help)
            {
                if (packagePath == null)
                    ListPackages();
                else
                    PackageHelp(packagePath);
                return 0;
            }

            string sandboxDirectory = null;
            if (!CheckPackage(ref packagePath, ref sandboxDirectory))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);

            Logger.Create(arguments.LogLevel, arguments.LogFilePath);
            Debug.WriteLine("##> " + Logger.Level);

            return ExecuteGlobal(packagePath, sandboxDirectory, targetDirectory, parameters, arguments.Schema, arguments.Wait);
        }

        private static bool CheckTargetDirectory(string targetDirectory)
        {
            if (Disk.DirectoryExists(targetDirectory))
                return true;

            PrintParameterError("Given target directory does not exist: " + targetDirectory);
            return false;
        }

        private static bool CheckPackage(ref string packagePath, ref string sandboxDirectory)
        {
            if (packagePath == null)
            {
                PrintParameterError("Missing package");
                return false;
            }

            if (!Path.IsPathRooted(packagePath))
                packagePath = Path.Combine(Disk.DefaultPackageDirectory(), packagePath);

            // Sandbox directory should always be the parent of the provided package, 
            // even if we find the package in the tools subfolder.
            sandboxDirectory = Path.GetDirectoryName(packagePath);

            // save original path for logging
            var originalPackagePath = packagePath;

            if (CheckPackageFileOrFolder(ref packagePath))
                return true;

            // If there is no such package in the provided package folder, we will look for
            // it inside the 'tools' subfolder where we store built-in tool packages like
            // import or index. In this case the sandbox (where the runtime is executed) still
            // has to be in the main package folder instead of the tools subfolder - this is why 
            // we set a different variable for the sandbox path.
            packagePath = InsertToolsFolderName(packagePath);

            if (CheckPackageFileOrFolder(ref packagePath))
                return true;

            PrintParameterError("Given package does not exist: " + originalPackagePath);
            return false;
        }
        private static bool CheckPackageFileOrFolder(ref string packagePath)
        {
            // Find the package provided by the caller. Package path can be absolute or relative,
            // or a simple name. It can end with a folder name or a zip extension.

            if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (!Disk.FileExists(packagePath))
                    return false;
            }
            else
            {
                if (!Disk.DirectoryExists(packagePath))
                {
                    var packageZipPath = packagePath + ".zip";
                    if (!Disk.FileExists(packageZipPath))
                        return false;

                    packagePath = packageZipPath;
                }
            }

            return true;
        }
        private static string InsertToolsFolderName(string packagePath)
        {
            // look for the package in the tools subfolder
            return packagePath.Insert(packagePath.LastIndexOf("\\", StringComparison.InvariantCultureIgnoreCase), "\\tools");
        }

        private static void PrintParameterError(string message)
        {
            Output.WriteLine(ToolTitle);
            Output.WriteLine(message);
            Output.WriteLine(UsageScreen);
            Output.WriteLine("Aborted.");
        }

        private static int ExecuteGlobal(string packagePath, string sandboxDirectory, string targetDirectory, string[] parameters, bool schema, bool wait)
        {
            Output.WriteLine();

            Logger.LogTitle(ToolTitle);
            Logger.LogWriteLine("Start at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            Logger.LogWriteLine("Target:  " + targetDirectory);
            Logger.LogWriteLine("Package: " + packagePath);

            packagePath = Unpack(packagePath);

            int result;
            var phase = 0;
            var errors = 0;
            string workerExe;

            while (true)
            {
                try
                {
                    workerExe = CreateSandbox(targetDirectory, sandboxDirectory);

                    var appBasePath = Path.GetDirectoryName(workerExe);
                    AppDomain.CreateDomain(ToolName + "WorkerDomain" + phase, null, appBasePath, null, false);
                }
                catch (Exception ex)
                {
                    Logger.LogWriteLine("ERROR during environment initialization:");
                    Logger.LogException(ex);

                    // end the operation here as we could not even build the environment correctly
                    result = -2;
                    break;
                }

                var phaseParameter = "PHASE:" + phase;
                var logParameter = "LOG:" + Logger.GetLogFileName();
                var logLevelParameter = "LOGLEVEL:" + Logger.Level;
                var targetDirParameter = "TargetDirectory:" + Quote(targetDirectory);

                var prms = new List<string> { Quote(packagePath), targetDirParameter, phaseParameter, Quote(logParameter), logLevelParameter };
                prms.AddRange(parameters);
                if (wait)
                    prms.Add("-WAIT");
                if (schema)
                    prms.Add("-SCHEMA");

                var processArgs = string.Join(" ", prms);

                try
                {
                    result = ProcessActivator.ExecuteProcess(workerExe, processArgs);
                }
                catch (Exception e)
                {
                    var preExMessage = GetPackagePreconditionExceptionMessage(e);
                    if (preExMessage != null)
                    {
                        Logger.LogWriteLine("PRECONDITION FAILED:");
                        Logger.LogWriteLine(preExMessage);
                    }
                    else
                    {
                        var pkgExMessage = GetInvalidPackageExceptionMessage(e);
                        if (pkgExMessage != null)
                        {
                            Logger.LogWriteLine("INVALID PACKAGE:");
                            Logger.LogWriteLine(pkgExMessage);
                        }
                        else
                        {
                            Logger.LogWriteLine("#### UNHANDLED EXCEPTION:");
                            Logger.LogException(e);
                        }
                    }
                    result = -1;
                }
                if (result > 0)
                {
                    errors += (result & -2) / 2;
                    result = result & 1;
                }

                if (result < 1)
                    break;

                phase++;

                // wait for the file system to release everything
                Thread.Sleep(2000);
            }

            Logger.LogWriteLine("===============================================================================");
            if (result == -1)
                Logger.LogWriteLine(ToolName + " terminated with warning.");
            else if (result < -1)
                Logger.LogWriteLine(ToolName + " stopped with error.");
            else if (errors == 0)
                Logger.LogWriteLine(ToolName + " has been successfully finished.");
            else
                Logger.LogWriteLine(ToolName + " has been finished with {0} errors.", errors);

            var msgLevel = MessageLevel.Success;
            if (result == -1)
                msgLevel = MessageLevel.Warning;
            else if (result < -1 || errors != 0)
                msgLevel = MessageLevel.Error;
            WriteMessage(packagePath, msgLevel);

            Output.WriteLine("See log file: {0}", Logger.GetLogFileName());
            if (Debugger.IsAttached && ProcessActivator.Instance is SnAdminRuntimeActivator)
            {
                Output.Write("[press any key] ");
                Console.ReadKey();
                Output.WriteLine();
            }
            return result;
        }
        private static string Quote(string prm)
        {
            return "\"" + prm + "\"";
        }

        private static string GetPackagePreconditionExceptionMessage(Exception e)
        {
            if (e?.GetType().Name == PackagePreconditionExceptionTypeName)
                return e.Message;

            e = e?.InnerException;

            return e?.GetType().Name == PackagePreconditionExceptionTypeName ? e.Message : null;
        }
        private static string GetInvalidPackageExceptionMessage(Exception e)
        {
            if (e?.GetType().Name == InvalidPackageExceptionTypeName)
                return e.Message;

            e = e?.InnerException;

            return e?.GetType().Name == InvalidPackageExceptionTypeName ? e.Message : null;
        }

        private enum MessageLevel { Success, Warning, Error }

        private static void WriteMessage(string packagePath, MessageLevel level)
        {
            var files = Disk.GetFiles(packagePath);
            if (files.Length != 1)
                return;

            var manifestXml = Disk.LoadManifest(files[0]);
            string elementName;

            switch (level)
            {
                case MessageLevel.Success: elementName = "SuccessMessage"; break;
                case MessageLevel.Warning: elementName = "WarningMessage"; break;
                case MessageLevel.Error: elementName = "ErrorMessage"; break;
                default: throw new NotSupportedException("Unknown level: " + level);
            }
            var msgElement = (XmlElement)manifestXml.DocumentElement?.SelectSingleNode(elementName);
            if (msgElement == null)
                return;

            var msg = msgElement.InnerText;

            var backgroundColorBackup = Console.BackgroundColor;
            var foregroundColorBackup = Console.ForegroundColor;
            switch (level)
            {
                case MessageLevel.Success:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case MessageLevel.Warning:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageLevel.Error:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    throw new NotSupportedException("Unknown level: " + level);
            }

            Logger.LogWrite(msg);
            Console.BackgroundColor = backgroundColorBackup;
            Console.ForegroundColor = foregroundColorBackup;
            Logger.LogWriteLine(string.Empty);
        }

        private static string CreateSandbox(string targetDirectory, string sandboxDirectory)
        {
            var sandboxPath = EnsureEmptySandbox(sandboxDirectory);
            var webBinPath = Path.Combine(targetDirectory, "bin");

            // #1 copy assemblies from webBin to sandbox
            var paths = GetRelevantFiles(webBinPath);
            foreach (var filePath in paths)
                Disk.FileCopy(filePath, Path.Combine(sandboxPath, Path.GetFileName(filePath)));

            // #2 copy missing files from Tools directory
            var toolsDir = Path.Combine(targetDirectory, "Tools");
            var toolPaths = GetRelevantFiles(toolsDir);
            var missingNames = toolPaths.Select(Path.GetFileName)
                .Except(paths.Select(Path.GetFileName)).OrderBy(r => r)
                .Where(r => !r.ToLower().Contains(".vshost.exe"))
                .ToArray();
            foreach (var fileName in missingNames)
                Disk.FileCopy(Path.Combine(toolsDir, fileName), Path.Combine(sandboxPath, fileName));

            // #3 return with path of the worker exe
            return Path.Combine(sandboxPath, RUNTIMEEXENAME);
        }

        private static string[] GetRelevantFiles(string dir)
        {
            return Disk.GetFiles(dir);
        }
        private static string EnsureEmptySandbox(string packagesDirectory)
        {
            var sandboxFolder = Path.Combine(packagesDirectory, SANDBOXDIRECTORYNAME);
            if (!Disk.DirectoryExists(sandboxFolder))
                Disk.CreateDirectory(sandboxFolder);
            else
                Disk.DeleteAllFrom(sandboxFolder);
            return sandboxFolder;
        }

        private static string Unpack(string package)
        {
            if (Disk.DirectoryExists(package))
                return package;

            var pkgFolder = Path.GetDirectoryName(package);
            var zipTarget = Path.Combine(pkgFolder, Path.GetFileNameWithoutExtension(package));

            Logger.LogWriteLine("Package directory: " + zipTarget);

            if (Disk.DirectoryExists(zipTarget))
            {
                Disk.DeleteAllFrom(zipTarget);
                Logger.LogWriteLine("Old files and directories are deleted.");
            }
            else
            {
                Disk.CreateDirectory(zipTarget);
                Logger.LogWriteLine("Package directory created.");
            }

            Logger.LogWriteLine("Extracting ...");

            ZipFile.ExtractToDirectory(package, zipTarget);
            
            Logger.LogWriteLine("Ok.");

            return zipTarget;
        }

        private static readonly string[] DisabledPackageNames = { "App_Data", "bin", "log", "run", "tools" };
        private static void ListPackages()
        {
            Output.WriteLine(ToolTitle);

            Output.WriteLine("Upgrade and package executor tool for Sense/Net ECM.");

            Output.WriteLine(UsageScreen);
            Output.WriteLine("Available packages");
            Output.WriteLine("==================");
            Output.WriteLine();

            var packageDirectory = Disk.DefaultPackageDirectory();
            PrintPackages(packageDirectory);

            var toolsDirectory = Path.Combine(packageDirectory, "tools");
            if (!Disk.DirectoryExists(toolsDirectory))
                return;

            Output.WriteLine();
            Output.WriteLine("Available tools");
            Output.WriteLine("---------------");
            Output.WriteLine();

            PrintPackages(toolsDirectory);

            Output.WriteLine();
        }
        private static void PrintPackages(string directory)
        {
            var unpackedPackages = Disk.GetDirectories(directory)
                .Select(s => new PackageHelpInfo(s, false))
                .Where(p => !DisabledPackageNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            var unpackedPackageNames = unpackedPackages
                .Select(p => p.Name)
                .ToArray();
            var packages = Disk.GetFiles(directory, "*.zip")
                .Select(s => new PackageHelpInfo(s, true))
                .Where(p => !unpackedPackageNames.Contains(p.Name))
                .Concat(unpackedPackages)
                .OrderBy(p => p.Name)
                .ToArray();

            if (packages.Length == 0)
            {
                Output.WriteLine("  There are no packages.");
            }
            else
            {
                var longestLength = packages.Max(s => s.Name.Length);
                var format = "  {0, -" + longestLength + "}  {1}";

                foreach (var package in packages)
                    Output.WriteLine(format, package.Name, package.GetDescription());
            }
        }

        private static void PackageHelp(string path)
        {
            var packagePath = GetPackagePath(path);
            if (packagePath == null)
                Output.WriteLine("Package does not exist: " + Path.GetFileName(path));
            else if (packagePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                Output.WriteLine("Package is compressed: " + Path.GetFileName(path));
            else
                PackageDirectoryHelp(packagePath);
        }
        private static string GetPackagePath(string packagePath)
        {
            if (packagePath == null)
                return null;

            var path = packagePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(Disk.DefaultPackageDirectory(), path);

            if (CheckPackageFileOrFolder(ref path))
                return path;
            path = InsertToolsFolderName(path);
            if (CheckPackageFileOrFolder(ref path))
                return path;

            return null;
        }
        private static void PackageDirectoryHelp(string packagePath)
        {
            var info = new PackageHelpInfo(packagePath, false);

            Output.WriteLine("Package:  {0}", info.Name);
            Output.WriteLine("path:     {0}", info.Path);
            Output.WriteLine(info.GetDescription());

            var parameters = info.GetParameters();
            if (parameters.Length == 0)
            {
                Output.WriteLine("Package has no parameter.");
                return;
            }
            Output.WriteLine("Parameters:");
            Output.WriteLine("-----------");

            var longestLength = Math.Min(parameters.Max(s => s.Name.Length), 40);
            var format = "  {0, -" + longestLength + "}  {1}";
            var maxLength = Math.Max(62 - longestLength, 20);

            foreach (var parameter in parameters)
            {
                Output.WriteLine(format, parameter.Name, parameter.Description);
                var value = parameter.DefaultValue;
                if (!string.IsNullOrEmpty(value))
                    Output.WriteLine(format, " ", "Default: " + (value.Length <= maxLength ? value : value.Substring(0, maxLength) + "..."));
            }
        }
    }
}