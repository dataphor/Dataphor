/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Streams;

using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// What each conveyor does: the round trip from a native value to the physical representation the engine stores
	/// and back again, for every conveyor the engine ships, at the boundary values of each one's type.
	/// </summary>
	/// <remarks>
	/// These assertions are 3.1's. It shipped them as the D4 diagnostic operator TestConveyors(), whose C# is still
	/// in the tree at tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs - inside a block comment,
	/// written against a pre-3.0 engine API (ScalarManager, which the engine no longer has), and so not compiled for
	/// years. There is no reference runtime for this engine, so what 3.1 asserted is the record of what correct looks
	/// like; the values below are the ones it used, including the guid verbatim.
	///
	/// They are a C# fixture rather than a D4 operator because a conveyor needs no engine: each one is a plain class
	/// in Alphora.Dataphor.DAE.Streams with a parameterless constructor, which is how the engine's class loader
	/// builds it as well. Driving them directly is the lowest layer that shows the behaviour.
	///
	/// Conveyor offers two sets of methods - a byte-buffer trio and a stream pair - and IsStreaming says which set a
	/// given conveyor implements, so the two cases below split along that line. That a conveyor's flag agrees with
	/// the methods it actually overrides is asserted elsewhere, by
	/// JSONObjectConveyorTest.EveryConveyorImplementsTheHalfItsFlagClaims; this fixture covers what the conveyors do.
	///
	/// Two conveyors are deliberately not here. JSONObjectConveyor has its own fixture. VersionNumberConveyor lives
	/// in Runtime.Data rather than Streams, is not part of the Streams coverage area, and 3.1's test did not reach it
	/// either - it is covered indirectly wherever a library version round trips.
	/// </remarks>
	[TestFixture]
	public class ConveyorTest
	{
		/// <summary>The string 3.1 used wherever it needed a non-empty one.</summary>
		private const string GoodLongString = "This is a good long string";

		/// <summary>
		/// 3.1's guid, kept verbatim: a guid's physical representation is the one place a byte order is easy to get
		/// wrong, and a value whose bytes are all distinct is what makes a reordering visible.
		/// </summary>
		private static readonly Guid SampleGuid = new Guid("{BA69917B-27ED-41fb-BA46-7DF95C146280}");

		private static IEnumerable<TestCaseData> ByteBufferConveyorCases()
		{
			yield return Case(new BooleanConveyor(), false, "false");
			yield return Case(new BooleanConveyor(), true, "true");
			yield return Case(new ByteConveyor(), Byte.MinValue, "MinValue");
			yield return Case(new ByteConveyor(), Byte.MaxValue, "MaxValue");
			yield return Case(new Int16Conveyor(), Int16.MinValue, "MinValue");
			yield return Case(new Int16Conveyor(), Int16.MaxValue, "MaxValue");
			yield return Case(new Int32Conveyor(), Int32.MinValue, "MinValue");
			yield return Case(new Int32Conveyor(), Int32.MaxValue, "MaxValue");
			yield return Case(new Int64Conveyor(), Int64.MinValue, "MinValue");
			yield return Case(new Int64Conveyor(), Int64.MaxValue, "MaxValue");
			yield return Case(new DecimalConveyor(), Decimal.MinValue, "MinValue");
			yield return Case(new DecimalConveyor(), Decimal.MaxValue, "MaxValue");
			yield return Case(new StringConveyor(), String.Empty, "empty");
			yield return Case(new StringConveyor(), GoodLongString, "sample");
			yield return Case(new DateTimeConveyor(), DateTime.MinValue, "MinValue");
			yield return Case(new DateTimeConveyor(), DateTime.MaxValue, "MaxValue");
			yield return Case(new TimeSpanConveyor(), TimeSpan.MinValue, "MinValue");
			yield return Case(new TimeSpanConveyor(), TimeSpan.MaxValue, "MaxValue");
			yield return Case(new GuidConveyor(), Guid.Empty, "Empty");
			yield return Case(new GuidConveyor(), SampleGuid, "sample");
		}

		/// <summary>
		/// The engine never hands a conveyor a buffer of its own: it writes every column of a row into one shared
		/// buffer at that column's offset. So a round trip at offset zero is only half of the behaviour - the other
		/// half is that writing at an offset touches exactly the GetSize bytes from there and nothing on either side
		/// of them, which is what the sentinel below pins.
		/// </summary>
		[TestCaseSource(nameof(ByteBufferConveyorCases))]
		public void AByteBufferConveyorRoundTripsItsValueAtAnyOffset(Conveyor conveyor, object value)
		{
			int size = conveyor.GetSize(value);

			byte[] buffer = new byte[size];
			conveyor.Write(value, buffer, 0);
			Assert.That(conveyor.Read(buffer, 0), Is.EqualTo(value), "the value did not survive a round trip at offset zero");

			// Three bytes either side: an odd lead-in, so that no value starts on the alignment its own width would
			// give it, and a trailer to catch a write that runs past the size it asked for.
			const int Lead = 3;
			const int Trailer = 3;
			const byte Sentinel = 0xCD;

			byte[] sharedBuffer = new byte[Lead + size + Trailer];
			Array.Fill(sharedBuffer, Sentinel);
			conveyor.Write(value, sharedBuffer, Lead);

			Assert.That(conveyor.Read(sharedBuffer, Lead), Is.EqualTo(value), "the value did not survive a round trip at a non-zero offset");
			Assert.That(sharedBuffer[..Lead], Is.All.EqualTo(Sentinel), "writing at an offset overwrote the bytes before it");
			Assert.That(sharedBuffer[(Lead + size)..], Is.All.EqualTo(Sentinel), "writing at an offset wrote past the size the conveyor asked for");
		}

		private static IEnumerable<TestCaseData> StreamingConveyorCases()
		{
			yield return Case(new BinaryConveyor(), Array.Empty<byte>(), "empty");
			yield return Case(new BinaryConveyor(), new byte[] { 0x00, 0x01, 0xFE, 0xFF }, "sample");

			// LibraryReference is one of the three D4 types registered to this conveyor, so this is the round trip it
			// actually performs; it is also what a .d4l library descriptor is made of, which is why BOP can carry it.
			yield return Case(new BOPObjectConveyor(), new Schema.LibraryReference("TestLibrary", new VersionNumber(1, 2, 3, 4)), "a library reference");
		}

		/// <summary>
		/// A streaming conveyor reads from wherever the stream is positioned, so the read side is given the stream
		/// back at the beginning - which is what the engine does as well.
		/// </summary>
		[TestCaseSource(nameof(StreamingConveyorCases))]
		public void AStreamingConveyorRoundTripsItsValueThroughAStream(Conveyor conveyor, object value)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				conveyor.Write(value, stream);
				stream.Position = 0;

				Assert.That(conveyor.Read(stream), Is.EqualTo(value), "the value did not survive a round trip through a stream");
			}
		}

		/// <summary>
		/// Names each case by its conveyor and its value rather than by the value's text: a boundary value's text is
		/// either culture-dependent (DateTime.MaxValue) or nothing at all (String.Empty).
		/// </summary>
		private static TestCaseData Case(Conveyor conveyor, object value, string valueName)
		{
			return new TestCaseData(conveyor, value).SetArgDisplayNames(conveyor.GetType().Name, valueName);
		}
	}
}
