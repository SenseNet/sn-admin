using System;
using System.Collections.Generic;

namespace SenseNet.Tools.SnAdmin.Tests
{
    internal class TestActivator : IProcessActivator
    {
        private int _phases;
        public TestActivator(int phases)
        {
            _phases = Math.Max(1, phases);
        }

        public List<string> ExePaths { get; } = new List<string>();
        public List<string> Args { get; } = new List<string>();

        public int ExecuteProcess(string exePath, string args)
        {
            ExePaths.Add(exePath);
            Args.Add(args);
            return --_phases > 0 ? 1 : 0;
        }
    }
}
