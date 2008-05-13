/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.ComponentModel.Design.Serialization;
using System.Configuration;

namespace Alphora.Dataphor.DAE.Server
{
	#if !DAC
	/// <summary> Encapsulates a Dataphor DAE Server into a startable/stopable service with Tcp settings. </summary>
	[ToolboxItem(true)]
	[DefaultProperty("PortNumber")]
	[DesignerSerializer(typeof(Alphora.Dataphor.DAE.Server.PropertyLastSerializer), typeof(CodeDomSerializer))]
	public class ServerService : System.ComponentModel.Component
	{
		// Do not localize
		public const int CDefaultPortNumber = 8061;
		public const string CServiceConfigFileName = "DataphorService.config";

		public ServerService()
		{
			InternalInitialize();
		}

		public ServerService(IContainer AContainer)
		{
			InternalInitialize();
			if (AContainer != null)
				AContainer.Add(this);
		}

		private void InternalInitialize()
		{
			FPortNumber = CDefaultPortNumber;
			FServerName = Server.CDefaultServerName;
		}

		protected override void Dispose(bool ADisposing)
		{
			Stop();
			base.Dispose(ADisposing);
		}

		private int FPortNumber;
		/// <summary> The TCP port the Dataphor DAE Server will listen on. </summary>
		/// <remarks> If the server is already started, changing the port will reset the tcp channel. </remarks>
		[Description("The TCP port the Dataphor DAE Server will listen on.")]
		[DefaultValue(CDefaultPortNumber)]
		[Category("Configuration")]
		public int PortNumber
		{
			get { return FPortNumber; }
			set 
			{
				if (FPortNumber != value)
				{
					CheckNotStarted();
					FPortNumber = value;
				}
			}
		}
		
		private bool FTracingEnabled = true;
		/// <summary>Determines whether or not the server will use the Dataphor application event log to report service events.</summary>
		[Description("Determines whether or not the server will use the Dataphor application event log to report service events.")]
		[DefaultValue(true)]
		[Category("Configuration")]
		public bool TracingEnabled
		{
			get { return FTracingEnabled; }
			set 
			{
				if (FTracingEnabled != value)
				{
					FTracingEnabled = value; 
					if (FServer != null)
						FServer.TracingEnabled = FTracingEnabled;
				}
			}
		}

		private bool FLogErrors = false;
		/// <summary>Determines whether or not the server will use the Dataphor application event log to report errors returned to clients.</summary>
		[Description("Determines whether or not the server will use the Dataphor application event log to report errors returned to clients.")]
		[DefaultValue(false)]
		[Category("Configuration")]
		public bool LogErrors
		{
			get { return FLogErrors; }
			set 
			{
				if (FLogErrors != value)
				{
					FLogErrors = value; 
					if (FServer != null)
						FServer.LogErrors = FLogErrors;
				}
			}
		}

		private string FStartupScriptUri = String.Empty;
		/// <summary> The D4 script to execute when the DAE server is started. </summary>
		[Description("The D4 script to execute when the DAE server is started.")]
		[DefaultValue("")]
		[Category("Configuration")]
		public string StartupScriptUri
		{
			get { return FStartupScriptUri; }
			set 
			{ 
				if (FStartupScriptUri != value)
				{
					CheckNotStarted();
					FStartupScriptUri = value; 
				}
			}
		}
		
		// CatalogStoreClassName
		private string FCatalogStoreClassName;
		/// <summary>
		/// Specifies the assembly-qualified class name of the store used to persist the system catalog.
		/// </summary>
		[Description("Specifies the assembly-qualified class name of the store used to persist the system catalog.")]
		[Category("Configuration")]
		public string CatalogStoreClassName
		{
			get { return FCatalogStoreClassName; }
			set
			{
				CheckNotStarted();
				FCatalogStoreClassName = value;
			}
		}
		
		// CatalogStoreConnectionString
		private string FCatalogStoreConnectionString;
		/// <summary>
		/// Specifies the connection string used to connect to the system catalog store.
		/// </summary>
		[Description("Specifies the connection string used to connect to the system catalog store.")]
		[Category("Configuration")]
		public string CatalogStoreConnectionString
		{
			get { return FCatalogStoreConnectionString; }
			set
			{
				CheckNotStarted();
				FCatalogStoreConnectionString = value;
			}
		}
		

		// CatalogDirectory
		private string FCatalogDirectory = String.Empty;
		/// <summary> Specifies the directory used to persist the DAE catalog (schema). </summary>
		/// <remarks>
		///		Default to empty.  Use <see cref="SetCatalogDirectoryToDefault"/> to point the file to 
		///		the system's default DAE server catalog.  The DAE server must not be started or an
		///		exception will be thrown.
		///	</remarks>
		[Description("Specifies the directory used to persist the DAE catalog (schema). This setting is for backwards-compatibility; use CatalogStoreConnectionString instead.")]
		[Category("Configuration")]
		public string CatalogDirectory
		{
			get { return FCatalogDirectory; }
			set
			{
				CheckNotStarted();
				FCatalogDirectory = value;
			}
		}

		/// <summary> Sets the <see cref="CatalogDirectory"/> to the default DAE catalog directory for the system. </summary>
		public void SetCatalogDirectoryToDefault()
		{
			CatalogDirectory = Server.GetDefaultCatalogDirectory();
		}

