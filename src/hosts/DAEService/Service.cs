/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Runtime.Versioning;

namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>The Windows service name of the host program, which is <see cref="DataphorHost"/>; DAEServer.exe runs the same program.</summary>
	/// <remarks>
	/// The class no longer derives from ServiceBase and has no instance members: UseWindowsService gives the host a
	/// Windows service lifetime, so one program serves both a console session and the service control manager. The type
	/// name and the file name are unchanged because an existing service registration names this executable and the
	/// project's StartupObject names this type.
	/// </remarks>
	public class Service
	{
		// The program is Windows-only: it registers a Windows service and reads the Windows event log.
		[SupportedOSPlatform("windows")]
		static int Main(string[] args)
		{
			return DataphorHost.Run(args);
		}
	}
}
