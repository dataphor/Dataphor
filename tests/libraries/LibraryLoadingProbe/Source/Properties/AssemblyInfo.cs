/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System.Reflection;

[assembly: AssemblyTitle("Alphora Dataphor - Library Loading Probe")]
[assembly: AssemblyDescription("Test library exercising LibraryAssemblyResolver's managed and native probing.")]
[assembly: Alphora.Dataphor.DAE.Server.DAERegister("Alphora.Dataphor.Diagnostics.LibraryLoadingProbe.DAERegister")]
