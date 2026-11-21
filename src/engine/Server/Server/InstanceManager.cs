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
	/// The instance configuration is stored in the file ServerConfigurations.config in the folder
	/// <see cref="InstanceDirectory"/> names, by default
	///	<&lt;>common application data path<&gt;>\Alphora\Dataphor\<&lt;>major version<&gt;>\Instances.
	/// The folder carries the major version so that installations of different major versions do not read and write
	/// each other's instance list, which the client's own machine-wide alias list (AliasManager) has always done.
	/// </remarks>
	public class InstanceManager
	{
		// Do not localize
		public const string InstanceConfigurationFileName = "ServerConfigurations.config";
		public const string DefaultInstanceName = "Dataphor";

		/// <summary>The environment variable that redirects the instance directory for a process that sets it.</summary>
		public const string InstanceDirectoryVariableName = "DATAPHOR_INSTANCE_DIRECTORY";

		private static InstanceList _instances;
		public static InstanceList Instances 
		{ 
			get 
			{ 
				CheckLoaded();
				return _instances; 
			} 
		}
		
		private static string _instanceDirectory;
		/// <summary>
		/// The folder holding this machine's instance list (ServerConfigurations.config) and, by default, each instance's
		/// own data folder.
		/// </summary>
		/// <remarks>
		/// Not to be confused with ServerConfiguration.InstanceDirectory, which is one instance's own data folder and is
		/// defaulted to a sub-folder of this one. Resolved, in order: a value set here; the environment variable
		/// DATAPHOR_INSTANCE_DIRECTORY; %ProgramData%\Alphora\Dataphor\&lt;major version&gt;\Instances. Reading it does not
		/// create the folder; GetInstanceDirectory does. Setting it discards the loaded list, so Load must be called again.
		/// <para>
		/// The variable is read on each resolution rather than cached, so a process that changes it changes where the next
		/// resolution looks; the list already loaded is not reloaded by itself. Setting the property to null or empty
		/// returns to resolving the variable and then the default, which is how a test restores what it changed.
		/// </para>
		/// </remarks>
		public static string InstanceDirectory
		{
			get { return ResolveInstanceDirectory(); }
			set
			{
				_instanceDirectory = value;

				// The loaded list came from the folder that was in force when it was loaded; serving it from here after the
				// folder has changed would hand out instances that are not in the new folder's file. Discarding it makes the
				// next Instances access throw InstanceConfigurationNotLoaded rather than answer from the old folder, and
				// GetInstance reloads as it already does for a list that was never loaded.
				_instances = null;
			}
		}

		/// <summary>Resolves <see cref="InstanceDirectory"/> to an absolute path without creating it.</summary>
		private static string ResolveInstanceDirectory()
		{
			string result = _instanceDirectory;
			if (String.IsNullOrEmpty(result))
				result = Environment.GetEnvironmentVariable(InstanceDirectoryVariableName);
			if (String.IsNullOrEmpty(result))
				result = Path.Combine(PathUtility.CommonAppDataPath(String.Empty, VersionModifier.MajorSpecific, ensureExists: false), Server.DefaultInstanceDirectory);

			// A relative override is relative to the process's current directory. An unusable one is left to
			// Directory.CreateDirectory to report, which names the path in its message - the diagnosis an operator needs.
			return Path.GetFullPath(result);
		}

		/// <summary>Resolves <see cref="InstanceDirectory"/>, creates it if it does not exist, and returns it.</summary>
		public static string GetInstanceDirectory()
		{
			string result = ResolveInstanceDirectory();
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
