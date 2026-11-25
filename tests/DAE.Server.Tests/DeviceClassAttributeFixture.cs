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
	/// What a fixture over what a create or alter statement built needs to see, and no test cases of its own: the running
	/// object as the catalog holds it, and the create statement the catalog emits for it. Named after the first of those
	/// fixtures. Some D4 objects are backed by a .NET class, and the attributes of their class definition are applied to the
	/// live object as properties; so a test over one has to read both the running object's property and the class definition
	/// the catalog holds, which it reads back as the emitted create statement.
	/// </summary>
	/// <remarks>
	/// The in-memory device's row limit is the setting the class-definition fixtures reach for, because nothing outside the
	/// engine is needed to set it; the SQL devices take class attributes of their own (server name, connection parameters)
	/// through the same code. SchemaCacheInvalidationTest and CreateStatementTagRerunTest derive from this too, for
	/// CatalogObject and Script alone: what they have to read after a repeated create statement is the same live object and the
	/// same emitted create statement, for devices and for the other catalog objects a statement builds. Moving those two
	/// helpers down to InProcessTestFixture would spare them the mismatched name, and is worth doing when a fourth fixture
	/// needs them.
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
			return CatalogObject<Schema.Device>(deviceName);
		}

		/// <summary>The object of that type with that unqualified name, as the running server's catalog holds it.</summary>
		protected T CatalogObject<T>(string name) where T : Schema.Object
		{
			foreach (Schema.Object objectValue in Instance.Server.Catalog)
				if ((objectValue is T) && (Schema.Object.Unqualify(objectValue.Name) == name))
					return (T)objectValue;
			Assert.Fail(String.Format("{0} {1} is not in the catalog.", typeof(T).Name, name));
			return null;
		}

		/// <summary>The create statement the catalog emits for the named object, which carries its class definition and its tags.</summary>
		protected string Script(string objectName)
		{
			return (string)Evaluate(String.Format("Script(Name(LibraryName() + '.{0}'))", objectName));
		}

		protected string OperatorScript(string operatorSpecifier)
		{
			return (string)Evaluate(String.Format("Script(LibraryName() + '.{0}')", operatorSpecifier));
		}

		// Private, so that a fixture evaluating on a session of its own can keep its own Evaluate.
		private object Evaluate(string expression)
		{
			return ((ServerProcess)Process).Evaluate(expression, null).AsNative;
		}
	}
}
