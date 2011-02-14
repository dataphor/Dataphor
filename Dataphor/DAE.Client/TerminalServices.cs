/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.DAE.Client
{
	public static class TerminalServiceUtility
	{
		public static bool InTerminalSession
		{
			#if SILVERLIGHT
			get { return false; }
			#else
			get { return System.Windows.Forms.SystemInformation.TerminalServerSession; }
			#endif
		}
		
		public static string ClientName 
		{ 
			get 
			{ 
				#if SILVERLIGHT
				return "";
				#else
				if (InTerminalSession)
				{
					IntPtr buffer;
					uint size;
					if (!WTSQuerySessionInformation(IntPtr.Zero, WTS_CURRENT_SESSION, WTS_INFO_CLASS.WTSClientName, out buffer, out size))
						return System.Environment.MachineName;
					else
						try
						{
							return Marshal.PtrToStringAnsi(buffer);
						}
						finally
						{
							WTSFreeMemory(buffer);
						}
				}
				else
					return System.Environment.MachineName;
				#endif
			}
		}

		#if !SILVERLIGHT
		public const int WTS_CURRENT_SERVER_HANDLE = -1;
		public const int WTS_CURRENT_SESSION = -1;

		private enum WTS_INFO_CLASS
		{
			WTSInitialProgram,
			WTSApplicationName,
			WTSWorkingDirectory,
			WTSOEMId,
			WTSSessionId,
			WTSUserName,
			WTSWinStationName,
			WTSDomainName,
			WTSConnectState,
			WTSClientBuildNumber,
			WTSClientName,
			WTSClientDirectory,
			WTSClientProductId,
			WTSClientHardwareId,
			WTSClientAddress,
			WTSClientDisplay,
			WTSClientProtocolType
		}

		[DllImport("Wtsapi32.dll")]
		private static extern bool WTSQuerySessionInformation(System.IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass, out System.IntPtr ppBuffer, out uint pBytesReturned);

		[DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
		private static extern void WTSFreeMemory(IntPtr memory);
		#endif
	}
}
