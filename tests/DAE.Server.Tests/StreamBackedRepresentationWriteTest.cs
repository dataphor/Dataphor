/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Writing through a representation of a value the engine keeps in a managed stream - Binary and Graphic - sets
	/// the value: what the representation reads back afterwards is exactly what was written, at any length. The write
	/// accessor used to write the new bytes over the existing stream without truncating it first, so a value shorter
	/// than the one already there kept the tail of the old one on its end and the read-back was longer than what was
	/// written. The accessor node also read the value being written to as a stream identifier without allowing for
	/// there being no value yet, so a write to a nil variable failed with a NullReferenceException instead of setting
	/// it; and it - like the selector beside it - took a stream out of the stream manager before reading the base-64 it
	/// was given, so a value that did not parse held that stream for the life of the session (all
	/// bug-binary-write-accessor-keeps-the-old-values-tail).
	///
	/// Setting the value also leaves any copy of it alone: a copy taken before the write keeps the value it was taken
	/// from. The engine does not copy the bytes of a stream-backed value when it is copied - the copy reads the
	/// original's stream until one of them is written - and the separation used to happen only when the write went
	/// through the copy, so writing the original changed every copy outstanding of it
	/// (bug-writing-a-large-value-changes-copies-of-it).
	/// </summary>
	/// <remarks>
	/// Binary and Graphic are the whole of the rule: they are the only two types the engine holds in a stream, each
	/// has exactly one writable representation - Value, base-64 - and both route through the same write accessor
	/// node, so these cases are both the general test and the specific one. Their other representation,
	/// AsDisplayString, is read-only on both types and raises unconditionally, so there is nothing there to cover.
	///
	/// The three lengths are the whole of the behaviour - shorter over longer is the defect, same length and longer
	/// over shorter are what already worked and must keep working - and the lengths differ by four bytes rather than
	/// one so that a fix which truncated by the wrong amount cannot pass. Each case asserts in D4 that the read-back
	/// base-64 is character-for-character what was written, which catches both a surviving tail and a value written
	/// short.
	/// </remarks>
	[TestFixture]
	public class StreamBackedRepresentationWriteTest : InProcessTestFixture
	{
		/// <summary>Eight bytes - 00 11 22 33 44 55 66 77 - whose distinctive tail would show up in a read-back if it survived a write.</summary>
		private const string EightBytes = "ABEiM0RVZnc=";

		/// <summary>Four bytes - AA BB CC DD - half the length of EightBytes, so a surviving tail is unmistakable.</summary>
		private const string FourBytes = "qrvM3Q==";

		/// <summary>Four other bytes - 01 02 03 04 - for the same-length case, which must still replace rather than be a no-op.</summary>
		private const string FourOtherBytes = "AQIDBA==";

		/// <summary>Not base-64 at all, so the conversion raises before the engine has a value to hold.</summary>
		private const string NotBase64 = "not base64 at all!";

		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void WritingAShorterValueReplacesTheWholeValue(string typeName)
		{
			AssertWriteReplaces(typeName, EightBytes, FourBytes);
		}

		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void WritingAValueOfTheSameLengthReplacesTheWholeValue(string typeName)
		{
			AssertWriteReplaces(typeName, FourBytes, FourOtherBytes);
		}

		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void WritingALongerValueReplacesTheWholeValue(string typeName)
		{
			AssertWriteReplaces(typeName, FourBytes, EightBytes);
		}

		/// <summary>
		/// A write to a variable that has no value yet sets it, rather than propagating the nil or failing: there is no
		/// stream to write into, so the accessor allocates one. This is the case the accessor node used to crash on.
		/// </summary>
		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void WritingToANilValueSetsIt(string typeName)
		{
			AssertWriteSets(String.Format("var LV : {0};", typeName), FourBytes);
		}

		/// <summary>
		/// A value the engine could not parse never had a stream allocated for it. Both the selector and the write
		/// accessor used to allocate before parsing the base-64 they were given, so a malformed value held a managed
		/// stream for the life of the session. StreamCount is the engine's own count of the streams it is holding.
		/// </summary>
		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void AValueThatDoesNotParseLeavesNoStreamBehind(string typeName)
		{
			ExecuteScript
			(
				String.Format
				(
					@"
						begin
							var LBefore : Integer := StreamCount();
							begin
								var LV : {0};
								try LV.Value := '{1}'; except end;
							end;
							if StreamCount() <> LBefore then
								raise Error('A write of a value that does not parse left ' + ToString(StreamCount() - LBefore) + ' stream(s) behind.');
							begin
								try var LW : {0} := {0}('{1}'); except end;
							end;
							if StreamCount() <> LBefore then
								raise Error('A selector given a value that does not parse left ' + ToString(StreamCount() - LBefore) + ' stream(s) behind.');
						end;
					",
					typeName,
					NotBase64
				)
			);
		}

		/// <summary>
		/// A copy of a stream-backed value taken before the original is written keeps the value it was taken from,
		/// rather than following the write through the original. This is the language-layer case for
		/// bug-writing-a-large-value-changes-copies-of-it; ServerStreamManagerTest carries the same behaviour at the
		/// stream manager, which is where the fix is.
		/// </summary>
		/// <remarks>
		/// One case covers every shape a copy is taken in - at declaration, as it is here, by a later assignment, and
		/// into a row column - because all three reach ServerStreamManager.Reference through DataValue.CopyNative and
		/// DataValue.CopyValue, and all three were confirmed to reproduce and to be fixed by the one change. The
		/// assertion on the variable that was written is as load-bearing as the one on the copy: a fix that separated
		/// the copy but lost the original's own write would otherwise pass.
		/// </remarks>
		[TestCase("Binary")]
		[TestCase("Graphic")]
		public void ACopyTakenBeforeAWriteKeepsTheValueItWasTakenFrom(string typeName)
		{
			ExecuteScript
			(
				String.Format
				(
					@"
						begin
							var LV : {0} := {0}('{1}');
							var LCopy : {0} := LV;
							LV.Value := '{2}';
							if LV.Value <> '{2}' then
								raise Error('Expected the variable written through to read back ''{2}'' but it read back ''' + LV.Value + '''.');
							if LCopy.Value <> '{1}' then
								raise Error('Expected a copy taken before the write to keep ''{1}'' but it read back ''' + LCopy.Value + '''.');
						end;
					",
					typeName,
					EightBytes,
					FourBytes
				)
			);
		}

		/// <summary>Writes the new value over a variable of the given type that already holds the initial value.</summary>
		private void AssertWriteReplaces(string typeName, string initialValue, string newValue)
		{
			AssertWriteSets(String.Format("var LV : {0} := {0}('{1}');", typeName, initialValue), newValue);
		}

		/// <summary>
		/// Runs the given declaration of a variable named LV, writes the new value through its Value representation, and
		/// raises in D4 unless the read-back is exactly the new value. The error text carries both the expected value and
		/// what was read, since the failure this is here for reads back as a third string that is neither.
		/// </summary>
		private void AssertWriteSets(string declaration, string newValue)
		{
			ExecuteScript
			(
				String.Format
				(
					@"
						begin
							{0}
							LV.Value := '{1}';
							if LV.Value <> '{1}' then
								raise Error('Expected ''{1}'' but read back ''' + LV.Value + '''.');
						end;
					",
					declaration,
					newValue
				)
			);
		}
	}
}
