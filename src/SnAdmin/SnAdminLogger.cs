using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IO = System.IO;

namespace SenseNet.Tools.SnAdmin
{
    internal enum LogLevel { Default, File, Console, Silent }

    internal interface ISnAdminLogger
    {
        LogLevel AcceptedLevel { get; }
        string LogFilePath { get; }
        void Initialize(LogLevel level, string logFilePath);
        void WriteTitle(string title);
        void Write(string message);
        void WriteLine(string message);
    }

    internal static class Logger
    {
        public static LogLevel Level { get; private set; }
        private static ISnAdminLogger[] _loggers;
        public static int Errors { get; set; }
        public static string PackageName { get; set; }

        public static void Create(LogLevel level, string logfilePath = null)
        {
            Level = level;
            _loggers = new[] { new SnAdminLogger(), new SnAdminConsoleLogger() };
            foreach (var logger in _loggers)
                logger.Initialize(level, logfilePath);
        }

        public static string GetLogFileName()
        {
            return _loggers.Where(l => l.LogFilePath != null).Select(l => l.LogFilePath).FirstOrDefault();
        }

        public static void LogTitle(string title)
        {
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteTitle(title);
        }
        public static void LogWrite(string message)
        {
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.Write(message);
        }
        public static void LogWrite(string format, params object[] parameters)
        {
            var msg = String.Format(format, parameters);
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.Write(msg);
        }
        public static void LogWriteLine(string message)
        {
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteLine(message);
        }
        public static void LogWriteLine(string format, params object[] parameters)
        {
            var msg = String.Format(format, parameters);
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteLine(msg);
        }
        public static void LogWarningMessage(string message)
        {
            var msg = String.Concat("WARNING: ", message);
            foreach (var logger in _loggers)
                if (logger.AcceptedLevel >= Level)
                    logger.WriteLine(msg);
        }
        public static void LogException(Exception e)
        {
            LogWriteLine(PrintException(e, null));
        }
        public static void LogException(Exception e, string prefix)
        {
            LogWriteLine(PrintException(e, prefix));
        }
        private static string PrintException(Exception e, string prefix)
        {
            Errors++;

            StringBuilder sb = new StringBuilder();
            if (prefix != null)
                sb.Append(prefix).Append(": ");

            sb.Append(e.GetType().Name);
            sb.Append(": ");
            sb.AppendLine(e.Message);
            PrintTypeLoadError(e as System.Reflection.ReflectionTypeLoadException, sb);
            sb.AppendLine(e.StackTrace);
            while ((e = e.InnerException) != null)
            {
                sb.AppendLine("---- Inner Exception:");
                sb.Append(e.GetType().Name);
                sb.Append(": ");
                sb.AppendLine(e.Message);
                PrintTypeLoadError(e as System.Reflection.ReflectionTypeLoadException, sb);
                sb.AppendLine(e.StackTrace);
            }
            return sb.ToString();
        }
        private static void PrintTypeLoadError(System.Reflection.ReflectionTypeLoadException exc, StringBuilder sb)
        {
            if (exc == null)
                return;
            sb.AppendLine("LoaderExceptions:");
            foreach (var e in exc.LoaderExceptions)
            {
                sb.Append("-- ");
                sb.Append(e.GetType().FullName);
                sb.Append(": ");
                sb.AppendLine(e.Message);

                var fileNotFoundException = e as System.IO.FileNotFoundException;
                if (fileNotFoundException != null)
                {
                    sb.AppendLine("FUSION LOG:");
                    sb.AppendLine(fileNotFoundException.FusionLog);
                }
            }
        }
    }

    //==========================================================================================

    internal class SnAdminLogger : ISnAdminLogger
    {
        private const int LINELENGTH = 80;

        protected Dictionary<char, string> _lines;

        public virtual LogLevel AcceptedLevel { get { return LogLevel.File; } }

        public SnAdminLogger()
        {
            _lines = new Dictionary<char, string>();
            _lines['='] = new StringBuilder().Append('=', LINELENGTH - 1).ToString();
            _lines['-'] = new StringBuilder().Append('-', LINELENGTH - 1).ToString();
        }

        public virtual void Initialize(LogLevel level, string logFilePath)
        {
            if (level <= LogLevel.File)
                CreateLog(logFilePath);
        }

        public void WriteTitle(string title)
        {
            LogWriteLine(_lines['=']);
            LogWriteLine(Center(title));
            LogWriteLine(_lines['=']);
        }
        public void Write(string message)
        {
            LogWrite(message);
        }
        public void WriteLine(string message)
        {
            LogWriteLine(message);
        }

        private string Center(string text)
        {
            if (text.Length >= LINELENGTH - 1)
                return text;
            var sb = new StringBuilder();
            sb.Append(' ', (LINELENGTH - text.Length) / 2).Append(text);
            return sb.ToString();
        }

        //================================================================================================================= Logger

        private static readonly string CR = Environment.NewLine;
        public string LogFilePath { get; private set; }

        private string _logFolder = null;
        public string LogFolder
        {
            get
            {
                if (_logFolder == null)
                {
                    _logFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\log\"));
                    if (!Directory.Exists(_logFolder))
                        Directory.CreateDirectory(_logFolder);
                }
                return _logFolder;
            }
            set
            {
                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _logFolder = value;
            }
        }

        protected bool _lineStart;

        public virtual void LogWrite(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, false);
            }
            _lineStart = false;
        }
        public virtual void LogWriteLine(params object[] values)
        {
            using (StreamWriter writer = OpenLog())
            {
                WriteToLog(writer, values, true);
            }
            _lineStart = true;
        }
        private void CreateLog(string logfilePath)
        {
            _lineStart = true;
            if (logfilePath != null)
                LogFilePath = logfilePath;
            else
                LogFilePath = Path.Combine(LogFolder, Logger.PackageName + DateTime.UtcNow.ToString("_yyyyMMdd-HHmmss") + ".log");

            if (!IO.File.Exists(LogFilePath))
            {
                using (FileStream fs = new FileStream(LogFilePath, FileMode.Create))
                {
                    using (StreamWriter wr = new StreamWriter(fs))
                    {
                        wr.WriteLine("");
                    }
                }
            }
        }
        private StreamWriter OpenLog()
        {
            return new StreamWriter(LogFilePath, true);
        }
        private void WriteToLog(StreamWriter writer, object[] values, bool newLine)
        {
            if (_lineStart)
            {
                //writer.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
                writer.Write(DateTime.UtcNow.ToString("HH:mm:ss.ffff"));
                writer.Write("\t");
            }
            foreach (object value in values)
            {
                writer.Write(value);
            }
            if (newLine)
            {
                writer.WriteLine();
            }
        }
    }
    internal class SnAdminConsoleLogger : SnAdminLogger
    {
        public override LogLevel AcceptedLevel { get { return LogLevel.Console; } }

        public override void Initialize(LogLevel level, string logFilePath) { }

        public override void LogWrite(params object[] values)
        {
            WriteToLog(values, false);
        }
        public override void LogWriteLine(params object[] values)
        {
            WriteToLog(values, true);
        }
        private void WriteToLog(object[] values, bool newLine)
        {
            foreach (object value in values)
                Console.Write(value);
            if (newLine)
                Console.WriteLine();
        }
    }
}
