/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// The reference bookkeeping of <see cref="ServerStreamManager"/>: that a stream survives a close and a reopen,
	/// that two streams opened on one identifier do not share a position, how a reference and its source insulate
	/// each other from a write, and that deallocating a stream in the middle of a reference chain leaves the streams
	/// below it reading the value they had.
	/// </summary>
	/// <remarks>
	/// These assertions are 3.1's. It shipped them as the D4 diagnostic operator TestStreamManager(), whose C# is
	/// still in the tree at tests/libraries/TestFramework.Coverage.Base/Source/TestNodes.cs - inside an #if on a
	/// symbol nothing defines, written against a pre-3.0 engine API, and so not compiled for years. There is no
	/// reference runtime for this engine, so what 3.1 asserted is the record of what correct looks like; the payload
	/// strings below are the ones it used, and the cases follow the order it made its assertions in.
	///
	/// They are a C# fixture rather than a D4 operator because the manager needs no engine: its constructor takes an
	/// IServer and ignores it, building a MemoryStreamProvider and nothing else, so a test can construct one and
	/// drive Allocate, Open, Close, Reference, Deallocate and Count directly. That is the lowest layer that shows
	/// the behaviour, it makes Count assertable as an exact number, and it reaches two paths no D4 script can -
	/// MoveReferences, and the re-parenting Deallocate does in the middle of a reference chain - because the only
	/// stream-backed values a script can make are variables. debt-streams-coverage covers what D4 does reach.
	///
	/// Every public method of ServerStreamManager is lock (this) and every test here drives it from one thread, so
	/// nothing below says anything about concurrent use.
	///
	/// Stream identifiers are allocation order from a counter that only goes up, so nothing here asserts on one:
	/// the assertions are on counts and on contents.
	/// </remarks>
	[TestFixture]
	public class ServerStreamManagerTest
	{
		/// <summary>
		/// The owner identifier every Open below passes. The manager records it on the ManagedStream it returns and
		/// hands it back to Close; nothing reads it, so one value for the whole fixture is enough.
		/// </summary>
		private const int OwnerID = 1;

		private const string GoodLongString = "This is a good long string";
		private const string SourceOriginal = "This is the value for stream 1";
		private const string SourceChanged = "This is the new value for stream 1";
		private const string ReferenceChanged = "This is the new value for stream 3";

		private ServerStreamManager _manager;

		[SetUp]
		public void SetUp()
		{
			// A manager per test is what makes the Count assertions exact: a fresh one holds nothing, so "back to the
			// count it started with" is zero, and no earlier test can leave a header behind to confuse a later one.
			_manager = new ServerStreamManager(null);
		}

		[TearDown]
		public void TearDown()
		{
			if (_manager != null)
			{
				_manager.Dispose();
				_manager = null;
			}
		}

		[Test]
		public void AStreamReadsBackWhatWasWrittenAfterACloseAndReopen()
		{
			StreamID streamID = _manager.Allocate();

			Stream stream = _manager.Open(OwnerID, streamID, LockMode.Exclusive);
			WriteString(stream, GoodLongString);
			stream.Close();

			stream = _manager.Open(OwnerID, streamID, LockMode.Shared);
			try
			{
				Assert.That(ReadString(stream), Is.EqualTo(GoodLongString), "a stream did not read back what was written to it before it was closed");
			}
			finally
			{
				stream.Close();
			}

			_manager.Deallocate(streamID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// Two opens on one identifier share the bytes and nothing else. Each one gets its own ManagedStream, which
		/// is a CoverStream and so carries its own position over the single underlying stream; reading through one
		/// would otherwise move the other's position out from under its holder.
		/// </summary>
		[Test]
		public void TwoStreamsOpenedOnOneIdentifierDoNotShareAPosition()
		{
			StreamID streamID = _manager.Allocate();

			Stream first = _manager.Open(OwnerID, streamID, LockMode.Exclusive);
			WriteString(first, GoodLongString);
			first.Close();

			first = _manager.Open(OwnerID, streamID, LockMode.Shared);
			Stream second = _manager.Open(OwnerID, streamID, LockMode.Shared);
			try
			{
				first.Position = 0;

				Assert.That(ReadString(second), Is.EqualTo(GoodLongString), "the second stream opened on an identifier did not read the stream's contents");
				Assert.That(first.Position, Is.Zero, "reading through one stream moved the position of another stream open on the same identifier");
			}
			finally
			{
				// Two opens, two closes, and then the deallocate: the count below is what says the manager let go of
				// everything rather than keeping the header alive on an open count that never reached zero.
				second.Close();
				first.Close();
			}

			_manager.Deallocate(streamID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		[Test]
		public void AReferenceTwoDeepReadsTheValueItWasTakenFrom()
		{
			StreamID sourceID = _manager.Allocate();
			Stream source = _manager.Open(OwnerID, sourceID, LockMode.Exclusive);
			WriteString(source, SourceOriginal);

			StreamID firstID = _manager.Reference(sourceID);
			Stream first = _manager.Open(OwnerID, firstID, LockMode.Exclusive);

			StreamID secondID = _manager.Reference(firstID);
			Stream second = _manager.Open(OwnerID, secondID, LockMode.Exclusive);

			try
			{
				Assert.That(ReadString(first), Is.EqualTo(SourceOriginal), "a reference did not read the value of the stream it was taken from");
				Assert.That(ReadString(second), Is.EqualTo(SourceOriginal), "a reference of a reference did not read the value of the stream it was taken from");
			}
			finally
			{
				second.Close();
				first.Close();
				source.Close();
			}

			_manager.Deallocate(secondID);
			_manager.Deallocate(firstID);
			_manager.Deallocate(sourceID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// The copy-on-write path: ServerStream.Write calls ServerStreamManager.Change, which copies a referencing
		/// stream off its source before the write lands, so the holder of the source keeps the value it had.
		/// </summary>
		[Test]
		public void WritingThroughAReferenceLeavesTheSourceWithItsOwnValue()
		{
			StreamID sourceID = _manager.Allocate();
			Stream source = _manager.Open(OwnerID, sourceID, LockMode.Exclusive);
			WriteString(source, SourceOriginal);

			StreamID referenceID = _manager.Reference(sourceID);
			Stream reference = _manager.Open(OwnerID, referenceID, LockMode.Exclusive);

			try
			{
				WriteString(reference, ReferenceChanged);

				Assert.That(ReadString(reference), Is.EqualTo(ReferenceChanged), "a reference did not read back the value written through it");
				Assert.That(ReadString(source), Is.EqualTo(SourceOriginal), "writing through a reference changed the value its source reads");
			}
			finally
			{
				reference.Close();
				source.Close();
			}

			_manager.Deallocate(referenceID);
			_manager.Deallocate(sourceID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// The half of 3.1's three-value assertion that the engine still holds, kept live on its own because the test
		/// carrying all three of them is ignored until bug-writing-a-large-value-changes-copies-of-it is fixed: a
		/// source with a reference outstanding still reads back what was written through it. It is the assertion
		/// either shape of that fix could break - the re-parenting shape hands the provider's stream to one of the
		/// references, which leaves the source's own open stream to be pointed somewhere - so it is the one worth
		/// holding while the fix is made rather than parking with the rest.
		/// </summary>
		[Test]
		public void WritingASourceThatHasReferencesLeavesItReadingItsOwnNewValue()
		{
			StreamID sourceID = _manager.Allocate();
			Stream source = _manager.Open(OwnerID, sourceID, LockMode.Exclusive);
			WriteString(source, SourceOriginal);

			StreamID referenceID = _manager.Reference(sourceID);
			Stream reference = _manager.Open(OwnerID, referenceID, LockMode.Exclusive);

			try
			{
				WriteString(source, SourceChanged);

				Assert.That(ReadString(source), Is.EqualTo(SourceChanged), "a source stream with a reference outstanding did not read back the value written through it");
			}
			finally
			{
				reference.Close();
				source.Close();
			}

			_manager.Deallocate(referenceID);
			_manager.Deallocate(sourceID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// The other direction of the same insulation, and 3.1's assertions for streams 1, 2 and 3 in one case: with
		/// two references outstanding, writing the source and writing one of the references must leave three distinct
		/// values - the source with its new value, the untouched reference with the value it was taken from, and the
		/// written reference with its own.
		///
		/// This is the one assertion of 3.1's that the engine no longer holds, and it is left here as 3.1 wrote it
		/// because it is the reproduction the fix is owed: a write through the source lands in the provider's stream
		/// that the references are still reading through, so the untouched reference reads the source's new value.
		/// Ignored rather than left failing so that it does not drown the signal from every other run; turn it back
		/// on with the fix. See bug-writing-a-large-value-changes-copies-of-it.
		/// </summary>
		[Test]
		[Ignore("Reproduces bug-writing-a-large-value-changes-copies-of-it: writing a source stream is visible through every outstanding reference to it, so the untouched reference reads the source's new value instead of the value it was taken from.")]
		public void WritingASourceAndOneOfItsTwoReferencesLeavesThreeDistinctValues()
		{
			StreamID sourceID = _manager.Allocate();
			Stream source = _manager.Open(OwnerID, sourceID, LockMode.Exclusive);
			WriteString(source, SourceOriginal);

			StreamID untouchedID = _manager.Reference(sourceID);
			Stream untouched = _manager.Open(OwnerID, untouchedID, LockMode.Exclusive);

			StreamID writtenID = _manager.Reference(sourceID);
			Stream written = _manager.Open(OwnerID, writtenID, LockMode.Exclusive);

			try
			{
				WriteString(source, SourceChanged);
				WriteString(written, ReferenceChanged);

				Assert.That(ReadString(source), Is.EqualTo(SourceChanged), "a source stream did not read back the value written through it");
				Assert.That(ReadString(untouched), Is.EqualTo(SourceOriginal), "writing through a source changed the value an untouched reference to it reads");
				Assert.That(ReadString(written), Is.EqualTo(ReferenceChanged), "a reference did not read back the value written through it");
			}
			finally
			{
				written.Close();
				untouched.Close();
				source.Close();
			}

			_manager.Deallocate(writtenID);
			_manager.Deallocate(untouchedID);
			_manager.Deallocate(sourceID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// MoveReferences and the re-parenting in Deallocate, which nothing else in the suite reaches. The middle of
		/// the chain is written through before the chain below it is taken, so that it holds a stream of its own
		/// rather than its source's - that is the shape Deallocate has to re-parent, because it has to hand the
		/// provider's stream to one of the references and point the rest at it.
		/// </summary>
		[Test]
		public void DeallocatingTheMiddleOfAReferenceChainLeavesTheStreamsBelowItReadingItsValue()
		{
			StreamID sourceID = _manager.Allocate();
			Stream source = _manager.Open(OwnerID, sourceID, LockMode.Exclusive);
			WriteString(source, SourceOriginal);

			StreamID middleID = _manager.Reference(sourceID);
			Stream middle = _manager.Open(OwnerID, middleID, LockMode.Exclusive);
			WriteString(middle, ReferenceChanged);

			StreamID firstID = _manager.Reference(middleID);
			Stream first = _manager.Open(OwnerID, firstID, LockMode.Exclusive);

			StreamID secondID = _manager.Reference(firstID);
			Stream second = _manager.Open(OwnerID, secondID, LockMode.Exclusive);

			middle.Close();
			_manager.Deallocate(middleID);

			try
			{
				Assert.That(ReadString(first), Is.EqualTo(ReferenceChanged), "a reference to a deallocated stream did not read the value that stream had");
				Assert.That(ReadString(second), Is.EqualTo(ReferenceChanged), "a reference two deep below a deallocated stream did not read the value that stream had");
			}
			finally
			{
				second.Close();
				first.Close();
				source.Close();
			}

			_manager.Deallocate(secondID);
			_manager.Deallocate(firstID);
			_manager.Deallocate(sourceID);
			Assert.That(_manager.Count(), Is.Zero, "the manager was still holding a stream after everything it allocated had been deallocated");
		}

		/// <summary>
		/// Deallocate does not consult the header's open count, so a stream deallocated while a ManagedStream is
		/// still open on it leaves that stream pointing at a destroyed provider entry. Nothing in the engine does
		/// this - every call site closes first, which is what 3.1's test did too - and what this pins is that the
		/// outcome is loud rather than a read of freed state: the open stream throws on its next use, and closing it
		/// throws as well, because the header its Close goes looking for is gone. See the NOTE on
		/// ServerStreamManager.Deallocate for why that is left as it is.
		/// </summary>
		[Test]
		public void DeallocatingAStreamThatIsStillOpenLeavesThatStreamUnusable()
		{
			StreamID streamID = _manager.Allocate();
			Stream stream = _manager.Open(OwnerID, streamID, LockMode.Exclusive);
			WriteString(stream, GoodLongString);

			_manager.Deallocate(streamID);

			Assert.That(_manager.Count(), Is.Zero, "Deallocate did not remove the header of a stream that was still open");
			Assert.Throws<ObjectDisposedException>(() => stream.Position = 0, "reading the length of a stream whose provider entry had been destroyed did not throw");
			Assert.Throws<StreamsException>(() => stream.Close(), "closing a stream whose header had been deallocated did not throw");
		}

		/// <summary>
		/// Writes a string over whatever the stream holds, from the beginning, the way 3.1's test did: through
		/// StringConveyor, which is a byte-buffer conveyor, so the value is rendered into a buffer and the buffer
		/// written in one call. The length prefix the conveyor writes is what makes the read side exact - the tail of
		/// a longer previous value left behind past the end cannot be mistaken for part of the value.
		/// </summary>
		private static void WriteString(Stream stream, string value)
		{
			StringConveyor conveyor = new StringConveyor();
			byte[] buffer = new byte[conveyor.GetSize(value)];
			conveyor.Write(value, buffer, 0);
			stream.Position = 0;
			stream.Write(buffer, 0, buffer.Length);
		}

		private static string ReadString(Stream stream)
		{
			stream.Position = 0;
			byte[] buffer = new byte[stream.Length];
			int read = 0;
			while (read < buffer.Length)
			{
				int count = stream.Read(buffer, read, buffer.Length - read);
				if (count == 0)
					break;
				read += count;
			}
			return (string)new StringConveyor().Read(buffer, 0);
		}
	}
}
