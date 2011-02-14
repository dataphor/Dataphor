/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Managages a collection of Name/Value configuration setting. </summary>
	[PublishDefaultList("List")]
	[PublishAs("SettingsContainer")]
	public class Settings
	{
		public Settings() {}

		public Settings(string fileName)
		{
			LoadSettings(fileName);
		}

		private SettingsList _list = new SettingsList();
		[Publish(PublishMethod.List)]
		public SettingsList List { get { return _list; } }

		public void ClearSettings()
		{
			_list.Clear();
		}

		/// <summary> Accesses the raw string configuration items by name. </summary>
		public string this[string settingName]
		{
			get { return _list[settingName]; }
			set { _list[settingName] = value; }
		}

		/// <summary> Reads a setting value from the configuration settings. </summary>
		/// <param name="settingName"> Name of the value to find. </param>
		/// <param name="type"> The type to convert the value to. </param>
		/// <param name="defaultValue"> If the value is not found or it is empty, this default value is returned. </param>
		/// <returns> The setting, converted to the specified type. </returns>
		public object GetSetting(string settingName, Type type, object defaultValue)
		{
			string tempValue = _list[settingName];
			if (tempValue != String.Empty)
				return ReflectionUtility.StringToValue(tempValue, type);
			else
				return defaultValue;
		}

		/// <summary> Writes a setting value into the configuration settings </summary>
		/// <param name="settingName"> Name of the value to find. </param>
		/// <param name="tempValue"> The value to convert and write. </param>
		public void SetSetting(string settingName, object tempValue)
		{
			_list[settingName] = ReflectionUtility.ValueToString(tempValue, tempValue.GetType());
		}

		/// <summary> Loads the configuration settings from file if the file exists. </summary>
		/// <remarks> Fatal errors will throw an exception.  Non-fatal errors will be reported in the ErrorList result. </remarks>
		/// <returns> An ErrorList collection of exceptions that occurred during loading.  Null if no errors. </returns>
		public ErrorList AttemptLoadSettings(string fileName)
		{
			// Read the settings for the configuration file if the file exists
			if (File.Exists(fileName))
				using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					// If the file doesn't have any data, don't deserialize it
					if (stream.Length > 0)
					{
						Deserializer deserializer = new Deserializer();
						deserializer.Deserialize(stream, this);
						if (deserializer.Errors.Count > 0)
							return deserializer.Errors;
					}
				}
			return null;
		}

		/// <summary> Loads the configuration settings from file if the file exists. </summary>
		/// <remarks> Throws on any error </remarks>
		public void LoadSettings(string fileName)
		{
			ErrorList errors = AttemptLoadSettings(fileName);
			if ((errors != null) && (errors.Count > 0))
				throw errors[0];	// Arbitrary... perhaps instead this could be a special exception class that had a list of exceptions
		}

		/// <summary> Saves the current configuration settings.</summary>
		public void SaveSettings(string fileName)
		{
			// Persist the setting to the configuration file
			using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				(new Serializer()).Serialize(stream, this);
			}
		}
	}

	/// <summary> Maintains a name/value collection of strings. </summary>
	/// <remarks> Used by the <see cref="Settings">Settings</see> class. </remarks>
	public class SettingsList : HashtableList<string, SettingsItem>
	{
		public SettingsList() : base(StringComparer.OrdinalIgnoreCase) { }

		public new string this[string key]
		{
			get 
			{ 
				SettingsItem item;
				if (TryGetValue(key, out item))
					return item.Value;
				else
					return String.Empty;
			}
			set
			{
				SettingsItem item;
				if (!TryGetValue(key, out item))
				{
					item = new SettingsItem(key);
					base.Add(key, item);
				}
				item.Value = value;
			}
		}

		public override int Add(object tempValue)
		{
			SettingsItem item = (SettingsItem)tempValue;
			base.Add(item.Name, item);
			return IndexOf(item);
		}
	}

	/// <summary> Maintains a name/value string pair. </summary>
	/// <remarks> Used by the <see cref="SettingsList"/>. </remarks>
	[PublishDefaultConstructor("System.String")]
	public class SettingsItem
	{
		public SettingsItem([PublishSource("Name")] string name)
		{
			_name = name;
		}
		
		public SettingsItem(string name, string tempValue) : base()
		{
			_name = name;
			_value = tempValue;
		}

		private string _name = String.Empty;
		[Publish(PublishMethod.Value)]
		public string Name
		{
			get { return _name; }
		}

		private string _value = String.Empty;
		[Publish(PublishMethod.Value)]
		public string Value
		{
			get { return _value; }
			set { _value = value; }
		}
	}
}
