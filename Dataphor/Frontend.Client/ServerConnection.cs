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
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client
{
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

		public event NameChangeHandler OnNameChanging;

		// Name

		private string FName;
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
		[DefaultValue(ServerService.CDefaultPortNumber)]
		public int PortNumber
		{
			get { return FPortNumber; }
			set { FPortNumber = value; }
		}
	}

	public class ConnectionAlias : ServerAlias
	{
		public const string CDefaultHostName = "localhost";

		private string FHostName = CDefaultHostName;
		[DefaultValue(CDefaultHostName)]
		[Description("The host name or IP address of the server.")]
		public string HostName
		{
			get { return FHostName; }
			set { FHostName = value; }
		}
		
		private bool FClientSideLoggingEnabled;
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

	public class InProcessAlias : ServerAlias
	{
		private string FStartupScriptUri = String.Empty;
		[Description("The URI to a startup script to run while initializing the server.")]
		[DefaultValue("")]
		public string StartupScriptUri
		{
			get { return FStartupScriptUri; }
			set { FStartupScriptUri = value; }
		}

		private string FCatalogDirectory = String.Empty;
		[DefaultValue("")]
		[Description("The directory used by the Dataphor Server to persist it's catalog (schema).")]
		public string CatalogDirectory
		{
			get { return FCatalogDirectory; }
			set { FCatalogDirectory = value; }
		}

		private string FLibraryDirectory = String.Empty;
		[DefaultValue("")]
		[Description("The directory used for Dataphor Server libraries.")]
		public string LibraryDirectory
		{
			get { return FLibraryDirectory; }
			set { FLibraryDirectory = value; }
		}
		
		private bool FTracingEnabled = true;
		[DefaultValue(true)]
		[Description("Determines whether the Dataphor Server will use the Dataphor event log to write service events.")]
		public bool TracingEnabled
		{
			get { return FTracingEnabled; }
			set { FTracingEnabled = value; }
		}

		private bool FLogErrors = false;
		[DefaultValue(false)]
		[Description("Determines whether the Dataphor Server will use the Dataphor event log to write errors returned to clients.")]
		public bool LogErrors
		{
			get { return FLogErrors; }
			set { FLogErrors = value; }
		}
		
		private bool FIsEmbedded = false;
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

	// Alias configuration
	[PublishDefaultList("Aliases")]
	public class AliasConfiguration
	{
		// Do not localize
		public const string CAliasConfigurationFileName = "ServerAliases.config";

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

		public static string AliasConfigurationFileName
		{
			get { return PathUtility.UserAppDataPath() + '\\' + CAliasConfigurationFileName; }
		}

		public static AliasConfiguration Load()
		{
			return AliasConfiguration.Load(AliasConfigurationFileName);
		}

		/// <summary> Loads a new alias configuration. </summary>
		/// <remarks> Creates a default alias configuration if the file doesn't exist. </remarks>
		public static AliasConfiguration Load(string AFileName)
		{
			AliasConfiguration LConfiguration = new AliasConfiguration();
			if (File.Exists(AFileName))
				using (Stream LStream = File.OpenRead(AFileName))
				{
					new BOP.Deserializer().Deserialize(LStream, LConfiguration);
				}
			return LConfiguration;
		}

		public void Save(string AFileName)
		{
			using (Stream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				new BOP.Serializer().Serialize(LStream, this);
			}
		}

		public ServerConnection Connect()
		{
			if (DefaultAliasName == String.Empty)
				throw new ClientException(ClientException.Codes.NoServerAliasSpecified);
			return new ServerConnection(Aliases[DefaultAliasName]);
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
	}

	public class ServerConnection : Disposable
	{
		public ServerConnection(ServerAlias AServerAlias)
		{
			FServerAlias = AServerAlias;
			InProcessAlias LInProcess = FServerAlias as InProcessAlias;
			ConnectionAlias LConnectionAlias = FServerAlias as ConnectionAlias;
			try
			{
				FDataSession = new DataSession();
				if (LInProcess != null)
				{
					Server LServer = new Server();
					try
					{
						LServer.Name = Server.CDefaultServerName;
						LServer.LogErrors = LInProcess.LogErrors;
						LServer.TracingEnabled = LInProcess.TracingEnabled;
						LServer.StartupScriptUri = LInProcess.StartupScriptUri;
						LServer.CatalogDirectory = LInProcess.CatalogDirectory;
						LServer.LibraryDirectory = LInProcess.LibraryDirectory;
						LServer.IsEmbedded = LInProcess.IsEmbedded;
						LServer.Start();

						FServerHost = new ServerHost(LServer, LInProcess.PortNumber);
					}
					catch
					{
						LServer.Dispose();
						throw;
					}

					FDataSession.ConfiguredServer = LServer;
				}
				else
				{
					FServerHost = null;
					FDataSession.ServerUri = String.Format("tcp://{0}:{1}/Dataphor", LConnectionAlias.HostName, LConnectionAlias.PortNumber);
					FDataSession.ClientSideLoggingEnabled = LConnectionAlias.ClientSideLoggingEnabled;
				}
				FDataSession.SessionInfo = (SessionInfo)FServerAlias.SessionInfo.Clone();
				FDataSession.Open();
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
			try
			{
				if (FDataSession != null)
				{
					FDataSession.Dispose();
					FDataSession = null;
				}
			}
			finally
			{
				if (FServerHost != null)
				{
					Server LServer = FServerHost.Server;
					FServerHost.Dispose();
					FServerHost = null;
					LServer.Dispose();
				}
			}
		}

		private ServerAlias FServerAlias;
		public ServerAlias ServerAlias
		{
			get { return FServerAlias; }
		}

		private DataSession FDataSession;
		public DataSession DataSession
		{
			get { return FDataSession; }
		}

		private ServerHost FServerHost;
		public ServerHost ServerHost
		{
			get { return FServerHost; }
		}

		public Server Server
		{
			get { return (FServerHost == null ? null : FServerHost.Server); }
		}

		public override string ToString()
		{
			return FServerAlias.ToString();
		}
	}
}