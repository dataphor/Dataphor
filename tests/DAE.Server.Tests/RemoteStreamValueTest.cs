/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A client handed a value the engine keeps in a managed stream - Binary or Graphic - receives the stream's
	/// identifier rather than its bytes, and reads the bytes back over the connection when it is asked for them.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The physical form of a value is written with streams unexpanded (<c>DataValue.AsPhysical</c> passes
	/// <c>expandStreams: false</c>), and <c>ScalarValue.WriteToPhysical</c> then writes the stream identifier for a
	/// value that is not native. <c>RemoteServerExpressionPlan.Evaluate</c> is the caller reached here: it hands the
	/// client that physical form, and <c>LocalExpressionPlan.Evaluate</c> rebuilds the value against the client's own
	/// value manager, whose stream manager is a <c>LocalStreamManager</c>. So the client ends up holding a value it has
	/// no bytes for, and opening it builds a <c>LocalStream</c> whose source - its only kind of source - is a
	/// <c>RemoteStreamWrapper</c> over the server's stream.
	/// </para>
	/// <para>
	/// One case, because the row path is the same decision made in the same place: <c>RowValue.WriteToPhysical</c>
	/// writes a stream identifier for a non-native column under the same <c>expandStreams: false</c>, and a table
	/// column is stored natively by the in-memory device, so a scalar expression is both the shortest way to reach the
	/// path and the only one that reaches it without a device that keeps its values in streams. Binary rather than
	/// Graphic: the two share the selector node, the accessor nodes and the conveyor, so nothing here can tell them
	/// apart.
	/// </para>
	/// <para>
	/// The load-bearing assertion is that the client's value is not native: a value marshalled inline would be, and
	/// then nothing on the client would open a stream at all. Given a non-native client scalar the wrapper follows by
	/// construction - <c>Scalar.OpenStream</c> goes to its own manager's stream manager, the client's is a
	/// <c>LocalStreamManager</c>, and the only source a <c>LocalStream</c> ever builds is a
	/// <c>RemoteStreamWrapper</c>. The second assertion is what holds that construction in place: the engine's own
	/// stream manager hands out <c>ManagedStream</c>s where the client's hands out a plain cover over a
	/// <c>LocalStream</c>, so a client that somehow ended up reading through the engine's manager directly would fail
	/// here rather than pass. Neither the wrapper nor the <c>LocalStream</c> can be asserted on directly:
	/// <c>LocalStreamManager</c> returns a cover over the one it holds, and <c>LocalStream.SourceStream</c> is
	/// protected.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class RemoteStreamValueTest : ClientStackTestFixture
	{
		/// <summary>Eight bytes - 00 11 22 33 44 55 66 77 - as base-64, which is what the Binary selector takes.</summary>
		private const string EightBytes = "ABEiM0RVZnc=";

		[Test]
		public void AClientOpensAStreamBackedValueThroughItsOwnStreamManager()
		{
			using (IDataValue value = ClientProcess.Evaluate(String.Format("Binary('{0}')", EightBytes), null))
			{
				Assert.That(value.IsNative, Is.False, "The client was handed the value's bytes rather than its stream identifier.");

				using (Stream stream = value.OpenStream())
				{
					Assert.That(stream, Is.Not.InstanceOf<ManagedStream>(), "The client read the value through the engine's own stream manager rather than its own.");

					byte[] buffer = new byte[stream.Length];
					stream.ReadExactly(buffer);
					Assert.That(Convert.ToBase64String(buffer), Is.EqualTo(EightBytes), "The bytes read back over the connection were not the value evaluated.");
				}
			}
		}
	}
}
