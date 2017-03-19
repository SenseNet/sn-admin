using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Ionic.Zip;
using System.Configuration;
using System.Xml;

namespace SenseNet.Tools.SnAdmin
{
    internal class SnAdmin
    {
        #region Constants
        private const string RUNTIMEEXENAME = "SnAdminRuntime.exe";
        private const string SANDBOXDIRECTORYNAME = "run";
        private static string ToolTitle = "Sense/Net Admin ";
        private static string ToolName = "SnAdmin";
        internal static readonly string ParameterRegex = @"^([\w_]+):";

        private static readonly string PackagePreconditionExceptionTypeName = "PackagePreconditionException";
        private static readonly string InvalidPackageExceptionTypeName = "InvalidPackageException";

        private static string CR = Environment.NewLine;
        private static string UsageScreen = String.Concat(
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

        private static int Main(string[] args)
        {
            ToolTitle += Assembly.GetExecutingAssembly().GetName().Version;
            if (args.FirstOrDefault(a => a.ToUpper() == "-WAIT") != null)
            {
                Console.WriteLine("Running in wait mode - now you can attach to the process with a debugger.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }

            string packagePath;
            string sandboxDirectory = null;
            string targetDirectory;
            string logFilePath;
            LogLevel logLevel;
            bool help;
            bool schema;
            bool wait;
            string[] parameters;

            if (!ParseParameters(args, out packagePath, out targetDirectory/*, out phase*/, out parameters, out logFilePath, out logLevel, out help, out schema, out wait))
                return -1;

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

            if (!CheckPackage(ref packagePath, ref sandboxDirectory))
                return -1;

            Logger.PackageName = Path.GetFileName(packagePath);

            Logger.Create(logLevel, logFilePath);
            Debug.WriteLine("##> " + Logger.Level);

            return ExecuteGlobal(packagePath, sandboxDirectory, targetDirectory, parameters, help, schema, wait);
        }

        private static bool ParseParameters(string[] args, out string packagePath, out string targetDirectory, out string[] parameters, out string logFilePath, out LogLevel logLevel, out bool help, out bool schema, out bool wait)
        {
            packagePath = null;
            targetDirectory = null;
            logFilePath = null;
            wait = false;
            help = false;
            schema = false;
            logLevel = LogLevel.Default;
            var prms = new List<string>();
            var argIndex = -1;

            foreach (var arg in args)
            {
                argIndex++;

                if (arg.StartsWith("-"))
                {
                    var verb = arg.Substring(1).ToUpper();
                    switch (verb)
                    {
                        case "?": help = true; break;
                        case "HELP": help = true; break;
                        case "SCHEMA": schema = true; break;
                        case "WAIT": wait = true; break;
                    }
                }
                else if (arg.StartsWith("LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    logFilePath = arg.Substring(4);
                }
                else if (arg.StartsWith("LOGLEVEL:", StringComparison.OrdinalIgnoreCase))
                {
                    logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), arg.Substring(9));
                }
                else if (arg.StartsWith("TARGETDIRECTORY:", StringComparison.OrdinalIgnoreCase))
                {
                    targetDirectory = arg.Substring(16).Trim('"');
                }
                else if (IsValidParameter(arg) && argIndex > 0)
                {
                    // Recognise this as a 'parameter' only if it is not the first one
                    // (which must be the package path without a param name prefix).
                    prms.Add(QuoteParameter(arg));
                }
                else if (packagePath == null)
                {
                    packagePath = arg;
                }
            }

            if (targetDirectory == null)
                targetDirectory = SearchTargetDirectory();

            parameters = prms.ToArray();

            return true;
        }
        private static bool IsValidParameter(string parameter)
        {
            return System.Text.RegularExpressions.Regex.Match(parameter, ParameterRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success;
        }
        private static bool CheckTargetDirectory(string targetDirectory)
        {
            if (Directory.Exists(targetDirectory))
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
                packagePath = Path.Combine(DefaultPackageDirectory(), packagePath);

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
                if (!File.Exists(packagePath))
                    return false;
            }
            else
            {
                if (!Directory.Exists(packagePath))
                {
                    var packageZipPath = packagePath + ".zip";
                    if (!File.Exists(packageZipPath))
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
            Console.WriteLine(ToolTitle);
            Console.WriteLine(message);
            Console.WriteLine(UsageScreen);
            Console.WriteLine("Aborted.");
        }

        private static int ExecuteGlobal(string packagePath, string sandboxDirectory, string targetDirectory, string[] parameters, bool help, bool schema, bool wait)
        {
            Console.WriteLine();

            Logger.LogTitle(ToolTitle);
            Logger.LogWriteLine("Start at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            Logger.LogWriteLine("Target:  " + targetDirectory);
            Logger.LogWriteLine("Package: " + packagePath);

            packagePath = Unpack(packagePath);

            var result = 0;
            var phase = 0;
            var errors = 0;
            var workerExe = string.Empty;

            while (true)
            {
                try
                {
                    workerExe = CreateSandbox(targetDirectory, sandboxDirectory);

                    var appBasePath = Path.GetDirectoryName(workerExe);
                    var workerDomain = AppDomain.CreateDomain(ToolName + "WorkerDomain" + phase, null, appBasePath, null, false);
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
                if (help)
                    prms.Add("-HELP");
                if (wait)
                    prms.Add("-WAIT");
                if (schema)
                    prms.Add("-SCHEMA");

                var processArgs = string.Join(" ", prms);
                var startInfo = new ProcessStartInfo(workerExe, processArgs)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(workerExe),
                    CreateNoWindow = false,
                };

                Process process;
                try
                {
                    process = Process.Start(startInfo);
                    process.WaitForExit();
                    result = process.ExitCode;
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

            Console.WriteLine("See log file: {0}", Logger.GetLogFileName());
            if (Debugger.IsAttached)
            {
                Console.Write("[press any key] ");
                Console.ReadKey();
                Console.WriteLine();
            }
            return result;
        }
        private static string Quote(string prm)
        {
            return "\"" + prm + "\"";
        }
        private static string QuoteParameter(string prm)
        {
            if (prm == null)
                return null;

            // Insert quotes around the parameter value (if there is none) so that this
            // parameter can be passed as a command line argument to the runtime tool.
            // 'Param1:x y' --> 'Param1:"x y"'

            var valueIndex = prm.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);

            // check if there is already a quote there
            if (prm.Substring(valueIndex + 1).StartsWith("\""))
                return prm;

            return prm.Insert(valueIndex + 1, "\"") + "\"";
        }

        private static string GetPackagePreconditionExceptionMessage(Exception e)
        {
            if (e != null && e.GetType().Name == PackagePreconditionExceptionTypeName)
                return e.Message;
            e = e.InnerException;
            if (e != null && e.GetType().Name == PackagePreconditionExceptionTypeName)
                return e.Message;
            return null;
        }
        private static string GetInvalidPackageExceptionMessage(Exception e)
        {
            if (e != null && e.GetType().Name == InvalidPackageExceptionTypeName)
                return e.Message;
            e = e.InnerException;
            if (e != null && e.GetType().Name == InvalidPackageExceptionTypeName)
                return e.Message;
            return null;
        }

        private enum MessageLevel { Success, Warning, Error }

        private static void WriteMessage(string packagePath, MessageLevel level)
        {
            var files = Directory.GetFiles(packagePath);
            if (files.Length != 1)
                return;

            var manifestXml = new XmlDocument();
            manifestXml.Load(files[0]);

            var elementName = string.Empty;
            switch (level)
            {
                case MessageLevel.Success: elementName = "SuccessMessage"; break;
                case MessageLevel.Warning: elementName = "WarningMessage"; break;
                case MessageLevel.Error: elementName = "ErrorMessage"; break;
                default: throw new NotSupportedException("Unknown level: " + level);
            }
            var msgElement = (XmlElement)manifestXml.DocumentElement.SelectSingleNode(elementName);
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

        private static string SearchTargetDirectory()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDirectory"];
            if (!string.IsNullOrEmpty(targetDir))
                return targetDir;

            // default location: ..\webfolder\Admin\bin
            var workerExe = Assembly.GetExecutingAssembly().Location;
            var path = workerExe;

            // go up on the parent chain
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);

            // get the name of the container directory (should be 'Admin')
            var adminDirName = Path.GetFileName(path);
            path = Path.GetDirectoryName(path);

            if (string.Compare(adminDirName, "Admin", StringComparison.OrdinalIgnoreCase) == 0)
            {
                // look for the web.config
                if (System.IO.File.Exists(Path.Combine(path, "web.config")))
                    return path;
            }
            throw new ApplicationException("Configure the TargetDirectory. This path does not exist or it is not a valid target: " + path);
        }
        private static string DefaultPackageDirectory()
        {
            var pkgDir = ConfigurationManager.AppSettings["PackageDirectory"];
            if (!string.IsNullOrEmpty(pkgDir))
                return pkgDir;
            var workerExe = Assembly.GetExecutingAssembly().Location;
            pkgDir = Path.GetDirectoryName(Path.GetDirectoryName(workerExe));
            return pkgDir;
        }

        private static string CreateSandbox(string targetDirectory, string sandboxDirectory)
        {
            var sandboxPath = EnsureEmptySandbox(sandboxDirectory);
            var webBinPath = Path.Combine(targetDirectory, "bin");

            // #1 copy assemblies from webBin to sandbox
            var paths = GetRelevantFiles(webBinPath);
            foreach (var filePath in paths)
                File.Copy(filePath, Path.Combine(sandboxPath, Path.GetFileName(filePath)));

            // #2 copy missing files from Tools directory
            var toolsDir = Path.Combine(targetDirectory, "Tools");
            var toolPaths = GetRelevantFiles(toolsDir);
            var missingNames = toolPaths.Select(p => Path.GetFileName(p))
                .Except(paths.Select(q => Path.GetFileName(q))).OrderBy(r => r)
                .Where(r => !r.ToLower().Contains(".vshost.exe"))
                .ToArray();
            foreach (var fileName in missingNames)
                File.Copy(Path.Combine(toolsDir, fileName), Path.Combine(sandboxPath, fileName));

            // #3 return with path of the worker exe
            return Path.Combine(sandboxPath, RUNTIMEEXENAME);
        }
        private static string[] _relevantExtensions = ".dll;.exe;.pdb;.config".Split(';');
        private static string[] GetRelevantFiles(string dir)
        {
            //return Directory.EnumerateFiles(dir, "*.*").Where(p => _relevantExtensions.Contains(Path.GetExtension(p).ToLower())).ToArray();
            return Directory.GetFiles(dir);
        }
        private static string EnsureEmptySandbox(string packagesDirectory)
        {
            var sandboxFolder = Path.Combine(packagesDirectory, SANDBOXDIRECTORYNAME);
            if (!Directory.Exists(sandboxFolder))
                Directory.CreateDirectory(sandboxFolder);
            else
                DeleteAllFrom(sandboxFolder);
            return sandboxFolder;
        }
        private static void DeleteAllFrom(string sandboxFolder)
        {
            var sandboxInfo = new DirectoryInfo(sandboxFolder);
            foreach (FileInfo file in sandboxInfo.GetFiles())
            {
                if (file.IsReadOnly)
                    file.IsReadOnly = false;
                file.Delete();
            }
            foreach (DirectoryInfo dir in sandboxInfo.GetDirectories())
                dir.Delete(true);
        }

        private static string Unpack(string package)
        {
            if (Directory.Exists(package))
                return package;

            var pkgFolder = Path.GetDirectoryName(package);
            var zipTarget = Path.Combine(pkgFolder, Path.GetFileNameWithoutExtension(package));

            Logger.LogWriteLine("Package directory: " + zipTarget);

            if (Directory.Exists(zipTarget))
            {
                DeleteAllFrom(zipTarget);
                Logger.LogWriteLine("Old files and directories are deleted.");
            }
            else
            {
                Directory.CreateDirectory(zipTarget);
                Logger.LogWriteLine("Package directory created.");
            }

            Logger.LogWriteLine("Extracting ...");
            using (ZipFile zip = ZipFile.Read(package))
            {
                foreach (var e in zip.Entries)
                    e.Extract(zipTarget);
            }
            Logger.LogWriteLine("Ok.");

            return zipTarget;
        }

        private static readonly string[] DisabledPackageNames = {"App_Data", "bin", "log", "run", "tools"};
        private static void ListPackages()
        {
            Console.WriteLine(ToolTitle);

            Console.WriteLine("Upgrade and package executor tool for Sense/Net ECM.");

            Console.WriteLine(UsageScreen);
            Console.WriteLine("Available packages");
            Console.WriteLine("==================");
            Console.WriteLine();

            var packageDirectory = DefaultPackageDirectory();
            PrintPackages(packageDirectory);

            var toolsDirectory = Path.Combine(packageDirectory, "tools");
            if (!Directory.Exists(toolsDirectory))
                return;

            Console.WriteLine();
            Console.WriteLine("Available tools");
            Console.WriteLine("---------------");
            Console.WriteLine();

            PrintPackages(toolsDirectory);

            Console.WriteLine();
        }
        private static void PrintPackages(string directory)
        {
            var unpackedPackages = Directory.GetDirectories(directory)
                .Select(s => new PackageHelpInfo(s, false))
                .Where(p => !DisabledPackageNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            var unpackedPackageNames = unpackedPackages
                .Select(p => p.Name)
                .ToArray();
            var packages = Directory.GetFiles(directory, "*.zip")
                .Select(s => new PackageHelpInfo(s, true))
                .Where(p => !unpackedPackageNames.Contains(p.Name))
                .Concat(unpackedPackages)
                .OrderBy(p => p.Name)
                .ToArray();

            if (packages.Length == 0)
            {
                Console.WriteLine("  There are no packages.");
            }
            else
            {
                var longestLength = packages.Max(s => s.Name.Length);
                var format = "  {0, -" + longestLength + "}  {1}";

                foreach (var package in packages)
                    Console.WriteLine(format, package.Name, package.GetDescription());
            }
        }

        private static void PackageHelp(string path)
        {
            var packagePath = GetPackagePath(path);
            if (packagePath == null)
                Console.WriteLine("Package does not exist: " + Path.GetFileName(path));
            else if (packagePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                Console.WriteLine("Package is compressed: " + Path.GetFileName(path));
            else
                PackageDirectoryHelp(packagePath);
        }
        private static string GetPackagePath(string packagePath)
        {
            if (packagePath == null)
                return null;

            var path = packagePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(DefaultPackageDirectory(), path);

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

            Console.WriteLine("Package:  {0}", info.Name);
            Console.WriteLine("path:     {0}", info.Path);
            Console.WriteLine(info.GetDescription());

            var parameters = info.GetParameters();
            if (parameters.Length == 0)
            {
                Console.WriteLine("Package has no parameter.");
                return;
            }
            Console.WriteLine("Parameters:");
            Console.WriteLine("-----------");

            var longestLength = Math.Min(parameters.Max(s => s.Name.Length), 40);
            var format = "  {0, -" + longestLength + "}  {1}";
            var maxLength = Math.Max(62 - longestLength, 20);

            foreach (var parameter in parameters)
            {
                Console.WriteLine(format, parameter.Name, parameter.Description);
                var value = parameter.DefaultValue;
                if (!string.IsNullOrEmpty(value))
                    Console.WriteLine(format, " ", "Default: " + (value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...")) ;
            }
        }
    }
}