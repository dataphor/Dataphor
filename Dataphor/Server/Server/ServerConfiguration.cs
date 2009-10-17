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
		public const int CDefaultPortNumber = 8061;
		public const int CDefaultSecurePortNumber = 8601;
		public const string CDefaultLocalInstanceName = "LocalInstance";
		public const int CDefaultLocalPortNumber = 8062;
		public const int CDefaultLocalSecurePortNumber = 8602;
		
		private string FName;
		public string Name
		{
			get { return FName; }
			set { FName = value; }
		}
		
		private int FPortNumber;
		public int PortNumber
		{
			get { return FPortNumber; }
			set { FPortNumber = value; }
		}
		
		private int FSecurePortNumber;
		public int SecurePortNumber
		{
			get { return FSecurePortNumber; }
			set { FSecurePortNumber = value; }
		}
		
		private bool FRequireSecureConnection;
		/// <summary>
		/// Indicates whether or not the server will require all remote connections to use a secure channel.
		/// </summary>
		[DefaultValue(false)]
		public bool RequireSecureConnection
		{
			get { return FRequireSecureConnection; }
			set { FRequireSecureConnection = value; }
		}
		
		private bool FShouldListen = true;
		[DefaultValue(true)]
		public bool ShouldListen
		{
			get { return FShouldListen; }
			set { FShouldListen = value; }
		}
		
		private int FOverrideListenerPortNumber;
		[DefaultValue(0)]
		public int OverrideListenerPortNumber
		{
			get { return FOverrideListenerPortNumber; }
			set { FOverrideListenerPortNumber = value; }
		}
		
		private int FOverrideSecureListenerPortNumber;
		[DefaultValue(0)]
		public int OverrideSecureListenerPortNumber
		{
			get { return FOverrideSecureListenerPortNumber; }
			set { FOverrideSecureListenerPortNumber = value; }
		}
		
		private bool FRequireSecureListenerConnection;
		[DefaultValue(false)]
		public bool RequireSecureListenerConnection
		{
			get { return FRequireSecureListenerConnection; }
			set { FRequireSecureListenerConnection = value; }
		}
		
		private bool FAllowSilverlightClients = true;
		[DefaultValue(true)]
		public bool AllowSilverlightClients
		{
			get { return FAllowSilverlightClients; }
			set { FAllowSilverlightClients = value; }
		}
		
		private string FLibraryDirectories;
		public string LibraryDirectories
		{
			get { return FLibraryDirectories; }
			set { FLibraryDirectories = value; }
		}
		
		private string FInstanceDirectory;
		public string InstanceDirectory
		{
			get { return FInstanceDirectory; }
			set { FInstanceDirectory = value; }
		}
		
		private string FCatalogStoreClassName;
		public string CatalogStoreClassName
		{
			get { return FCatalogStoreClassName; }
			set { FCatalogStoreClassName = value; }
		}
		
		private string FCatalogStoreConnectionString;
		public string CatalogStoreConnectionString
		{
			get { return FCatalogStoreConnectionString; }
			set { FCatalogStoreConnectionString = value; }
		}
		
		private Schema.DeviceSettings FDeviceSettings = new Schema.DeviceSettings();
		public Schema.DeviceSettings DeviceSettings
		{
			get { return FDeviceSettings; }
		}
		
		public void ApplyTo(Server AServer)
		{
			AServer.Name = FName;
			AServer.LibraryDirectory = FLibraryDirectories;
			AServer.InstanceDirectory = FInstanceDirectory;
			AServer.CatalogStoreClassName = FCatalogStoreClassName;
			AServer.CatalogStoreConnectionString = FCatalogStoreConnectionString;
			AServer.DeviceSettings.AddRange(FDeviceSettings);
		}
		
		public static ServerConfiguration DefaultInstance(string AInstanceName)
		{
			ServerConfiguration LInstance = new ServerConfiguration();
			LInstance.Name = String.IsNullOrEmpty(AInstanceName) ? Engine.CDefaultServerName : AInstanceName;
			LInstance.PortNumber = CDefaultPortNumber;
			LInstance.SecurePortNumber = CDefaultSecurePortNumber;
			LInstance.LibraryDirectories = Path.Combine(PathUtility.GetInstallationDirectory(), Server.CDefaultLibraryDirectory);
			return LInstance;
		}
		
		public static ServerConfiguration DefaultLocalInstance()
		{
			ServerConfiguration LInstance = new ServerConfiguration();
			LInstance.Name = CDefaultLocalInstanceName;
			LInstance.PortNumber = CDefaultLocalPortNumber; // don't use the same default port as the service
			LInstance.SecurePortNumber = CDefaultLocalSecurePortNumber;
			LInstance.LibraryDirectories = Path.Combine(PathUtility.GetInstallationDirectory(), Server.CDefaultLibraryDirectory);
			return LInstance;
		}
	}
}
