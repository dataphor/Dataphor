using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

					if (scalarType.Equals(valueManager.DataTypes.SystemDateTime) && value is DateTimeOffset)
						return new DateTime(((DateTimeOffset)value).Ticks);

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
							newList.Values.Add(ToNativeOf(valueManager, listType.ElementType, iList[index]));
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

		public static void SetHostProperty(object instance, PropertyInfo property, object value)
		{
			if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
			{
				if (property.PropertyType.TypeOrUnderlyingNullableType().IsEnum && value is string && property.CanWrite)
				{
					property.SetValue(instance, Enum.Parse(property.PropertyType.TypeOrUnderlyingNullableType(), (string)value, true), null);
				}
				else if (property.PropertyType.TypeOrUnderlyingNullableType() == typeof(DateTimeOffset) && value is DateTime && property.CanWrite)
				{
					property.SetValue(instance, new DateTimeOffset((DateTime)value), null);
				}
				else if (value is TableValue)
				{
					var sourceTableValue = value as TableValue;

					IDictionary targetDictionaryValue = null;

					if (property.CanWrite)
					{
						// If the target property supports assignment, construct a new dictionary and perform the assignment
						targetDictionaryValue = Activator.CreateInstance(property.PropertyType) as IDictionary;
						if (targetDictionaryValue == null)
							throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));
					}
					else
					{
						// Otherwise, we clear the current dictionary and then add the elements to it
						targetDictionaryValue = property.GetValue(instance, null) as IDictionary;
						if (targetDictionaryValue == null)
							throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));

						targetDictionaryValue.Clear();
					}

					using (var iTable = sourceTableValue.OpenCursor())
					{
						while (iTable.Next())
						{
							using (var row = iTable.Select())
							{
								targetDictionaryValue.Add(row[0], row[1]);
							}
						}
					}

					if (property.CanWrite)
					{
						property.SetValue(instance, targetDictionaryValue, null);
					}
				}
				else if (value is IList)
				{
					// In theory, the only way this is possible is if the source is a ListValue and the target is a native list representation such as List<T> or other IList
					var sourceListValue = value as IList;
					if (sourceListValue == null)
						throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for source value: {0}", value.GetType().FullName));

					IList targetListValue = null;

					if (property.CanWrite)
					{
						// If the target property supports assignment, we construct a new list of the type of the property, and perform the assignment
						targetListValue = Activator.CreateInstance(property.PropertyType) as IList;
						if (targetListValue == null)
							throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));
					}
					else
					{
						// Otherwise, we attempt to clear the current list and then add the elements to it
						targetListValue = property.GetValue(instance, null) as IList;
						if (targetListValue == null)
							throw new RuntimeException(RuntimeException.Codes.InternalError, String.Format("Unexpected type for target property: {0}", property.PropertyType.FullName));

						targetListValue.Clear();
					}

					for (int index = 0; index < sourceListValue.Count; index++)
						targetListValue.Add(sourceListValue[index]);

					if (property.CanWrite)
						property.SetValue(instance, targetListValue, null);
				}
			}
			else
			{
				if (property.CanWrite)
					property.SetValue(instance, value, null);
			}
		}
	}
}
