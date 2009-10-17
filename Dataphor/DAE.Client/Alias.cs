/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Collections.Generic;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Contracts;

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
			#if !SILVERLIGHT
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
		
		private ConnectionSecurityMode FSecurityMode;
		/// <summary>
		/// Determines the security mode of the connection.
		/// </summary>
		/// <remarks>
		/// Default indicates that the connection will be made using the default security mode of the server.
		/// None indicates that the connection will be made with no security.
		/// Transport indicates that the connection will be established with transport security.
		/// </remarks>
		[DefaultValue(ConnectionSecurityMode.Default)]
		[Description("Determines the security mode of the connection.")]
		public ConnectionSecurityMode SecurityMode
		{
			get { return FSecurityMode; }
			set { FSecurityMode = value; }
		}
		
		private int FOverrideListenerPortNumber;
		/// <summary>
		/// Allows the port number for the listener to be explicitly specified.
		/// </summary>
		[DefaultValue(0)]
		[Description("If specified, a uri for the listener is constructed with this port number, rather than using the default listener port to connect.")]
		public int OverrideListenerPortNumber
		{
			get { return FOverrideListenerPortNumber; }
			set { FOverrideListenerPortNumber = value; }
		}
		
		private ConnectionSecurityMode FListenerSecurityMode;
		/// <summary>
		/// Determines the security mode of the listener connection.
		/// </summary>
		/// <remarks>
		/// Default or None indicates that the listener connection will be made with no security.
		/// Transport indicates that the listener connection will be made with transport security.
		/// </remarks>
		[DefaultValue(ConnectionSecurityMode.Default)]
		[Description("Determines the security mode of the listener connection.")]
		public ConnectionSecurityMode ListenerSecurityMode
		{
			get { return FListenerSecurityMode; }
			set { FListenerSecurityMode = value; }
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
}
