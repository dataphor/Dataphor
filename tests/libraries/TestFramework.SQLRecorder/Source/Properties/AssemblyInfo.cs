/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System.Reflection;

[assembly: AssemblyTitle("Alphora Dataphor - SQL Recorder")]
[assembly: AssemblyDescription("Test library whose connection class records the SQL a device generates instead of sending it to a database server.")]
[assembly: Alphora.Dataphor.DAE.Server.DAERegister("Alphora.Dataphor.DAE.SQLRecorder.DAERegister")]
