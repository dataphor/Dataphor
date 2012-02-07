/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Client
{
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
		public const string AliasConfigurationFileName = "Aliases.config";

		private static string _defaultAliasName;		
		public static string DefaultAliasName
		{
			get 
			{ 
				CheckLoaded();
				return _defaultAliasName; 
			}
			set 
			{ 
				CheckLoaded();
				_defaultAliasName = value; 
			}
		}

		private static AliasList _aliases;
		public static AliasList Aliases 
		{ 
			get 
			{ 
				CheckLoaded();
				return _aliases; 
			} 
		}
		
		private static AliasList _overriddenAliases;
		
		private static DateTime _machineConfigurationFileDate;
		
		private static string GetMachineAliasConfigurationFileName()
		{
			return Path.Combine(PathUtility.CommonAppDataPath(String.Empty, VersionModifier.MajorSpecific), AliasConfigurationFileName);
		}
		
		private static AliasConfiguration LoadMachineAliasConfiguration()
		{
			var fileName = GetMachineAliasConfigurationFileName();
			if (File.Exists(fileName))
				_machineConfigurationFileDate = File.GetLastWriteTimeUtc(fileName);
			else
				_machineConfigurationFileDate = default(DateTime);
			return AliasConfiguration.Load(fileName);
		}
		
		private static string GetUserAliasConfigurationFileName()
		{
			return Path.Combine(PathUtility.UserAppDataPath(String.Empty, VersionModifier.MajorSpecific), AliasConfigurationFileName);
		}
		
		private static AliasConfiguration LoadUserAliasConfiguration()
		{
			return AliasConfiguration.Load(GetUserAliasConfigurationFileName());
		}
		
		private static void SaveUserAliasConfiguration(AliasConfiguration configuration)
		{
			configuration.Save(GetUserAliasConfigurationFileName());
		}
		
		private static void SaveMachineAliasConfiguration(AliasConfiguration configuration)
		{
			var fileName = GetMachineAliasConfigurationFileName();
			if 
			(
				_machineConfigurationFileDate == default(DateTime) 
					|| !File.Exists(fileName)
					|| (File.GetLastWriteTimeUtc(fileName) <= _machineConfigurationFileDate)
			)
				try
				{
					configuration.Save(fileName);
				}
				catch (UnauthorizedAccessException)
				{
					// Don't throw if machine level saving fails due to permission error
				}
		}
		
		public static void Load()
		{
			Reset();

			// Load Machine Aliases
			AliasConfiguration machineConfiguration = LoadMachineAliasConfiguration();
			foreach (ServerAlias alias in machineConfiguration.Aliases.Values)
				_aliases.Add(alias);
			
			// Load User Aliases
			AliasConfiguration userConfiguration = LoadUserAliasConfiguration();
			foreach (ServerAlias alias in userConfiguration.Aliases.Values)
			{
				ServerAlias machineAlias = _aliases[alias.Name];
				if (machineAlias != null)
				{
					_overriddenAliases.Add(machineAlias);
					_aliases.Remove(alias.Name);
				}
				
				_aliases.Add(alias);
			}

			_defaultAliasName = userConfiguration.DefaultAliasName;
		}
		
		private static void CheckLoaded()
		{
			if (_aliases == null)
				throw new ClientException(ClientException.Codes.AliasConfigurationNotLoaded);
		}
		
		public static void Save()
		{
			CheckLoaded();
			
			AliasConfiguration userConfiguration = new AliasConfiguration();
			AliasConfiguration machineConfiguration = new AliasConfiguration();
			
			userConfiguration.DefaultAliasName = _defaultAliasName;
			foreach (ServerAlias alias in _aliases.Values)
				if (alias.IsUserAlias)
					userConfiguration.Aliases.Add(alias);
				else
					machineConfiguration.Aliases.Add(alias);
			
			foreach (ServerAlias alias in _overriddenAliases.Values)
				if (!machineConfiguration.Aliases.ContainsKey(alias.Name))
					machineConfiguration.Aliases.Add(alias);

			SaveUserAliasConfiguration(userConfiguration);
			SaveMachineAliasConfiguration(machineConfiguration);
		}

		private static void Reset()
		{
			_defaultAliasName = String.Empty;
			_aliases = new AliasList();
			_machineConfigurationFileDate = default(DateTime);
			_overriddenAliases = new AliasList();
		}
		
		public static ServerAlias GetAlias(string aliasName)
		{
			if (_aliases == null)
				Load();
			return Aliases.GetAlias(aliasName);
		}
		
		public static AliasConfiguration LoadConfiguration()
		{
			AliasConfiguration configuration = new AliasConfiguration();
			Load();
			configuration.DefaultAliasName = _defaultAliasName;
			foreach (ServerAlias alias in _aliases.Values)
				configuration.Aliases.Add(alias);
			return configuration;
		}
		
		public static void SaveConfiguration(AliasConfiguration configuration)
		{
			CheckLoaded();
			_aliases.Clear();
			foreach (ServerAlias alias in configuration.Aliases.Values)
				_aliases.Add(alias);
			_defaultAliasName = configuration.DefaultAliasName;
			Save();
		}
	}
	
	// Alias configuration
	[PublishDefaultList("Aliases")]
	public class AliasConfiguration
	{
		private string _defaultAliasName = String.Empty;
		[DefaultValue("")]
		public string DefaultAliasName
		{
			get { return _defaultAliasName; }
			set { _defaultAliasName = value; }
		}

		private AliasList _aliases = new AliasList();
		public AliasList Aliases
		{
			get { return _aliases; }
		}
		
		/// <summary> Loads a new alias configuration. </summary>
		/// <remarks> Creates a default alias configuration if the file doesn't exist. </remarks>
		public static AliasConfiguration Load(string fileName)
		{
			AliasConfiguration configuration = new AliasConfiguration();
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

		// NOTE: These custom serializers guarantee backwards compatibility of the Aliases config file with previous builds of the 2.1 branch.
		// NOTE: Because the Aliases are now in the DAE.Client namespace, i/o the Frontend.Client namespace, the new deserializer will fail to load the
		// NOTE: types of a config file written from an old serializer. These overrides force the new config file to be written the same way that the old
		// NOTE: deserializer will write it, allowing both versions to work together.
		private class Serializer : BOP.Serializer
		{
			protected override string GetElementNamespace(Type type)
			{
				return "Alphora.Dataphor.DAE.Client,Alphora.Dataphor.DAE.Client";
			}
		}
		
		private class Deserializer : BOP.Deserializer
		{
			protected override Type GetClassType(string name, string namespaceValue)
			{
				if (String.Compare(name, "AliasConfiguration", true) == 0)
					return typeof(AliasConfiguration);
				if (String.Compare(name, "ConnectionAlias", true) == 0)
					return typeof(ConnectionAlias);
				if (String.Compare(name, "InProcessAlias", true) == 0)
					return typeof(InProcessAlias);
				return base.GetClassType(name, namespaceValue);
			}
		}
	}

	/// <summary> List of server aliases. </summary>
	/// <remarks> Names must be case insensitively unique. </remarks>
	public class AliasList : HashtableList<string, ServerAlias>
	{
		public AliasList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object value)
		{
			ServerAlias alias = (ServerAlias)value;
			Add(alias.Name, alias);
			return IndexOf(alias.Name);
		}
		
		public new ServerAlias this[string aliasName]
		{
			get
			{
				ServerAlias alias = null;
				TryGetValue(aliasName, out alias);
				return alias;
			}
			set
			{
				base[aliasName] = value;
			}
		}

		public ServerAlias GetAlias(string aliasName)
		{
			ServerAlias alias;
			if (!TryGetValue(aliasName, out alias))
				throw new ClientException(ClientException.Codes.AliasNotFound, aliasName);
			else
				return alias;
		}
		
		public bool HasAlias(string aliasName)
		{
			return ContainsKey(aliasName);
		}
	}
}
