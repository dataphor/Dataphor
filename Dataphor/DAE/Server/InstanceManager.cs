/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Alphora.Dataphor.BOP;
using System.ComponentModel;

namespace Alphora.Dataphor.DAE.Server
{
	// Instance Manager
	/// <summary>Provides a class for managing the set of instances available on this machine.</summary>
	/// <remarks>
	/// The InstanceManager provides a mechanism for managing the instance configuration for a machine.
	/// The instance configuration is stored in the following configuration file:
	///	<&lt;>common application data path<&gt;>\Alphora\Dataphor\Instances\ServerConfigurations.config
	/// </remarks>
	public class InstanceManager
	{
		// Do not localize
		public const string CInstanceConfigurationFileName = "ServerConfigurations.config";
		public const string CDefaultInstanceName = "Dataphor";

		private static InstanceList FInstances;
		public static InstanceList Instances 
		{ 
			get 
			{ 
				CheckLoaded();
				return FInstances; 
			} 
		}
		
		private static string GetInstanceDirectory()
		{
			string LResult = Path.Combine(PathUtility.CommonAppDataPath(String.Empty, VersionModifier.None), Server.CDefaultInstanceDirectory);
			Directory.CreateDirectory(LResult);
			return LResult;
		}
		
		private static string GetMachineInstanceConfigurationFileName()
		{
			return Path.Combine(GetInstanceDirectory(), CInstanceConfigurationFileName);
		}
		
		private static InstanceConfiguration LoadMachineInstanceConfiguration()
		{
			return InstanceConfiguration.Load(GetMachineInstanceConfigurationFileName());
		}
		
		private static void SaveMachineInstanceConfiguration(InstanceConfiguration AConfiguration)
		{
			AConfiguration.Save(GetMachineInstanceConfigurationFileName());
		}
		
		public static void Load()
		{
			Reset();

			// Load Machine Instances
			InstanceConfiguration LMachineConfiguration = LoadMachineInstanceConfiguration();
			foreach (ServerConfiguration LInstance in LMachineConfiguration.Instances.Values)
				FInstances.Add(LInstance);
		}
		
		private static void CheckLoaded()
		{
			if (FInstances == null)
				throw new ServerException(ServerException.Codes.InstanceConfigurationNotLoaded);
		}
		
		public static void Save()
		{
			CheckLoaded();
			
			InstanceConfiguration LMachineConfiguration = new InstanceConfiguration();
			
			foreach (ServerConfiguration LInstance in FInstances.Values)
				LMachineConfiguration.Instances.Add(LInstance);

			SaveMachineInstanceConfiguration(LMachineConfiguration);
		}

		private static void Reset()
		{
			FInstances = new InstanceList();
		}
		
		public static ServerConfiguration GetInstance(string AInstanceName)
		{
			if (FInstances == null)
				Load();
			return Instances.GetInstance(AInstanceName);
		}
		
		public static InstanceConfiguration LoadConfiguration()
		{
			InstanceConfiguration LConfiguration = new InstanceConfiguration();
			Load();
			foreach (ServerConfiguration LInstance in FInstances.Values)
				LConfiguration.Instances.Add(LInstance);
			return LConfiguration;
		}
		
		public static void SaveConfiguration(InstanceConfiguration AConfiguration)
		{
			CheckLoaded();
			FInstances.Clear();
			foreach (ServerConfiguration LInstance in AConfiguration.Instances.Values)
				FInstances.Add(LInstance);
			Save();
		}
	}
	
	// Instance configuration
	[PublishDefaultList("Instances")]
	public class InstanceConfiguration
	{
		private InstanceList FInstances = new InstanceList();
		public InstanceList Instances
		{
			get { return FInstances; }
		}
		
		/// <summary> Loads a new Instance configuration. </summary>
		/// <remarks> Creates a default Instance configuration if the file doesn't exist. </remarks>
		public static InstanceConfiguration Load(string AFileName)
		{
			InstanceConfiguration LConfiguration = new InstanceConfiguration();
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
	}

	/// <summary> List of server Instances. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class InstanceList : HashtableList
	{
		public InstanceList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object AValue)
		{
			ServerConfiguration LInstance = (ServerConfiguration)AValue;
			Add(LInstance.Name, LInstance);
			return IndexOf(LInstance.Name);
		}

		public new ServerConfiguration this[int AIndex]
		{
			get { return (ServerConfiguration)base[AIndex]; }
			set { base[AIndex] = value; }
		}

		public ServerConfiguration this[string AInstanceName]
		{
			get { return (ServerConfiguration)base[AInstanceName]; }
			set { base[AInstanceName] = value; }
		}
		
		public ServerConfiguration GetInstance(string AInstanceName)
		{
			ServerConfiguration LInstance = this[AInstanceName];
			if (LInstance == null)
				throw new ServerException(ServerException.Codes.InstanceNotFound, AInstanceName);
			return LInstance;
		}
		
		public bool HasInstance(string AInstanceName)
		{
			return this[AInstanceName] != null;
		}
	}
}