		private string FCatalogStoreDatabaseName = "DAECatalog";
		/// <summary>The name of the database to be used for the catalog store.</summary>
		[DefaultValue("DAECatalog")]
		[Description("The name of the database to be used for the catalog store. This setting is for backwards-compatibility; use CatalogStoreConnectionString instead.")]
		public string CatalogStoreDatabaseName
		{
			get { return FCatalogStoreDatabaseName; }
			set 
			{
				CheckNotStarted(); 
				if ((value == null) || (value == String.Empty))
					FCatalogStoreDatabaseName = "DAECatalog";
				else
					FCatalogStoreDatabaseName = value;
			}
		}

		private string FCatalogStorePassword = String.Empty;
		/// <summary>If not using integrated security, the password to be used to connect to the catalog store.</summary>
		[DefaultValue("")]
		[Description("If not using integrated security, the password to be used to connect to the catalog store. This setting is for backwards-compatibility; use CatalogStoreConnectionString instead.")]
		public string CatalogStorePassword
		{
			get { return FCatalogStorePassword; }
			set 
			{
				CheckNotStarted(); 
				if ((value == null) || (value == String.Empty))
					FCatalogStorePassword = "";
				else
					FCatalogStorePassword = value;
			}
		}
		
		// LibraryDirectory
		private string FLibraryDirectory = String.Empty;
		/// <summary> Specifies the directory for the Libraries used by the DAE. </summary>
		/// <remarks>
		///		Default to the Libraries directory is within the same directory where the DAE
		///		Service is running.
		///	</remarks>
		[Description("Specifies the directory used for the DAE Libraries.")]
		[Category("Service")]
		public string LibraryDirectory
		{
			get { return FLibraryDirectory; }
			set
			{
				CheckNotStarted();
				FLibraryDirectory = value;
			}
		}

		// ServerName
		private string FServerName;
		/// <summary> The name of the DAE server. </summary>
		/// <remarks> This is used both for internal naming and for Tcp registration. </remarks>
		[DefaultValue(Server.CDefaultServerName)]
		[Category("Service")]
		public string ServerName
		{
			get { return FServerName; }
			set
			{
				if (FServerName != value)
				{
					CheckNotStarted();
					FServerName = value; 
				}
			}
		}

		private void CheckNotStarted()
		{
			if (Started)
				throw new ServerException(ServerException.Codes.ServerIsStarted);
		}
		
		private ServerHost FServerHost;

		private Server FServer;
		[BOP.Publish(BOP.PublishMethod.None)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public Server Server
		{
			get { return FServer; }
		}

		/// <summary> Starts the DAE Server (if not already started). </summary>
		public void Start()
		{
			if (!Started)
			{
				FServer = new Server();
				try
				{
					FServer = new Server();
					FServer.Name = ServerName;
					FServer.LogErrors = FLogErrors;
					FServer.TracingEnabled = FTracingEnabled;
					FServer.StartupScriptUri = StartupScriptUri;
					FServer.CatalogStoreClassName = CatalogStoreClassName;
					FServer.CatalogStoreConnectionString = CatalogStoreConnectionString;
					FServer.CatalogDirectory = CatalogDirectory;
					FServer.CatalogStoreDatabaseName = CatalogStoreDatabaseName;
					FServer.CatalogStorePassword = CatalogStorePassword;
					FServer.LibraryDirectory = LibraryDirectory;
					FServer.Start();

					FServerHost = new ServerHost(FServer, FPortNumber);				
				}
				catch
				{
					FServer.Dispose();
					FServer = null;
					throw;
				}
			}
		}

		/// <summary> Stops the DAE Server (if started). </summary>
		public void Stop()
		{
			if (Started)
			{
				try
				{
					FServerHost.Dispose();
					FServerHost = null;
				}
				finally
				{
					FServer.Dispose();
					FServer = null;
				}
			}
		}

		/// <summary> True when the server is started (active). </summary>
		[DefaultValue(false)]
		[Description("The state of the current DAE server")]
		[Category("Configuration")]
		public bool Started
		{
			get { return FServer != null; }
			set
			{
				if (Started != value)
				{
					if (value)
						Start();
					else
						Stop();
				}
			}
		}

		/// <summary>Get a ServiceController object that allows access to the DAE Service.</summary>
		/// <returns>System.ServiceProcess.ServiceController</returns>
		static public System.ServiceProcess.ServiceController GetService()
		{
			System.ServiceProcess.ServiceController LService = new System.ServiceProcess.ServiceController(GetServiceName());
			try
			{
				// This is retarded, but it's the only way to check if the service exists.
				System.ServiceProcess.ServiceControllerStatus LStatus = LService.Status;
			}
			catch
			{
				return null;
			}

			return LService;
		}

		public static string GetServiceConfigFileName()
		{
			string LFileName = null;
			IDictionary LSettings = (IDictionary)System.Configuration.ConfigurationManager.GetSection("dataphor");
			if (LSettings != null)
				LFileName = (string)LSettings["configurationFileName"];
			if (LFileName == null)
				return PathUtility.CommonAppDataPath(String.Empty, true) + CServiceConfigFileName;
			else
				return LFileName;
		}

		public static string GetServiceName()
		{
			Version LVersion = Assembly.Load("Alphora.Dataphor.DAE", null).GetName().Version;
			return String.Format("Alphora Dataphor {0}.{1}", LVersion.Major, LVersion.Minor);
		}
	}
	#endif
}