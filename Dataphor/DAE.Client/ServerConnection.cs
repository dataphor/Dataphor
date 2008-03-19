/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	/// <summary> A delegate definition to handle name changes. </summary>
	public delegate void AliasNameChangeHandler(object ASender, string AOldName, string ANewName);

	/// <summary> The abstract base class for modeling connection aliases to a Dataphor Server. </summary>
	/// <remarks>
	/// A connection alias provides a .NET class that models all the information necessary to connect to a Dataphor
	/// Server. There are two main varieties of aliases, the in-process, and the out-of-process. An In-process alias
	/// actually constructs a Dataphor Server in-process with the given configuration, and then connects to that
	/// server. An out-of-process alias connects to an existing Dataphor Server running in another application domain
	/// either as part of some other application, or as a service. For more information on the configuring and using
	/// connection aliases, refer to the Dataphor User's Guide.
	/// </remarks>
	public abstract class ServerAlias
	{
		public ServerAlias()
		{
			FName = Strings.Get("CDefaultAliasName");
		}

		// SessionInfo

		/// <summary> This type converter hides the password from the property grid. </summary>
		private class SessionInfoConverter : TypeConverter
		{
			public override bool GetPropertiesSupported(ITypeDescriptorContext AContext)
			{
				return true;
			}

			public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext AContext, object AInstance, Attribute[] AAttributes)
			{
				PropertyDescriptorCollection LCollection = TypeDescriptor.GetProperties(AInstance, AAttributes);
				PropertyDescriptor[] LFiltered = new PropertyDescriptor[LCollection.Count - 1];
				int LCollectionIndex = 0;
				for (int i = 0; i < LFiltered.Length; i++)
				{
					if (LCollection[LCollectionIndex].Name == "Password")
						LCollectionIndex++;
					LFiltered[i] = LCollection[LCollectionIndex];
					LCollectionIndex++;
				}
				return new PropertyDescriptorCollection(LFiltered);
			}
		}

		[System.ComponentModel.TypeConverter(typeof(SessionInfoConverter))]
		private class InternalSessionInfo : SessionInfo
		{
		}

		private SessionInfo FSessionInfo = new InternalSessionInfo();
		[Publish(PublishMethod.Inline)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		[System.ComponentModel.TypeConverter(typeof(SessionInfoConverter))]
		[Description("Contextual information for server session initialization")]
		public SessionInfo SessionInfo
		{
			get { return FSessionInfo; }
		}

		// OnNameChanged

		public event AliasNameChangeHandler OnNameChanging;

		// Name

		private string FName;
		/// <summary> The name of the server alias. </summary>
		/// <remarks>
		/// This name uniquely identifies this alias, and allows clients
		/// to connect to a Dataphor Server by referencing only the alias
		/// name and optionally providing authentication information.
		/// Note that alias names are case-insensitive.
		/// </remarks>
		public string Name
		{
			get { return FName; }
			set 
			{
				if (value != FName)
				{
					if (OnNameChanging != null)
						OnNameChanging(this, FName, value);
					FName = value; 
				}
			}
		}

		// PortNumber

		private int FPortNumber = ServerService.CDefaultPortNumber;
		/// <summary> The port number configured for this alias. </summary>
		/// <remarks>
		/// The port number that will be used to communicate with the Dataphor Server.
		/// For an in-process alias, this is the port that the server will use
		/// to listen for incoming connections. For an out-of-process alias, this
		/// port number must be the same as the configured port number for the
		/// server that is being connected to. The default port for a Dataphor
		/// Server is 8061.
		/// </remarks>
		[DefaultValue(ServerService.CDefaultPortNumber)]
		public int PortNumber
		{
			get { return FPortNumber; }
			set { FPortNumber = value; }
		}
		
		// IsUserAlias
		
		private bool FIsUserAlias = true;
		/// <summary> Determines whether or not this is a user-specified alias. </summary>
		[DefaultValue(true)]
		public bool IsUserAlias 
		{ 
			get { return FIsUserAlias; } 
			set { FIsUserAlias = value; } 
		}
	}

	/// <summary> A ServerAlias descendent that models an out-of-process connection to a Dataphor Server. </summary>
	public class ConnectionAlias : ServerAlias
	{
		public const string CDefaultHostName = "localhost";

		private string FHostName = CDefaultHostName;
		/// <summary> The host name or IP address of the server to connect. </summary>
		/// <remarks>
		/// This is the computer name or IP address of the machine on which the Dataphor Server is running.
		/// The default value of 'localhost' can be used if the Dataphor Server is running in another
		/// application or service on the same machine as the client.
		/// </remarks>
		[DefaultValue(CDefaultHostName)]
		[Description("The host name or IP address of the server.")]
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value; }
		}
		
		private bool FClientSideLoggingEnabled;
		/// <summary> Whether or not to perform client-side logging. </summary>
		/// <remarks>
		/// This property determines whether or not client-side logging will be enabled for this connection.
		/// If enabled, a log file will be created at the following path: 
		/// <&lt;>Common Application Data<&gt;>\Alphora\Dataphor\Dataphor.log
		/// </remarks>
		[DefaultValue(false)]
		[Description("Whether or not to perform client-side logging.")]
		public bool ClientSideLoggingEnabled
		{
			get { return FClientSideLoggingEnabled; }
			set { FClientSideLoggingEnabled = value; }
		}

		public override string ToString()
		{
			return String.Format("{0} ({1}:{2})", Name, FHostName, PortNumber.ToString());
		}
	}

	/// <summary> A ServerAlias descendent that models an in-process connection to a Dataphor Server. </summary>
	/// <remarks>
	/// <para>
	/// An in-process connection will construct a Dataphor Server with the given configuration, and then connect
	/// to that server directly. Once this server has started, it will be available to other applications in the
	/// same way that a server running as a service is available. Note that the port number configured for use
	/// by an in-process Dataphor Server must not be in use by some other server on the same machine. The 
	/// combination of a machine name and port number constitute a unique identifier for a Dataphor Server within
	/// a given network scope.
	/// </para>
	/// <para>For more information on configuring and using an in-process server, refer to the Dataphor User's Guide.</para>
	/// </remarks>
	public class InProcessAlias : ServerAlias
	{
		private string FStartupScriptUri = String.Empty;
		/// <summary>The URI to a startup script to run while initializing the server.</summary>
		[Description("The URI to a startup script to run while initializing the server.")]
		[DefaultValue("")]
		public string StartupScriptUri
		{
			get { return FStartupScriptUri; }
			set { FStartupScriptUri = value; }
		}

		private string FCatalogDirectory = String.Empty;
		/// <summary>The directory used by the Dataphor Server to persist it's catalog (schema).</summary>
		[DefaultValue("")]
		[Description("The directory used by the Dataphor Server to persist it's catalog (schema).")]
		public string CatalogDirectory
		{
			get { return FCatalogDirectory; }
			set { FCatalogDirectory = value; }
		}
		
		private string FCatalogStoreDatabaseName = "DAECatalog";
		/// <summary>The name of the database to be used for the catalog store.</summary>
		[DefaultValue("DAECatalog")]
		[Description("The name of the database to be used for the catalog store.")]
		public string CatalogStoreDatabaseName
		{
			get { return FCatalogStoreDatabaseName; }
			set 
			{ 
				if ((value == null) || (value == String.Empty))
					FCatalogStoreDatabaseName = "DAECatalog";
				else
					FCatalogStoreDatabaseName = value;
			}
		}

		private string FCatalogStorePassword = String.Empty;
		/// <summary>If not using integrated security, the password to be used to connect to the catalog store.</summary>
		[DefaultValue("")]
		[Description("If not using integrated security, the password to be used to connect to the catalog store.")]
		public string CatalogStorePassword
		{
			get { return FCatalogStorePassword; }
			set 
			{ 
				if ((value == null) || (value == String.Empty))
					FCatalogStorePassword = "";
				else
					FCatalogStorePassword = value;
			}
		}
		
		private bool FCatalogStoreShared = false;
		/// <summary>This property is deprecated. Setting it will have no effect. It is retained only for backwards compatibility with existing alias definitions.</summary>
		[DefaultValue(false)]
		[Description("This property is deprecated. Setting it will have no effect. It is retained only for backwards compatibility with existing alias definitions.")]
		public bool CatalogStoreShared
		{
			get { return FCatalogStoreShared; }
			set { FCatalogStoreShared = value; }
		}
		
		private string FLibraryDirectory = String.Empty;
		/// <summary>The directory used for Dataphor Server libraries.</summary>
		[DefaultValue("")]
		[Description("The directory used for Dataphor Server libraries.")]
		public string LibraryDirectory
		{
			get { return FLibraryDirectory; }
			set { FLibraryDirectory = value; }
		}
		
		private bool FTracingEnabled = true;
		/// <summary>Determines whether the Dataphor Server will use the Dataphor event log to write service events.</summary>
		[DefaultValue(true)]
		[Description("Determines whether the Dataphor Server will use the Dataphor event log to write service events.")]
		public bool TracingEnabled
		{
			get { return FTracingEnabled; }
			set { FTracingEnabled = value; }
		}

		private bool FLogErrors = false;
		/// <summary>Determines whether the Dataphor Server will use the Dataphor event log to write errors returned to clients.</summary>
		[DefaultValue(false)]
		[Description("Determines whether the Dataphor Server will use the Dataphor event log to write errors returned to clients.")]
		public bool LogErrors
		{
			get { return FLogErrors; }
			set { FLogErrors = value; }
		}
		
		private bool FIsEmbedded = false;
		/// <summary>Determines whether the Dataphor Server is intended to be used as a single-user data access engine embedded in a client application.</summary>
		[DefaultValue(false)]
		[Description("Determines whether the Dataphor Server is intended to be used as a single-user data access engine embedded in a client application.")]
		public bool IsEmbedded
		{
			get { return FIsEmbedded; }
			set { FIsEmbedded = value; }
		}
		
		public override string ToString()
		{
			return String.Format("{0} ({1}) - {2}", Name, PortNumber.ToString(), Strings.Get("CInProcess"));
		}
	}
	
	// Alias Manager
	/// <summary>Provides a class for managing the set of aliases available on this machine.</summary>
	/// <remarks>
	/// The Alias Manager provides a mechanism for obtaining the alias configuration for a process.
	/// The default behavior of the alias manager provides for machine- and user-specific aliases.
	/// If a user-specific alias has the same name as a machine-specific alias, the user-specific
	/// alias overrides the machine-specific alias.
	/// The default alias manager uses multiple alias configuration files:
	///	Machine specific: <common application data path>\Alphora\Dataphor\ServerAliases.config
	/// User specific: <user application data path>\Alphora\Dataphor\ServerAliases.config
	///
	/// Note that to retain backwards compatibility with previous versions of the Dataphor product,
	/// if a version-specific user configuration file exists, it will be used, with a duplicate
	/// of the configuration saved in the non-version-specific directory. Otherwise, only the
	/// non-version-specific file will be used.
	/// </remarks>
	public class AliasManager
	{
		// Do not localize
		public const string CAliasConfigurationFileName = "ServerAliases.config";

		private static string FDefaultAliasName;		
		public static string DefaultAliasName
		{
			get 
			{ 
				CheckLoaded();
				return FDefaultAliasName; 
			}
			set 
			{ 
				CheckLoaded();
				FDefaultAliasName = value; 
			}
		}

		private static AliasList FAliases;
		public static AliasList Aliases 
		{ 
			get 
			{ 
				CheckLoaded();
				return FAliases; 
			} 
		}
		
		private static AliasList FOverriddenAliases;
		
		private static string GetMachineAliasConfigurationFileName()
		{
			return PathUtility.CommonAppDataPath(String.Empty, VersionModifier.None) + '\\' + CAliasConfigurationFileName;
		}
		
		private static AliasConfiguration LoadMachineAliasConfiguration()
		{
			return AliasConfiguration.Load(GetMachineAliasConfigurationFileName());
		}
		
		private static string GetUserAliasConfigurationFileName()
		{
			return PathUtility.UserAppDataPath(String.Empty, VersionModifier.None) + '\\' + CAliasConfigurationFileName;
		}
		
		private static string GetVersionSpecificUserAliasConfigurationFileName()
		{
			return PathUtility.UserAppDataPath() + '\\' + CAliasConfigurationFileName;
		}
		
		private static AliasConfiguration LoadUserAliasConfiguration()
		{
			// If a version specific file exists, it will be used, and an identical copy will be saved to the non-version specific path
			// Otherwise, only the non-versioned path will be used.
			
			string LVersionFileName = GetVersionSpecificUserAliasConfigurationFileName();
				
			if (File.Exists(LVersionFileName))
				return AliasConfiguration.Load(LVersionFileName);
			else
				return AliasConfiguration.Load(GetUserAliasConfigurationFileName());
		}
		
		private static void SaveUserAliasConfiguration(AliasConfiguration AConfiguration)
		{
			string LVersionFileName = GetVersionSpecificUserAliasConfigurationFileName();
			
			if (File.Exists(LVersionFileName))
				AConfiguration.Save(LVersionFileName);
			else
				AConfiguration.Save(GetUserAliasConfigurationFileName());
		}
		
		private static void SaveMachineAliasConfiguration(AliasConfiguration AConfiguration)
		{
			AConfiguration.Save(GetMachineAliasConfigurationFileName());
		}
		
		public static void Load()
		{
			Reset();

			// Load Machine Aliases
			AliasConfiguration LMachineConfiguration = LoadMachineAliasConfiguration();
			foreach (ServerAlias LAlias in LMachineConfiguration.Aliases.Values)
				FAliases.Add(LAlias);
			
			// Load User Aliases
			AliasConfiguration LUserConfiguration = LoadUserAliasConfiguration();
			foreach (ServerAlias LAlias in LUserConfiguration.Aliases.Values)
			{
				ServerAlias LMachineAlias = FAliases[LAlias.Name];
				if (LMachineAlias != null)
				{
					FOverriddenAliases.Add(LMachineAlias);
					FAliases.Remove(LAlias.Name);
				}
				
				FAliases.Add(LAlias);
			}

			FDefaultAliasName = LUserConfiguration.DefaultAliasName;
		}
		
		private static void CheckLoaded()
		{
			if (FAliases == null)
				throw new ClientException(ClientException.Codes.AliasConfigurationNotLoaded);
		}
		
		public static void Save()
		{
			CheckLoaded();
			
			AliasConfiguration LUserConfiguration = new AliasConfiguration();
			AliasConfiguration LMachineConfiguration = new AliasConfiguration();
			
			LUserConfiguration.DefaultAliasName = FDefaultAliasName;
			foreach (ServerAlias LAlias in FAliases.Values)
				if (LAlias.IsUserAlias)
					LUserConfiguration.Aliases.Add(LAlias);
				else
					LMachineConfiguration.Aliases.Add(LAlias);
			
			foreach (ServerAlias LAlias in FOverriddenAliases.Values)
				if (!LMachineConfiguration.Aliases.ContainsKey(LAlias.Name))
					LMachineConfiguration.Aliases.Add(LAlias);

			SaveUserAliasConfiguration(LUserConfiguration);
			SaveMachineAliasConfiguration(LMachineConfiguration);
		}

		private static void Reset()
		{
			FDefaultAliasName = String.Empty;
			FAliases = new AliasList();
			FOverriddenAliases = new AliasList();
		}
		
		public static ServerAlias GetAlias(string AAliasName)
		{
			if (FAliases == null)
				Load();
			return Aliases.GetAlias(AAliasName);
		}
		
		public static AliasConfiguration LoadConfiguration()
		{
			AliasConfiguration LConfiguration = new AliasConfiguration();
			Load();
			LConfiguration.DefaultAliasName = FDefaultAliasName;
			foreach (ServerAlias LAlias in FAliases.Values)
				LConfiguration.Aliases.Add(LAlias);
			return LConfiguration;
		}
		
		public static void SaveConfiguration(AliasConfiguration AConfiguration)
		{
			CheckLoaded();
			FAliases.Clear();
			foreach (ServerAlias LAlias in AConfiguration.Aliases.Values)
				FAliases.Add(LAlias);
			FDefaultAliasName = AConfiguration.DefaultAliasName;
			Save();
		}
	}
	
	// Alias configuration
	[PublishDefaultList("Aliases")]
	public class AliasConfiguration
	{
		private string FDefaultAliasName = String.Empty;
		[DefaultValue("")]
		public string DefaultAliasName
		{
			get { return FDefaultAliasName; }
			set { FDefaultAliasName = value; }
		}

		private AliasList FAliases = new AliasList();
		public AliasList Aliases
		{
			get { return FAliases; }
		}
		
		/// <summary> Loads a new alias configuration. </summary>
		/// <remarks> Creates a default alias configuration if the file doesn't exist. </remarks>
		public static AliasConfiguration Load(string AFileName)
		{
			AliasConfiguration LConfiguration = new AliasConfiguration();
			if (File.Exists(AFileName))
				using (Stream LStream = File.OpenRead(AFileName))
				{
					new Deserializer().Deserialize(LStream, LConfiguration);
				}
			return LConfiguration;
		}
		
		public void Save(string AFileName)
		{
			using (Stream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				new Serializer().Serialize(LStream, this);
			}
		}

		// NOTE: These custom serializers guarantee backwards compatibility of the Aliases config file with previous builds of the 2.1 branch.
		// NOTE: Because the Aliases are now in the DAE.Client namespace, i/o the Frontend.Client namespace, the new deserializer will fail to load the
		// NOTE: types of a config file written from an old serializer. These overrides force the new config file to be written the same way that the old
		// NOTE: deserializer will write it, allowing both versions to work together.
		private class Serializer : BOP.Serializer
		{
			protected override string GetElementNamespace(Type AType)
			{
				return "Alphora.Dataphor.Frontend.Client,Alphora.Dataphor.Frontend.Client";
			}
		}
		
		private class Deserializer : BOP.Deserializer
		{
			protected override Type GetClassType(string AName, string ANamespace)
			{
				if (String.Compare(AName, "AliasConfiguration", true) == 0)
					return typeof(AliasConfiguration);
				if (String.Compare(AName, "ConnectionAlias", true) == 0)
					return typeof(ConnectionAlias);
				if (String.Compare(AName, "InProcessAlias", true) == 0)
					return typeof(InProcessAlias);
				return base.GetClassType(AName, ANamespace);
			}
		}
	}

	/// <summary> List of server aliases. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class AliasList : HashtableList
	{
		public AliasList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object AValue)
		{
			ServerAlias LAlias = (ServerAlias)AValue;
			Add(LAlias.Name, LAlias);
			return IndexOf(LAlias.Name);
		}

		public new ServerAlias this[int AIndex]
		{
			get { return (ServerAlias)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public ServerAlias this[string AAliasName]
		{
			get { return (ServerAlias)base[AAliasName]; }
			set { base[AAliasName] = value; }
		}
		
		public ServerAlias GetAlias(string AAliasName)
		{
			ServerAlias LAlias = this[AAliasName];
			if (LAlias == null)
				throw new ClientException(ClientException.Codes.AliasNotFound, AAliasName);
			return LAlias;
		}
		
		public bool HasAlias(string AAliasName)
		{
			return this[AAliasName] != null;
		}
	}

	/// <summary>Manages the connection to a server based on a given server alias definition.</summary>
	/// <remarks>The ServerConnection class manages the differences between in-process and out-of-process
	/// aliases, allowing clients to connect using only an alias name and a server connection. This
	/// class is used by the DataSession to establish a connection to a server based on an alias definition.
	/// </remarks>
	public class ServerConnection : Disposable
	{
		public ServerConnection(IServer AExistingServer)
		{
			FExistingServer = AExistingServer;
		}
		
		public ServerConnection(ServerAlias AServerAlias) : this(AServerAlias, true) {}
		
		public ServerConnection(ServerAlias AServerAlias, bool AAutoStart)
		{
			if (AServerAlias == null)
				throw new ClientException(ClientException.Codes.ServerAliasRequired);
				
			FServerAlias = AServerAlias;
			InProcessAlias LInProcessAlias = FServerAlias as InProcessAlias;
			ConnectionAlias LConnectionAlias = FServerAlias as ConnectionAlias;
			try
			{
				if (LInProcessAlias != null)
				{
					FHostedServer = new Server.Server();
					FHostedServer.Name = Alphora.Dataphor.DAE.Server.Server.CDefaultServerName;
					FHostedServer.LogErrors = LInProcessAlias.LogErrors;
					FHostedServer.TracingEnabled = LInProcessAlias.TracingEnabled;
					FHostedServer.StartupScriptUri = LInProcessAlias.StartupScriptUri;
					FHostedServer.CatalogDirectory = LInProcessAlias.CatalogDirectory;
					FHostedServer.CatalogStoreDatabaseName = LInProcessAlias.CatalogStoreDatabaseName;
					FHostedServer.CatalogStorePassword = LInProcessAlias.CatalogStorePassword;
					FHostedServer.LibraryDirectory = LInProcessAlias.LibraryDirectory;
					FHostedServer.IsEmbedded = LInProcessAlias.IsEmbedded;
					FHostedServer.OnDeviceStarting += new DeviceNotifyEvent(FHostedServer_OnDeviceStarting);
					FHostedServer.OnDeviceStarted += new DeviceNotifyEvent(FHostedServer_OnDeviceStarted);
					FHostedServer.OnLibraryLoading += new LibraryNotifyEvent(FHostedServer_OnLibraryLoading);
					FHostedServer.OnLibraryLoaded += new LibraryNotifyEvent(FHostedServer_OnLibraryLoaded);
					if (!LInProcessAlias.IsEmbedded)
						FServerHost = new ServerHost(FHostedServer, LInProcessAlias.PortNumber);
					if (AAutoStart)
						FHostedServer.Start();
				}
				else
				{
					FRemoteServer = 
						ServerFactory.Connect
						(
							String.Format("tcp://{0}:{1}/Dataphor", LConnectionAlias.HostName, LConnectionAlias.PortNumber), 
							LConnectionAlias.ClientSideLoggingEnabled,
							TerminalServiceUtility.ClientName
						);
				}
			}
			catch
			{
				CleanUp();
				throw;
			}
		}

		// OnDeviceStarting
		
		public event DeviceNotifyEvent OnDeviceStarting;
		
		private void FHostedServer_OnDeviceStarting(Server.Server AServer, Schema.Device ADevice)
		{
			if (OnDeviceStarting != null)
				OnDeviceStarting(AServer, ADevice);
		}
		
		// OnDeviceStarted
		
		public event DeviceNotifyEvent OnDeviceStarted;
		
		private void FHostedServer_OnDeviceStarted(Server.Server AServer, Schema.Device ADevice)
		{
			if (OnDeviceStarted != null)
				OnDeviceStarted(AServer, ADevice);
		}
		
		// OnLibraryLoading
		
		public event LibraryNotifyEvent OnLibraryLoading;

		private void FHostedServer_OnLibraryLoading(Server.Server AServer, string ALibraryName)
		{
			if (OnLibraryLoading != null)
				OnLibraryLoading(AServer, ALibraryName);
		}
		
		// OnLibraryLoaded
		
		public event LibraryNotifyEvent OnLibraryLoaded;

		private void FHostedServer_OnLibraryLoaded(Server.Server AServer, string ALibraryName)
		{
			if (OnLibraryLoaded != null)
				OnLibraryLoaded(AServer, ALibraryName);
		}

		protected override void Dispose(bool ADisposed)
		{
			base.Dispose(ADisposed);
			CleanUp();
		}

		private void CleanUp()
		{
			if (FExistingServer != null)
				FExistingServer = null;
				
			if (FServerHost != null)
			{
				FServerHost.Dispose();
				FServerHost = null;
			}
			
			if (FHostedServer != null)
			{
				FHostedServer.OnDeviceStarting -= new DeviceNotifyEvent(FHostedServer_OnDeviceStarting);
				FHostedServer.OnDeviceStarted -= new DeviceNotifyEvent(FHostedServer_OnDeviceStarted);
				FHostedServer.OnLibraryLoading -= new LibraryNotifyEvent(FHostedServer_OnLibraryLoading);
				FHostedServer.OnLibraryLoaded -= new LibraryNotifyEvent(FHostedServer_OnLibraryLoaded);
				FHostedServer.Dispose();
				FHostedServer = null;
			}
			
			if (FRemoteServer != null)
			{
				ServerFactory.Disconnect(FRemoteServer);
				FRemoteServer = null;
			}
		}

		private ServerAlias FServerAlias;
		/// <summary>The alias used to establish this connection.</summary>
		public ServerAlias Alias { get { return FServerAlias; } }
		
		private ServerHost FServerHost;
		private Server.Server FHostedServer;
		private IServer FRemoteServer;
		private IServer FExistingServer;

		/// <summary>The IServer interface for the server connection.</summary>
		public IServer Server { get { return (FExistingServer != null ? FExistingServer : (FHostedServer != null ? FHostedServer : FRemoteServer)); } }

		public override string ToString()
		{
			return FServerAlias.ToString();
		}
	}
}