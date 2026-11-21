using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace Alphora.Dataphor.DAE.Service
{
	public enum ServiceStatus { Running, Stopped, Unavailable, Unassigned };

	/// <summary>Registers, removes and inspects the Windows service that runs a Dataphor Server instance.</summary>
	/// <remarks>
	/// <para>
	/// Registration talks to the service control manager directly, because the .NET Framework installer classes this
	/// used to drive (System.Configuration.Install, TransactedInstaller, ServiceProcessInstaller) do not exist on
	/// .NET 10. What is registered - service name, display name, description, service type, start type, account and
	/// command line - is what 3.1 registered, so replacing the files of an existing installation in place needs no
	/// re-registration.
	/// </para>
	/// <para>
	/// Registering or reconfiguring a service needs administrative rights; reading a service's status or start mode
	/// does not. A failure reaches the caller as a Win32Exception carrying the service control manager's error code.
	/// </para>
	/// </remarks>
	public static class ServiceUtility
	{
		// Do not localize
		// The same string as Engine.ServerSourceName; kept local so that this assembly needs no reference to the engine.
		public const string EventLogSource = "Dataphor Server";

		// Do not localize: the description the service control manager shows, as 3.1 registered it.
		private const string ServiceDescriptionText = "Provides platform services for Dataphor applications.";

		/// <summary>The service binary the default registration names.</summary>
		/// <remarks>
		/// An existing 3.1 registration names this executable with exactly the argument shape GetDefaultImagePath
		/// builds, so replacing the files of an installation needs no re-registration. The two-argument Install
		/// overload registers a different binary where a caller needs one (the installer, the command-line tool).
		/// </remarks>
		private const string ServiceExecutableName = "DAEService.exe";

		public static string GetServiceName(string instanceName)
		{
			return String.Format("Alphora Dataphor ({0})", instanceName);
		}

		/// <summary>The command line a default registration carries: the service executable next to this assembly, with the instance name.</summary>
		/// <remarks>
		/// The quotes around the path are not optional: without them the service control manager splits the path at
		/// the first space, and the default installation folder has one. The folder is the running program's own
		/// (AppContext.BaseDirectory), which in every deployment holds the service executable next to the console
		/// host and the configuration utility - hence the folder rather than the calling process's own file name
		/// (Environment.ProcessPath), so that the same command line is registered whichever of them installs it.
		/// </remarks>
		private static string GetDefaultImagePath(string instanceName)
		{
			return String.Format("\"{0}\" -name \"{1}\"", Path.Combine(AppContext.BaseDirectory, ServiceExecutableName), instanceName);
		}

		/// <summary>Registers the service for the given instance, running the service executable next to this assembly.</summary>
		[SupportedOSPlatform("windows")]
		public static void Install(string instanceName)
		{
			Install(instanceName, GetDefaultImagePath(instanceName));
		}

		/// <summary>Registers the service for the given instance with the given binary path and arguments.</summary>
		/// <remarks>Fails with ERROR_SERVICE_EXISTS (1073) if the service is already registered, and with ERROR_ACCESS_DENIED (5) if the process is not elevated.</remarks>
		[SupportedOSPlatform("windows")]
		public static void Install(string instanceName, string imagePath)
		{
			string serviceName = GetServiceName(instanceName);

			IntPtr manager = OpenSCManager(null, null, ScManagerConnect | ScManagerCreateService);
			int error = Marshal.GetLastWin32Error();
			if (manager == IntPtr.Zero)
				throw new Win32Exception(error);
			try
			{
				IntPtr service =
					CreateService
					(
						manager,
						serviceName,
						serviceName,
						ServiceAllAccess,
						ServiceWin32OwnProcess,
						ServiceAutoStart,
						ServiceErrorNormal,
						imagePath,
						null,			// no load order group
						IntPtr.Zero,	// no tag identifier
						null,			// no dependencies
						null,			// LocalSystem
						null			// LocalSystem has no password
					);
				error = Marshal.GetLastWin32Error();
				if (service == IntPtr.Zero)
					throw new Win32Exception(error);
				try
				{
					// NOTE: the description is a second call, so a failure here leaves the service registered without one and
					// throws, and the next Install then fails with ERROR_SERVICE_EXISTS. The handle has full access, so there is
					// no known way for it to fail; if one turns up, delete the service before rethrowing.
					SetServiceDescription(service, ServiceDescriptionText);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(manager);
			}
		}

		/// <summary>Removes the service registration for the given instance.</summary>
		/// <remarks>Fails with ERROR_SERVICE_DOES_NOT_EXIST (1060) if the service is not registered, and with ERROR_ACCESS_DENIED (5) if the process is not elevated.</remarks>
		[SupportedOSPlatform("windows")]
		public static void Uninstall(string instanceName)
		{
			IntPtr manager = OpenSCManager(null, null, ScManagerConnect);
			int error = Marshal.GetLastWin32Error();
			if (manager == IntPtr.Zero)
				throw new Win32Exception(error);
			try
			{
				IntPtr service = OpenService(manager, GetServiceName(instanceName), ServiceDelete);
				error = Marshal.GetLastWin32Error();
				if (service == IntPtr.Zero)
					throw new Win32Exception(error);
				try
				{
					bool deleted = DeleteService(service);
					error = Marshal.GetLastWin32Error();
					if (!deleted)
						throw new Win32Exception(error);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(manager);
			}
		}

		/// <summary>Whether the service for the given instance starts with Windows; false if it is not installed.</summary>
		/// <remarks>
		/// Reads through the service control manager, which asks for query access only. Before, this opened the
		/// service's registry key for write in order to read it, so an unelevated process - the configuration utility,
		/// normally - either reported an installed service as not installed or failed outright.
		/// </remarks>
		[SupportedOSPlatform("windows")]
		public static bool GetServiceAutoStart(string instanceName)
		{
			try
			{
				using (ServiceController serviceController = new ServiceController(GetServiceName(instanceName)))
					return serviceController.StartType == ServiceStartMode.Automatic;
			}
			catch
			{
				// Not installed, or its configuration cannot be read: the answer the missing registry key gave.
				return false;
			}
		}

		/// <summary>Sets whether the service for the given instance starts with Windows; does nothing if it is not installed.</summary>
		[SupportedOSPlatform("windows")]
		public static void SetServiceAutoStart(string instanceName, bool autoStart)
		{
			IntPtr manager = OpenSCManager(null, null, ScManagerConnect);
			int error = Marshal.GetLastWin32Error();
			if (manager == IntPtr.Zero)
				throw new Win32Exception(error);
			try
			{
				IntPtr service = OpenService(manager, GetServiceName(instanceName), ServiceChangeConfig);
				error = Marshal.GetLastWin32Error();
				if (service == IntPtr.Zero)
				{
					// Not installed: nothing to write, as the missing registry key had nothing to write to.
					if (error == ErrorServiceDoesNotExist)
						return;
					throw new Win32Exception(error);
				}
				try
				{
					bool changed =
						ChangeServiceConfig
						(
							service,
							ServiceNoChange,
							autoStart ? ServiceAutoStart : ServiceDemandStart,
							ServiceNoChange,
							null, null, IntPtr.Zero, null, null, null, null
						);
					error = Marshal.GetLastWin32Error();
					if (!changed)
						throw new Win32Exception(error);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(manager);
			}
		}

		[SupportedOSPlatform("windows")]
		public static ServiceStatus GetServiceStatus(string instanceName)
		{
			try
			{
				using (ServiceController serviceController = new ServiceController(GetServiceName(instanceName)))
					switch (serviceController.Status)
					{
						case ServiceControllerStatus.Running: return ServiceStatus.Running;
						case ServiceControllerStatus.Stopped: return ServiceStatus.Stopped;
					}
			}
			catch
			{
			}

			return ServiceStatus.Unavailable;
		}

		[SupportedOSPlatform("windows")]
		private static void SetServiceDescription(IntPtr service, string description)
		{
			ServiceDescriptionInfo info = new ServiceDescriptionInfo();
			info.Description = description;
			bool changed = ChangeServiceConfig2(service, ServiceConfigDescription, ref info);
			int error = Marshal.GetLastWin32Error();
			if (!changed)
				throw new Win32Exception(error);
		}

		#region Service control manager

		// Access rights (winsvc.h). The new service handle asks for SERVICE_ALL_ACCESS so that the description can be
		// set on it; the other handles ask for only what they use.
		private const uint ScManagerConnect = 0x0001;
		private const uint ScManagerCreateService = 0x0002;
		private const uint ServiceAllAccess = 0xF01FF;
		private const uint ServiceChangeConfig = 0x0002;
		private const uint ServiceDelete = 0x10000;			// the standard right DELETE

		// Service configuration (winsvc.h): what the .NET Framework installer classes produced.
		private const uint ServiceWin32OwnProcess = 0x00000010;
		private const uint ServiceAutoStart = 0x00000002;
		private const uint ServiceDemandStart = 0x00000003;
		private const uint ServiceErrorNormal = 0x00000001;
		private const uint ServiceNoChange = 0xFFFFFFFF;
		private const uint ServiceConfigDescription = 1;

		private const int ErrorServiceDoesNotExist = 1060;

		/// <summary>SERVICE_DESCRIPTION, the argument ChangeServiceConfig2 takes for SERVICE_CONFIG_DESCRIPTION.</summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct ServiceDescriptionInfo
		{
			[MarshalAs(UnmanagedType.LPWStr)]
			public string Description;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint desiredAccess);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr CreateService
		(
			IntPtr scManager,
			string serviceName,
			string displayName,
			uint desiredAccess,
			uint serviceType,
			uint startType,
			uint errorControl,
			string binaryPathName,
			string loadOrderGroup,
			IntPtr tagId,
			string dependencies,
			string serviceStartName,
			string password
		);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr OpenService(IntPtr scManager, string serviceName, uint desiredAccess);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool ChangeServiceConfig
		(
			IntPtr service,
			uint serviceType,
			uint startType,
			uint errorControl,
			string binaryPathName,
			string loadOrderGroup,
			IntPtr tagId,
			string dependencies,
			string serviceStartName,
			string password,
			string displayName
		);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool ChangeServiceConfig2(IntPtr service, uint infoLevel, ref ServiceDescriptionInfo info);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool DeleteService(IntPtr service);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool CloseServiceHandle(IntPtr handle);

		#endregion
	}
}
