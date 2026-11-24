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
	/// written (bug-binary-write-accessor-keeps-the-old-values-tail).
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
		/// Declares a variable of the given type holding the initial value, writes the new value through its Value
		/// representation, and raises in D4 unless the read-back is exactly the new value. The error text carries both
		/// the expected value and what was read, since the failure this is here for reads back as a third string that
		/// is neither.
		/// </summary>
		private void AssertWriteReplaces(string typeName, string initialValue, string newValue)
		{
			ExecuteScript
			(
				String.Format
				(
					@"
						begin
							var LV : {0} := {0}('{1}');
							LV.Value := '{2}';
							if LV.Value <> '{2}' then
								raise Error('Expected ''{2}'' but read back ''' + LV.Value + '''.');
						end;
					",
					typeName,
					initialValue,
					newValue
				)
			);
		}
	}
}
