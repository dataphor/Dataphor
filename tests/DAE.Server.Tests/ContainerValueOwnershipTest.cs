/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// A container value - a Row or a ListValue - releases what it holds when it is disposed, and only when it owns it.
	/// DataValue.ValuesOwned is the flag that says which, and both halves of the contract are asserted here over the
	/// value classes directly: a container that owns its contents releases every one of them, and a container built as
	/// a window onto a native structure something else holds releases none.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The flag is declared on DataValue and honoured by each subclass separately, so neither the build nor a D4 script
	/// says whether a given subclass honours it at all - which is how ListValue came to have no Dispose of its own,
	/// releasing none of its elements for the life of the session (bug-a-list-value-never-releases-its-elements). These
	/// cases reach the classes rather than the language for that reason: a new container, or a new path through an
	/// existing one, fails here at the layer where the rule lives. StreamValues.d4 is the D4-visible half of it.
	/// </para>
	/// <para>
	/// The instrument is Diagnostics.StreamCount(), the number of streams the server is holding, read before and after
	/// each case; a stream-backed Scalar - the non-native constructor allocates one - is what makes a held value
	/// countable. Exact counts, because the assertion is arithmetic: the count inside a case says how many copies the
	/// container took, and the count after it says whether it gave them back.
	/// </para>
	/// </remarks>
	[TestFixture]
	public class ContainerValueOwnershipTest : InProcessTestFixture
	{
		/// <summary>Two values rather than one, so that a container releasing only its first is not a pass.</summary>
		private const int ElementCount = 2;

		[Test]
		public void ARowReleasesItsColumnValuesWhenItIsDisposed()
		{
			int before = StreamCount();
			using (Row row = new Row(Manager, RowOfBinary()))
			{
				FillWithStreams(row);
				Assert.That(StreamCount(), Is.EqualTo(before + ElementCount), "The row did not take a copy of each column value it was given.");
			}

			Assert.That(StreamCount(), Is.EqualTo(before), "The row left its column values behind.");
		}

		[Test]
		public void AListReleasesItsElementsWhenItIsDisposed()
		{
			int before = StreamCount();
			using (ListValue list = new ListValue(Manager, ListOfBinary()))
			{
				for (int index = 0; index < ElementCount; index++)
					using (Scalar element = NewStream())
						list.Add(element);

				Assert.That(StreamCount(), Is.EqualTo(before + ElementCount), "The list did not take a copy of each element it was given.");
			}

			Assert.That(StreamCount(), Is.EqualTo(before), "The list left its elements behind.");
		}

		[Test]
		public void AListOfRowsReleasesTheValuesInThemWhenItIsDisposed()
		{
			int before = StreamCount();
			using (ListValue list = new ListValue(Manager, new Schema.ListType(RowOfBinary())))
			{
				for (int index = 0; index < ElementCount; index++)
					using (Row row = new Row(Manager, RowOfBinary()))
					{
						FillWithStreams(row);
						list.Add(row);
					}

				Assert.That(StreamCount(), Is.EqualTo(before + (ElementCount * ElementCount)), "The list did not take a copy of each row it was given.");
			}

			Assert.That(StreamCount(), Is.EqualTo(before), "The list left the values in its rows behind.");
		}

		[Test]
		public void ARowWithAListColumnReleasesTheElementsInItWhenItIsDisposed()
		{
			int before = StreamCount();
			Schema.RowType rowType = new Schema.RowType();
			rowType.Columns.Add(new Schema.Column("L", ListOfBinary()));

			using (Row row = new Row(Manager, rowType))
			{
				using (ListValue list = new ListValue(Manager, ListOfBinary()))
				{
					for (int index = 0; index < ElementCount; index++)
						using (Scalar element = NewStream())
							list.Add(element);

					row[0] = list;
				}

				Assert.That(StreamCount(), Is.EqualTo(before + ElementCount), "The row did not take a copy of the list it was given.");
			}

			Assert.That(StreamCount(), Is.EqualTo(before), "The row left the elements of its list column behind.");
		}

		/// <summary>
		/// The other half of the flag, and the assumption every DataValue.FromNative rests on: the host representation
		/// of a native structure is a window onto values something else holds, so disposing the window must release
		/// nothing. Both containers in one case, inside an Assert.Multiple so that the second is reached even when the
		/// first fails.
		/// </summary>
		[Test]
		public void AContainerOverANativeStructureItDoesNotOwnReleasesNothing()
		{
			Assert.Multiple
			(
				() =>
				{
					AssertWindowReleasesNothing
					(
						"A row",
						() =>
						{
							Row row = new Row(Manager, RowOfBinary());
							FillWithStreams(row);
							return row;
						}
					);

					AssertWindowReleasesNothing
					(
						"A list",
						() =>
						{
							ListValue list = new ListValue(Manager, ListOfBinary());
							for (int index = 0; index < ElementCount; index++)
								using (Scalar element = NewStream())
									list.Add(element);
							return list;
						}
					);
				}
			);
		}

		/// <summary>
		/// Builds an owned container, wraps a window around the native structure inside it, disposes the window and
		/// asserts the count has not moved; then disposes the owner and asserts it has. Every assertion names the
		/// container, because the case above runs this twice.
		/// </summary>
		private void AssertWindowReleasesNothing(string container, Func<DataValue> build)
		{
			int before = StreamCount();
			using (DataValue owner = build())
			{
				int held = StreamCount();
				using (IDataValue window = DataValue.FromNative(Manager, owner.DataType, owner.AsNative))
					Assert.That(window.ValuesOwned, Is.False, container + " built over a native structure it does not own claimed to own its contents.");

				Assert.That(StreamCount(), Is.EqualTo(held), container + " built over a native structure it does not own released something when it was disposed.");
			}

			Assert.That(StreamCount(), Is.EqualTo(before), container + " that owns its contents left them behind.");
		}

		/// <summary>A row type of ElementCount Binary columns, so that a row of it holds one stream per column.</summary>
		private Schema.RowType RowOfBinary()
		{
			Schema.RowType rowType = new Schema.RowType();
			for (int index = 0; index < ElementCount; index++)
				rowType.Columns.Add(new Schema.Column("B" + index.ToString(), Process.DataTypes.SystemBinary));
			return rowType;
		}

		private Schema.ListType ListOfBinary()
		{
			return new Schema.ListType(Process.DataTypes.SystemBinary);
		}

		/// <summary>Gives every column of the row a stream-backed value of its own, releasing each value it handed over.</summary>
		private void FillWithStreams(Row row)
		{
			for (int index = 0; index < row.DataType.Columns.Count; index++)
				using (Scalar value = NewStream())
					row[index] = value;
		}

		/// <summary>A Binary scalar holding a stream of its own: the non-native constructor allocates one.</summary>
		private Scalar NewStream()
		{
			return new Scalar(Manager, Process.DataTypes.SystemBinary);
		}

		private IValueManager Manager { get { return Process.ValueManager; } }

		/// <summary>How many streams the server is holding, from Diagnostics.StreamCount(), which resolves unqualified.</summary>
		private int StreamCount()
		{
			return (int)Process.Evaluate("StreamCount()", null).AsNative;
		}
	}
}
