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

		public Settings(string AFileName)
		{
			LoadSettings(AFileName);
		}

		private SettingsList FList = new SettingsList();
		[Publish(PublishMethod.List)]
		public SettingsList List { get { return FList; } }

		public void ClearSettings()
		{
			FList.Clear();
		}

		/// <summary> Accesses the raw string configuration items by name. </summary>
		public string this[string ASettingName]
		{
			get { return FList[ASettingName]; }
			set { FList[ASettingName] = value; }
		}

		/// <summary> Reads a setting value from the configuration settings. </summary>
		/// <param name="ASettingName"> Name of the value to find. </param>
		/// <param name="AType"> The type to convert the value to. </param>
		/// <param name="ADefault"> If the value is not found or it is empty, this default value is returned. </param>
		/// <returns> The setting, converted to the specified type. </returns>
		public object GetSetting(string ASettingName, Type AType, object ADefault)
		{
			string LValue = FList[ASettingName];
			if (LValue != String.Empty)
				return ReflectionUtility.StringToValue(LValue, AType);
			else
				return ADefault;
		}

		/// <summary> Writes a setting value into the configuration settings </summary>
		/// <param name="ASettingName"> Name of the value to find. </param>
		/// <param name="AValue"> The value to convert and write. </param>
		public void SetSetting(string ASettingName, object AValue)
		{
			FList[ASettingName] = ReflectionUtility.ValueToString(AValue, AValue.GetType());
		}

		/// <summary> Loads the configuration settings from file if the file exists. </summary>
		/// <remarks> Fatal errors will throw an exception.  Non-fatal errors will be reported in the ErrorList result. </remarks>
		/// <returns> An ErrorList collection of exceptions that occurred during loading.  Null if no errors. </returns>
		public ErrorList AttemptLoadSettings(string AFileName)
		{
			// Read the settings for the configuration file if the file exists
			if (File.Exists(AFileName))
				using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
				{
					// If the file doesn't have any data, don't deserialize it
					if (LStream.Length > 0)
					{
						Deserializer LDeserializer = new Deserializer();
						LDeserializer.Deserialize(LStream, this);
						if (LDeserializer.Errors.Count > 0)
							return LDeserializer.Errors;
					}
				}
			return null;
		}

		/// <summary> Loads the configuration settings from file if the file exists. </summary>
		/// <remarks> Throws on any error </remarks>
		public void LoadSettings(string AFileName)
		{
			ErrorList LErrors = AttemptLoadSettings(AFileName);
			if ((LErrors != null) && (LErrors.Count > 0))
				throw LErrors[0];	// Arbitrary... perhaps instead this could be a special exception class that had a list of exceptions
		}

		/// <summary> Saves the current configuration settings.</summary>
		public void SaveSettings(string AFileName)
		{
			// Persist the setting to the configuration file
			using (FileStream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				(new Serializer()).Serialize(LStream, this);
			}
		}
	}

	/// <summary> Maintains a name/value collection of strings. </summary>
	/// <remarks> Used by the <see cref="Settings">Settings</see> class. </remarks>
	public class SettingsList : HashtableList<string, SettingsItem>
	{
		public SettingsList() : base(StringComparer.OrdinalIgnoreCase) { }

		public new string this[string AKey]
		{
			get 
			{ 
				SettingsItem LItem;
				if (TryGetValue(AKey, out LItem))
					return LItem.Value;
				else
					return String.Empty;
			}
			set
			{
				SettingsItem LItem;
				if (!TryGetValue(AKey, out LItem))
				{
					LItem = new SettingsItem(AKey);
					base.Add(AKey, LItem);
				}
				LItem.Value = value;
			}
		}

		public override int Add(object AValue)
		{
			SettingsItem LItem = (SettingsItem)AValue;
			base.Add(LItem.Name, LItem);
			return IndexOf(LItem);
		}
	}

	/// <summary> Maintains a name/value string pair. </summary>
	/// <remarks> Used by the <see cref="SettingsList"/>. </remarks>
	[PublishDefaultConstructor("System.String")]
	public class SettingsItem
	{
		public SettingsItem([PublishSource("Name")] string AName)
		{
			FName = AName;
		}
		
		public SettingsItem(string AName, string AValue) : base()
		{
			FName = AName;
			FValue = AValue;
		}

		private string FName = String.Empty;
		[Publish(PublishMethod.Value)]
		public string Name
		{
			get { return FName; }
		}

		private string FValue = String.Empty;
		[Publish(PublishMethod.Value)]
		public string Value
		{
			get { return FValue; }
			set { FValue = value; }
		}
	}
}
