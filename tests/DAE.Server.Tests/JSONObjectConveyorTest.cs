/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The physical representation of a System.Internet Dynamic (JSON) value, and a sweep asserting that every
	/// conveyor in the loaded assemblies implements the half of Conveyor its IsStreaming flag claims.
	/// </summary>
	/// <remarks>
	/// JSONObjectConveyor overrode only the streaming Read/Write pair while reporting IsStreaming as false, so every
	/// engine call site that branches on the flag took the byte-buffer branch and hit the base class's throwing stub;
	/// it also closed the stream the engine handed it, which the engine needs afterwards to size the row buffer. The
	/// first two tests below are that defect's reproduction, taken at the engine's own physical-representation path -
	/// the lowest layer that shows it, and the only one reachable in process (there is no device scalar type mapping
	/// for Dynamic, so such a column lives only in the memory device, which never asks for a physical representation).
	/// </remarks>
	// Not [Parallelizable]: this fixture starts a server, and two engines in one process share the engine's static
	// state (see the note on TestServerInstance).
	[TestFixture]
	public class JSONObjectConveyorTest : InProcessTestFixture
	{
		/// <summary>
		/// Frontend comes before System.Internet because System.Internet's registration script calls the
		/// Frontend.ExecuteScript operator without declaring the dependency (bug-system-internet-registration-needs-frontend),
		/// and SimpleDevice is in the set because Frontend's own registration fails without it.
		/// </summary>
		private static readonly string[] InternetLibraries =
		[
			"System.Platform",
			"Frontend",
			"System.Internet",
			"SimpleDevice",
		];

		protected override string[] Libraries => InternetLibraries;

		protected override string[] LibrariesToRegister => InternetLibraries;

		private const string DynamicExpression = @"Dynamic('{""a"":1}')";

		[Test]
		public void DynamicScalarRoundTripsThroughItsPhysicalRepresentation()
		{
			using (IDataValue written = ((ServerProcess)Process).Evaluate(DynamicExpression, null))
			{
				byte[] physical = written.AsPhysical;
				using (IDataValue read = DataValue.FromPhysical(((ServerProcess)Process).ValueManager, written.DataType, physical, 0))
					Assert.That(Describe(read.AsNative), Is.EqualTo(Describe(written.AsNative)), "A Dynamic value did not survive a round trip through its physical representation.");
			}
		}

		[Test]
		public void RowWithDynamicColumnRoundTripsThroughItsPhysicalRepresentation()
		{
			using (IDataValue written = ((ServerProcess)Process).Evaluate(@"row { " + DynamicExpression + " Payload, 7 Tag }", null))
			{
				byte[] physical = written.AsPhysical;
				using (IDataValue read = DataValue.FromPhysical(((ServerProcess)Process).ValueManager, written.DataType, physical, 0))
				{
					Assert.That(Describe(((IRow)read)["Payload"]), Is.EqualTo(Describe(((IRow)written)["Payload"])), "The Dynamic column of a row did not survive a round trip through its physical representation.");
					Assert.That(((IRow)read)["Tag"], Is.EqualTo(((IRow)written)["Tag"]), "The Integer column of a row did not survive a round trip through its physical representation.");
				}
			}
		}

		/// <summary>
		/// A Dynamic value's native representation is a Newtonsoft JToken, which this assembly does not reference;
		/// its text is what the conveyor writes and reads, so comparing the text compares the value.
		/// </summary>
		private static string Describe(object nativeValue)
		{
			return nativeValue == null ? "<nil>" : nativeValue.ToString();
		}

		/// <summary>
		/// Conveyor offers two ways to render a value - a byte-buffer trio and a stream pair - and a flag saying which
		/// one a given conveyor implements; every method a subclass does not override throws. A subclass whose
		/// overrides and flag disagree therefore throws from every call site that trusts the flag, which is the defect
		/// the tests above reproduce, and nothing but this sweep would say so.
		/// </summary>
		[Test]
		public void EveryConveyorImplementsTheHalfItsFlagClaims()
		{
			Type[] conveyorTypes =
				AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(GetLoadableTypes)
					.Where(type => typeof(Conveyor).IsAssignableFrom(type) && !type.IsAbstract && !type.ContainsGenericParameters)
					.Distinct()
					.OrderBy(type => type.FullName, StringComparer.Ordinal)
					.ToArray();

			// A sweep that saw only the engine assembly would pass while covering nothing new, so require the one
			// conveyor that lives in a library. Deliberately no assertion on the count: NUnit runs the whole project
			// in one process, so another fixture's libraries may be loaded too and finding more conveyors is the point.
			Assert.That(
				conveyorTypes.Select(type => type.Name),
				Contains.Item("JSONObjectConveyor"),
				"The sweep did not reach a library assembly, so it covers only the engine's own conveyors."
			);

			List<string> mismatches = new List<string>();
			foreach (Type conveyorType in conveyorTypes)
			{
				bool streamingDeclared = IsDeclared(conveyorType, "Read", typeof(Stream)) || IsDeclared(conveyorType, "Write", typeof(object), typeof(Stream));
				bool bufferDeclared =
					IsDeclared(conveyorType, "GetSize", typeof(object))
						|| IsDeclared(conveyorType, "Read", typeof(byte[]), typeof(int))
						|| IsDeclared(conveyorType, "Write", typeof(object), typeof(byte[]), typeof(int));

				if (streamingDeclared == bufferDeclared)
					continue; // Implements both halves, or neither: the flag cannot be wrong against what is there.

				Assert.That(
					conveyorType.GetConstructor(Type.EmptyTypes),
					Is.Not.Null,
					String.Format("{0} has no parameterless constructor; Conveyor's own remarks require one.", conveyorType.FullName)
				);

				bool isStreaming = ((Conveyor)Activator.CreateInstance(conveyorType)).IsStreaming;
				if (isStreaming != streamingDeclared)
					mismatches.Add(
						String.Format(
							"{0} reports IsStreaming as {1} but overrides only the {2} methods.",
							conveyorType.FullName,
							isStreaming,
							streamingDeclared ? "streaming Read/Write" : "byte-buffer GetSize/Read/Write"
						)
					);
			}

			Assert.That(mismatches, Is.Empty, "A conveyor's IsStreaming flag disagrees with the methods it overrides, so every engine call site that trusts the flag throws NotSupportedException.");
		}

		/// <summary>True when this type, or something between it and Conveyor, overrides the named method - as opposed to inheriting Conveyor's throwing stub.</summary>
		private static bool IsDeclared(Type conveyorType, string methodName, params Type[] parameterTypes)
		{
			MethodInfo method = conveyorType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
			return method != null && method.DeclaringType != typeof(Conveyor);
		}

		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				// A dynamic or partially loadable assembly must not fail the sweep; the types that did load still count.
				return exception.Types.Where(type => type != null);
			}
		}
	}
}
