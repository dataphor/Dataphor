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
	public class AliasList : HashtableList<string, ServerAlias>
	{
		public AliasList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object AValue)
		{
			ServerAlias LAlias = (ServerAlias)AValue;
			Add(LAlias.Name, LAlias);
			return IndexOf(LAlias.Name);
		}
		
		public new ServerAlias this[string AAliasName]
		{
			get
			{
				ServerAlias LAlias = null;
				TryGetValue(AAliasName, out LAlias);
				return LAlias;
			}
			set
			{
				base[AAliasName] = value;
			}
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
}
