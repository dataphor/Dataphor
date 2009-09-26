/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.IO;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Server;
#if !SILVERLIGHT
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.Windows;
using Alphora.Dataphor.DAE.Service;
#endif

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

			#if !SILVERLIGHT
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
			#endif
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
		
		private string FInstanceName;
		/// <summary>
		/// The name of the instance. If this is not specified, the name of the alias is assumed to be the name of the instance.
		/// </summary>
		public string InstanceName
		{
			get { return FInstanceName; }
			set { FInstanceName = value; }
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
		
		private int FOverridePortNumber;
		/// <summary>
		/// Allows the port number for the connection to be explicitly specified.
		/// </summary>
		/// <remarks>
		/// If an override port number is specified, the connection will not attempt to use the listener for URI discovery, but
		/// will construct a URI based on this port number.
		/// </remarks>
		[DefaultValue(0)]
		[Description("If specified, a uri is constructed with this port number, rather than attempting to use listener services to connect.")]
		public int OverridePortNumber
		{
			get { return FOverridePortNumber; }
			set { FOverridePortNumber = value; }
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
			return String.Format("{0} ({1} on {2})", Name, InstanceName ?? Name, FHostName.ToString());
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
		private bool FIsEmbedded;
		public bool IsEmbedded
		{
			get { return FIsEmbedded; }
			set { FIsEmbedded = value; }
		}
		
		public override string ToString()
		{
			return String.Format("{0} ({1}) - {2}", Name, String.IsNullOrEmpty(InstanceName) ? Name : InstanceName, Strings.Get("CInProcess"));
		}
	}
	
	#if !SILVERLIGHT
	// Alias Manager
	/// <summary>Provides a class for managing the set of aliases available on this machine.</summary>
	/// <remarks>
	/// The Alias Manager provides a mechanism for obtaining the alias configuration for a process.
	/// The default behavior of the alias manager provides for machine- and user-specific aliases.
	/// If a user-specific alias has the same name as a machine-specific alias, the user-specific
	/// alias overrides the machine-specific alias.
	/// The default alias manager uses multiple alias configuration files:
	///	Machine specific: <&lt;>common application data path<&gt;>\Alphora\Dataphor\Aliases.config
	/// User specific: <&lt;>user application data path<&gt;>\Alphora\Dataphor\Aliases.config
	/// </remarks>
	public class AliasManager
	{
		// Do not localize
		public const string CAliasConfigurationFileName = "Aliases.config";

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
			return Path.Combine(PathUtility.CommonAppDataPath(String.Empty, VersionModifier.None), CAliasConfigurationFileName);
		}
		
		private static AliasConfiguration LoadMachineAliasConfiguration()
		{
			return AliasConfiguration.Load(GetMachineAliasConfigurationFileName());
		}
		
		private static string GetUserAliasConfigurationFileName()
		{
			return Path.Combine(PathUtility.UserAppDataPath(String.Empty, VersionModifier.None), CAliasConfigurationFileName);
		}
		
		private static AliasConfiguration LoadUserAliasConfiguration()
		{
			return AliasConfiguration.Load(GetUserAliasConfigurationFileName());
		}
		
		private static void SaveUserAliasConfiguration(AliasConfiguration AConfiguration)
		{
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
	#endif
	
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
				return "Alphora.Dataphor.DAE.Client,Alphora.Dataphor.DAE.Client";
			}
		}
		
		private class Deserializer : BOP.Deserializer
		{
			protected override Type GetClassType(string AName, string ANamespace)
			{
				if (String.Equals(AName, "AliasConfiguration", StringComparison.OrdinalIgnoreCase))
					return typeof(AliasConfiguration);
				if (String.Equals(AName, "ConnectionAlias", StringComparison.OrdinalIgnoreCase))
					return typeof(ConnectionAlias);
				if (String.Equals(AName, "InProcessAlias", StringComparison.OrdinalIgnoreCase))
					return typeof(InProcessAlias);
				return base.GetClassType(AName, ANamespace);
			}
		}
	}

	/// <summary> List of server aliases. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class AliasList : HashtableList<string, ServerAlias>
	{
		public AliasList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object AValue)
		{
			ServerAlias LAlias = (ServerAlias)AValue;
			Add(LAlias.Name, LAlias);
			return IndexOf(LAlias.Name);
		}

		public ServerAlias GetAlias(string AAliasName)
		{
			ServerAlias LAlias;
			if (!TryGetValue(AAliasName, out LAlias))
				throw new ClientException(ClientException.Codes.AliasNotFound, AAliasName);
			else
				return LAlias;
		}
		
		public bool HasAlias(string AAliasName)
		{
			return ContainsKey(AAliasName);
		}
	}

	/// <summary>Manages the connection to a server based on a given server alias definition.</summary>
	/// <remarks>The ServerConnection class manages the differences between in-process and out-of-process
	/// aliases, allowing clients to connect using only an alias name and a server connection. This
	/// class is used by the DataSession to establish a connection to a server based on an alias definition.
	/// </remarks>
	public class ServerConnection : Disposable
	{
		public ServerConnection(ServerAlias AServerAlias) : this(AServerAlias, true) {}
		
		public ServerConnection(ServerAlias AServerAlias, bool AAutoStart)
		{
			if (AServerAlias == null)
				throw new ClientException(ClientException.Codes.ServerAliasRequired);
				
			FServerAlias = AServerAlias;
			try
			{
				#if !SILVERLIGHT
				InProcessAlias LInProcessAlias = FServerAlias as InProcessAlias;
				if (LInProcessAlias != null)
				{
					if (LInProcessAlias.IsEmbedded)
					{
						ServerConfiguration LConfiguration = InstanceManager.GetInstance(LInProcessAlias.InstanceName);
						FHostedServer = new Server.Server();
						LConfiguration.ApplyTo(FHostedServer);
						if (AAutoStart)
							FHostedServer.Start();
					}
					else
					{
						FServiceHost = new DataphorServiceHost();
						FServiceHost.InstanceName = LInProcessAlias.InstanceName;
						if (AAutoStart)
							FServiceHost.Start();
					}
				}
				else
				{
				#endif
					ConnectionAlias LConnectionAlias = FServerAlias as ConnectionAlias;
					FClientServer = new ClientServer(LConnectionAlias.HostName, LConnectionAlias.OverridePortNumber, LConnectionAlias.InstanceName);
					FLocalServer = new LocalServer(FClientServer, LConnectionAlias.ClientSideLoggingEnabled, TerminalServiceUtility.ClientName);
				#if !SILVERLIGHT
				}
				#endif
			}
			catch
			{
				CleanUp();
				throw;
			}
		}

		protected override void Dispose(bool ADisposed)
		{
			base.Dispose(ADisposed);
			CleanUp();
		}

		private void CleanUp()
		{
			#if !SILVERLIGHT
			if (FHostedServer != null)
			{
				FHostedServer.Stop();
				FHostedServer = null;
			}
			
			if (FServiceHost != null)
			{
				FServiceHost.Stop();
				FServiceHost = null;
			}
			#endif
			if (FLocalServer != null)
			{
				FLocalServer.Dispose();
				FLocalServer = null;
			}
			
			if (FClientServer != null)
			{
				FClientServer.Close();
				FClientServer = null;
			}
		}

		private ServerAlias FServerAlias;
		/// <summary>The alias used to establish this connection.</summary>
		public ServerAlias Alias { get { return FServerAlias; } }
	
		private ClientServer FClientServer; // Used for out-of-process server
		private LocalServer FLocalServer; // Used for out-of-process server

		#if !SILVERLIGHT	
		private DataphorServiceHost FServiceHost; // Used for non-embedded in-process server
		private Server.Server FHostedServer; // Used for embedded in-process server

		/// <summary>The IServer interface for the server connection.</summary>
		public IServer Server { get { return FHostedServer != null ? FHostedServer : (FServiceHost != null ? FServiceHost.Server : FLocalServer); } }
		#else
		/// <summary>The IServer interface for the server connection.</summary>
		public IServer Server { get { return FLocalServer; } }
		#endif

		public override string ToString()
		{
			return FServerAlias.ToString();
		}
	}
}