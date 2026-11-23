/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Device.Memory;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What the fixtures over class definitions need to see, and no assertions of its own. Some D4 objects are backed by a
	/// .NET class, and the attributes of their class definition are applied to the live object as properties; so a test over
	/// one has to read both the running object's property and the class definition the catalog holds, which it reads back as
	/// the create statement the catalog emits.
	/// </summary>
	/// <remarks>
	/// The in-memory device's row limit is the setting these fixtures reach for, because nothing outside the engine is needed
	/// to set it; the SQL devices take class attributes of their own (server name, connection parameters) through the same code.
	/// </remarks>
	public abstract class DeviceClassAttributeFixture : InProcessTestFixture
	{
		/// <summary>The row limit the running device enforces, read off the live object: nothing in the catalog reports it.</summary>
		protected int MaxRowCount(string deviceName)
		{
			return ((MemoryDevice)Device(deviceName)).MaxRowCount;
		}

		protected bool IgnoreUnsupported(string deviceName)
		{
			return Device(deviceName).IgnoreUnsupported;
		}

		protected Schema.Device Device(string deviceName)
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is Schema.Device) && (Schema.Object.Unqualify(objectValue.Name) == deviceName))
					return (Schema.Device)objectValue;
			Assert.Fail(String.Format("Device {0} is not in the catalog.", deviceName));
			return null;
		}

		/// <summary>The create statement the catalog emits for the device, which carries its class definition.</summary>
		protected string Script(string deviceName)
		{
			return (string)Evaluate(String.Format("Script(Name(LibraryName() + '.{0}'))", deviceName));
		}

		protected string OperatorScript(string operatorSpecifier)
		{
			return (string)Evaluate(String.Format("Script(LibraryName() + '.{0}')", operatorSpecifier));
		}

		protected object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}
	}
}
