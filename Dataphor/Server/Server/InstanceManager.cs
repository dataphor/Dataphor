/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Windows;

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
		public const string InstanceConfigurationFileName = "ServerConfigurations.config";
		public const string DefaultInstanceName = "Dataphor";

		private static InstanceList _instances;
		public static InstanceList Instances 
		{ 
			get 
			{ 
				CheckLoaded();
				return _instances; 
			} 
		}
		
		public static string GetInstanceDirectory()
		{
			string result = Path.Combine(PathUtility.CommonAppDataPath(String.Empty, VersionModifier.None), Server.DefaultInstanceDirectory);
			Directory.CreateDirectory(result);
			return result;
		}
		
		private static string GetMachineInstanceConfigurationFileName()
		{
			return Path.Combine(GetInstanceDirectory(), InstanceConfigurationFileName);
		}
		
		private static InstanceConfiguration LoadMachineInstanceConfiguration()
		{
			return InstanceConfiguration.Load(GetMachineInstanceConfigurationFileName());
		}
		
		private static void SaveMachineInstanceConfiguration(InstanceConfiguration configuration)
		{
			configuration.Save(GetMachineInstanceConfigurationFileName());
		}
		
		public static void Load()
		{
			Reset();

			// Load Machine Instances
			InstanceConfiguration machineConfiguration = LoadMachineInstanceConfiguration();
			foreach (ServerConfiguration instance in machineConfiguration.Instances.Values)
				_instances.Add(instance);
		}
		
		private static void CheckLoaded()
		{
			if (_instances == null)
				throw new ServerException(ServerException.Codes.InstanceConfigurationNotLoaded);
		}
		
		public static void Save()
		{
			CheckLoaded();
			
			InstanceConfiguration machineConfiguration = new InstanceConfiguration();
			
			foreach (ServerConfiguration instance in _instances.Values)
				machineConfiguration.Instances.Add(instance);

			SaveMachineInstanceConfiguration(machineConfiguration);
		}

		private static void Reset()
		{
			_instances = new InstanceList();
		}
		
		public static ServerConfiguration GetInstance(string instanceName)
		{
			if (_instances == null)
				Load();
			return Instances.GetInstance(instanceName);
		}
		
		public static InstanceConfiguration LoadConfiguration()
		{
			InstanceConfiguration configuration = new InstanceConfiguration();
			Load();
			foreach (ServerConfiguration instance in _instances.Values)
				configuration.Instances.Add(instance);
			return configuration;
		}
		
		public static void SaveConfiguration(InstanceConfiguration configuration)
		{
			CheckLoaded();
			_instances.Clear();
			foreach (ServerConfiguration instance in configuration.Instances.Values)
				_instances.Add(instance);
			Save();
		}
	}
	
	// Instance configuration
	[PublishDefaultList("Instances")]
	public class InstanceConfiguration
	{
		private InstanceList _instances = new InstanceList();
		public InstanceList Instances
		{
			get { return _instances; }
		}
		
		/// <summary> Loads a new Instance configuration. </summary>
		/// <remarks> Creates a default Instance configuration if the file doesn't exist. </remarks>
		public static InstanceConfiguration Load(string fileName)
		{
			InstanceConfiguration configuration = new InstanceConfiguration();
			if (File.Exists(fileName))
				using (Stream stream = File.OpenRead(fileName))
				{
					new Deserializer().Deserialize(stream, configuration);
				}
			return configuration;
		}
		
		public void Save(string fileName)
		{
			using (Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				new Serializer().Serialize(stream, this);
			}
		}
	}

	/// <summary> List of server Instances. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class InstanceList : HashtableList<string, ServerConfiguration>
	{
		public InstanceList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object tempValue)
		{
			ServerConfiguration instance = (ServerConfiguration)tempValue;
			Add(instance.Name, instance);
			return IndexOf(instance.Name);
		}
		
		public new ServerConfiguration this[string instanceName]
		{
			get
			{
				ServerConfiguration instance = null;
				TryGetValue(instanceName, out instance);
				return instance;
			}
			set
			{
				base[instanceName] = value;
			}
		}

		public ServerConfiguration GetInstance(string instanceName)
		{
			ServerConfiguration instance;
			if (!TryGetValue(instanceName, out instance))
				throw new ServerException(ServerException.Codes.InstanceNotFound, instanceName);
			else
				return instance;
		}
		
		public bool HasInstance(string instanceName)
		{
			return ContainsKey(instanceName);
		}
	}
}
