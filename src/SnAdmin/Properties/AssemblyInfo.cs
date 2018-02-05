using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyTitle("SnAdmin (Debug)")]
#else
[assembly: AssemblyTitle("SnAdmin (Release)")]
#endif

[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Sense/Net Inc.")]
[assembly: AssemblyCopyright("Copyright © Sense/Net Inc.")]
[assembly: AssemblyProduct("sensenet ECM")]
[assembly: AssemblyTrademark("Sense/Net Inc.")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.4.4.0")]
[assembly: AssemblyFileVersion("1.4.4.0")]
[assembly: AssemblyInformationalVersion("1.4.4.0")]

[assembly: ComVisible(false)]
[assembly: Guid("1B973251-9AAE-48D2-9FFF-408AA95CA576")]

[assembly: InternalsVisibleTo("SnAdmin.Tests")]