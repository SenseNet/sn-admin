using System;
using System.Diagnostics;
using System.IO;

namespace SenseNet.Tools.SnAdmin
{
    internal interface IProcessActivator
    {
        int ExecuteProcess(string workerExePath, string processArgs);
    }

    internal abstract class ProcessActivator
    {
        internal static IProcessActivator Instance { get; set; } = new SnAdminRuntimeActivator();

        public static int ExecuteProcess(string workerExePath, string processArgs)
        {
            return Instance.ExecuteProcess(workerExePath, processArgs);
        }
    }

    internal class SnAdminRuntimeActivator : IProcessActivator
    {
        public int ExecuteProcess(string workerExePath, string processArgs)
        {
            if (workerExePath == null)
                throw new ArgumentNullException(nameof(workerExePath));

            var startInfo = new ProcessStartInfo(workerExePath, processArgs)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(workerExePath),
                CreateNoWindow = false
            };

            var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode ?? -1;
        }
    }
}
