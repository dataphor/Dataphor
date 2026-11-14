/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;

using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary>
	/// Defines the configuration of a single instance of a Dataphor server.
	/// </summary>
	public class ServerConfiguration
	{
		public const int DefaultPortNumber = 8061;
		public const string DefaultLocalInstanceName = "LocalInstance";
		public const int DefaultLocalPortNumber = 8062;
		
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		
		private int _portNumber;
		public int PortNumber
		{
			get { return _portNumber; }
			set { _portNumber = value; }
		}
		
		private bool _requireSecureConnection;
		/// <summary>
		/// Indicates whether or not the server will require all remote connections to use a secure channel.
		/// </summary>
		[DefaultValue(false)]
		public bool RequireSecureConnection
		{
			get { return _requireSecureConnection; }
			set { _requireSecureConnection = value; }
		}
		
		private bool _shouldListen = true;
		[DefaultValue(true)]
		public bool ShouldListen
		{
			get { return _shouldListen; }
			set { _shouldListen = value; }
		}
		
		private int _overrideListenerPortNumber;
		[DefaultValue(0)]
		public int OverrideListenerPortNumber
		{
			get { return _overrideListenerPortNumber; }
			set { _overrideListenerPortNumber = value; }
		}
		
		private bool _requireSecureListenerConnection;
		[DefaultValue(false)]
		public bool RequireSecureListenerConnection
		{
			get { return _requireSecureListenerConnection; }
			set { _requireSecureListenerConnection = value; }
		}

		private bool _useServiceConfiguration;
		[DefaultValue(false)]
		public bool UseServiceConfiguration
		{
			get { return _useServiceConfiguration; }
			set { _useServiceConfiguration = value; }
		}
		
		private bool _allowSilverlightClients = true;
		[DefaultValue(true)]
		public bool AllowSilverlightClients
		{
			get { return _allowSilverlightClients; }
			set { _allowSilverlightClients = value; }
		}
		
		private string _libraryDirectories;
		public string LibraryDirectories
		{
			get { return _libraryDirectories; }
			set { _libraryDirectories = value; }
		}
		
		private string _instanceDirectory;
		public string InstanceDirectory
		{
			get { return _instanceDirectory; }
			set { _instanceDirectory = value; }
		}
		
		private string _catalogStoreClassName;
		public string CatalogStoreClassName
		{
			get { return _catalogStoreClassName; }
			set { _catalogStoreClassName = value; }
		}
		
		private string _catalogStoreConnectionString;
		public string CatalogStoreConnectionString
		{
			get { return _catalogStoreConnectionString; }
			set { _catalogStoreConnectionString = value; }
		}
		
		private Schema.DeviceSettings _deviceSettings = new Schema.DeviceSettings();
		public Schema.DeviceSettings DeviceSettings
		{
			get { return _deviceSettings; }
		}
		
		public void ApplyTo(Server server)
		{
			server.Name = _name;
			server.LibraryDirectory = _libraryDirectories;
			server.InstanceDirectory = _instanceDirectory;
			server.CatalogStoreClassName = _catalogStoreClassName;
			server.CatalogStoreConnectionString = _catalogStoreConnectionString;
			server.DeviceSettings.AddRange(_deviceSettings);
		}
		
		public static ServerConfiguration DefaultInstance(string instanceName)
		{
			ServerConfiguration instance = new ServerConfiguration();
			instance.Name = String.IsNullOrEmpty(instanceName) ? Engine.DefaultServerName : instanceName;
			instance.PortNumber = DefaultPortNumber;
			instance.LibraryDirectories = Path.Combine(PathUtility.GetInstallationDirectory(), Server.DefaultLibraryDirectory);
			return instance;
		}
		
		public static ServerConfiguration DefaultLocalInstance()
		{
			ServerConfiguration instance = new ServerConfiguration();
			instance.Name = DefaultLocalInstanceName;
			instance.PortNumber = DefaultLocalPortNumber; // don't use the same default port as the service
			instance.LibraryDirectories = Path.Combine(PathUtility.GetInstallationDirectory(), Server.DefaultLibraryDirectory);
			return instance;
		}
	}
}
