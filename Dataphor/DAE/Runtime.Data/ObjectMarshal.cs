using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public static class ObjectMarshal
	{
		/// <summary>
		/// Converts the C# host representation of a value to the "Native" representation (using NativeLists, NativeRows, and NativeTables)
		/// </summary>
		/// <param name="dataType">The target data type for the conversion.</param>
		/// <param name="value">The source value to be converted.</param>
		/// <returns>The value in its "Native" representation.</returns>
		public static object ToNativeOf(IValueManager valueManager, Schema.IDataType dataType, object value)
		{
			if (value != null)
			{
				var scalarType = dataType as Schema.IScalarType;
				if (scalarType != null)
				{
					if (scalarType.Equals(valueManager.DataTypes.SystemString) && !(value is String))
						return value.ToString(); // The usual scenario would be an enumerated type...

					return value; // Otherwise, return the C# representation directly
				}

				var listType = dataType as Schema.IListType;
				if (listType != null && value != null)
				{
					var listValue = value as ListValue;
					if (listValue != null)
						return listValue;

					var nativeList = value as NativeList;
					if (nativeList != null)
						return nativeList;

					var iList = value as IList;
					if (iList != null)
					{
						var newList = new NativeList();
						for (int index = 0; index < iList.Count; index++)
						{
							newList.DataTypes.Add(listType.ElementType);
							newList.Values.Add(iList[index]);
						}

						return newList;
					}

					throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for property: {0}", value.GetType().FullName));
				}

				var tableType = dataType as Schema.ITableType;
				if (tableType != null && value != null)
				{
					var tableValue = value as TableValue;
					if (tableValue != null)
						return tableValue;

					var nativeTable = value as NativeTable;
					if (nativeTable != null)
						return nativeTable;

					var iDictionary = value as IDictionary;
					if (iDictionary != null)
					{
						var newTableVar = new Schema.BaseTableVar(tableType);
						newTableVar.EnsureTableVarColumns();
						// Assume the first column is the key, this is potentially problematic, but without key information in table types, not much else we can do....
						// The assumption here is that the C# representation of a "table" is a dictionary
						newTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { newTableVar.Columns[0] }));
						var newTable = new NativeTable(valueManager, newTableVar);
						foreach (DictionaryEntry entry in iDictionary)
						{
							using (Row row = new Row(valueManager, newTableVar.DataType.RowType))
							{
								row[0] = ToNativeOf(valueManager, tableType.Columns[0].DataType, entry.Key);
								row[1] = ToNativeOf(valueManager, tableType.Columns[1].DataType, entry.Value);
								newTable.Insert(valueManager, row);
							}
						}

						return newTable;
					}

					throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for property: {0}", value.GetType().FullName));
				}
			}

			return value;
		}

		// We need something here, but the current implementation of ObjectPropertyWriteNode needs the property, not just the value...
		//public static object FromNativeOf(Schema.IDataType dataType, object value)
		//{
		//}
	}
}
