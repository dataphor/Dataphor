/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.Versioning;

namespace DAEServer
{
	using Alphora.Dataphor.DAE.Service;

	/// <summary>The console name of the host program, which is <see cref="DataphorHost"/>; DAEService.exe runs the same program.</summary>
	public class Program
	{
		// The program is Windows-only: it registers a Windows service and reads the Windows event log.
		[SupportedOSPlatform("windows")]
		static int Main(string[] args)
		{
			return DataphorHost.Run(args);
		}
	}
}
