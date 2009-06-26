using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Alphora Dataphor - Native CLI")]
[assembly: AssemblyDescription("Dataphor Native CLI")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Alphora")]
[assembly: AssemblyProduct("Dataphor")]
[assembly: AssemblyCopyright("© Copyright 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f8e0c8cd-15bf-45d7-a076-dbc1ddc61fd1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.2.*")]

[assembly: AssemblyDelaySign(false)]
#if SIGNASSEMBLIES
[assembly: AssemblyKeyFile("..\\..\\..\\KeyFile.snk")]
#else
[assembly: AssemblyKeyFile("")]
#endif
[assembly: AssemblyKeyName("")]