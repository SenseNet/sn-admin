using System;
using System.Collections.Generic;

namespace SenseNet.Tools.SnAdmin
{
    internal class Arguments
    {
        internal static readonly string ParameterRegex = @"^(([\w_]+[\-]{0,1})+):";

        public string PackagePath { get; private set; }
        public string TargetDirectory { get; private set; }
        public string LogFilePath { get; private set; }
        public LogLevel LogLevel { get; private set; }
        public bool Help { get; private set; }
        public bool Schema { get; private set; }
        public bool Wait { get; private set; }
        public string[] Parameters { get; private set; }

        public bool Parse(string[] args)
        {
            LogLevel = LogLevel.Default;
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
                        case "?": Help = true; break;
                        case "HELP": Help = true; break;
                        case "SCHEMA": Schema = true; break;
                        case "WAIT": Wait = true; break;
                    }
                }
                else if (arg.StartsWith("LOG:", StringComparison.OrdinalIgnoreCase))
                {
                    LogFilePath = arg.Substring(4);
                }
                else if (arg.StartsWith("LOGLEVEL:", StringComparison.OrdinalIgnoreCase))
                {
                    LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), arg.Substring(9), true);
                }
                else if (arg.StartsWith("TARGETDIRECTORY:", StringComparison.OrdinalIgnoreCase))
                {
                    TargetDirectory = arg.Substring(16).Trim('"');
                }
                else if (IsValidParameter(arg) && argIndex > 0)
                {
                    // Recognise this as a 'parameter' only if it is not the first one
                    // (which must be the package path without a param name prefix).
                    prms.Add(QuoteParameter(arg));
                }
                else if (PackagePath == null)
                {
                    PackagePath = arg;
                }
            }

            Parameters = prms.ToArray();

            return true;

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

        private static bool IsValidParameter(string parameter)
        {
            return System.Text.RegularExpressions.Regex.Match(parameter, ParameterRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success;
        }
    }
}
