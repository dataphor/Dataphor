/*
	Dataphor
	© Copyright 2000-2018 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class NativeRowMap : NativeRowIndex
	{
		public NativeRowMap(Schema.Key key, Dictionary<String, Schema.Sort> equalitySorts, Schema.RowType keyRowType, Schema.RowType dataRowType) : base(keyRowType, dataRowType)
		{
			_key = key;
			_equalityComparer = new NativeRowMapEqualityComparer(this);
			_rows = new Dictionary<NativeRow, NativeRow>(_equalityComparer);
			_equalitySorts = equalitySorts;
		}

		private Schema.Key _key;
		/// <summary>The description of the key for the index.</summary>
		public Schema.Key Key { get { return _key; } }

		private Dictionary<String, Schema.Sort> _equalitySorts;
		private Schema.Sort GetEqualitySort(String columnName)
		{
			return _equalitySorts[columnName];
		}

		private class NativeRowMapEqualityComparer : IEqualityComparer<NativeRow>
		{
			public NativeRowMapEqualityComparer(NativeRowMap nativeRowMap)
			{
				_nativeRowMap = nativeRowMap;
			}

			private NativeRowMap _nativeRowMap;

			public bool Equals(NativeRow left, NativeRow right)
			{
				return _nativeRowMap.CompareEqual(_nativeRowMap._manager, left, right);
			}

			public int GetHashCode(NativeRow row)
			{
				var result = 17;
				var changeMultiplier = false;

				for (int i = 0; i < row.Values.Count(); i++)
				{
					result = result + ((row.Values[i] == null ? 0 : row.Values[i].GetHashCode()) * (changeMultiplier ? 43 : 47));
					changeMultiplier = !changeMultiplier;
				}

				return result;
			}
		}

		private NativeRowMapEqualityComparer _equalityComparer;
		private Dictionary<NativeRow, NativeRow> _rows;
		
		internal IValueManager _manager; // Only used during the Add and Remove calls
		
		public bool CompareEqual(IValueManager manager, NativeRow indexKey, NativeRow compareKey)
		{
			// If AIndexKeyRowType is null, the index key must have the structure of an index key,
			// Otherwise, the IndexKey row could be a subset of the actual index key.
			// In that case, AIndexKeyRowType is the RowType for the IndexKey row.
			// It is the caller's responsibility to ensure that the passed IndexKey RowType 
			// is a subset of the actual IndexKey with order intact.
			//Row LIndexKey = new Row(AManager, AIndexKeyRowType, AIndexKey);
				
			// If ACompareContext is null, the compare key must have the structure of an index key,
			// Otherwise the CompareKey could be a subset of the actual index key.
			// In that case, ACompareContext is the RowType for the CompareKey row.
			// It is the caller's responsibility to ensure that the passed CompareKey RowType 
			// is a subset of the IndexKey with order intact.
			//Row LCompareKey = new Row(AManager, ACompareKeyRowType, ACompareKey);
				
			for (int index = 0; index < _keyRowType.Columns.Count; index++)
			{
				if (index >= compareKey.Values.Count())
				{
					return false;
				}
					
				if ((indexKey.Values[index] != null) && (compareKey.Values[index] != null))
				{
					if (_keyRowType.Columns[index].DataType is Schema.ScalarType)
					{
						if (!manager.EvaluateEqualitySort(GetEqualitySort(Key.Columns[index].Name), indexKey.Values[index], compareKey.Values[index]))
						{
							return false;
						}
					}
					else
					{
						using (var indexValue = DataValue.FromNative(manager, indexKey.DataTypes[index], indexKey.Values[index]))
						{
							using (var compareValue = DataValue.FromNative(manager, compareKey.DataTypes[index], compareKey.Values[index]))
							{
								if (!manager.EvaluateEqualitySort(GetEqualitySort(Key.Columns[index].Name), indexValue, compareValue))
								{
									return false;
								}
							}
						}
					}
				}
				else if (indexKey.Values[index] != null)
				{
					return false;
				}
				else if (compareKey.Values[index] != null)
				{
					return false;
				}
			}
			
			return true;
		}
		
		public override void Drop(IValueManager manager)
		{
			foreach (var key in _rows.Keys)
			{
				DisposeKey(manager, key);
			}

			foreach (var data in _rows.Values)
			{
				DisposeData(manager, data);
			}
		}

		private void InternalAdd(IValueManager manager, NativeRow key, NativeRow data)
		{
			_manager = manager;
			try
			{
				_rows.Add(key, data);
			}
			finally
			{
				_manager = null;
			}
		}
		
		private void InternalRemove(IValueManager manager, NativeRow key)
		{
			_manager = manager;
			try
			{
				_rows.Remove(key);
			}
			finally
			{
				_manager = null;
			}
		}

		private NativeRow InternalGet(IValueManager manager, NativeRow key)
		{
			_manager = manager;
			try
			{
				NativeRow result;
				_rows.TryGetValue(key, out result);
				return result;
			}
			finally
			{
				_manager = null;
			}
		}

		public Dictionary<NativeRow, NativeRow>.Enumerator GetEnumerator()
		{
			return _rows.GetEnumerator();
		}

		public int Count()
		{
			return _rows.Count;
		}

		public bool ContainsKey(IValueManager manager, NativeRow key)
		{
			return InternalGet(manager, key) != null;
		}

		public override void Insert(IValueManager manager, NativeRow key, NativeRow data)
		{
			InternalAdd(manager, key, data);
		}

		public override void Update(IValueManager manager, NativeRow oldKey, NativeRow newKey, NativeRow newData)
		{
			var oldData = InternalGet(manager, oldKey);
			if (newData != null)
			{
				DisposeData(manager, oldData);
			}
			else
			{
				newData = oldData;
			}
			DisposeKey(manager, oldKey);
			InternalRemove(manager, oldKey);

			InternalAdd(manager, newKey, newData);
		}

		public override void Delete(IValueManager manager, NativeRow key)
		{
			DisposeData(manager, InternalGet(manager, key));
			DisposeKey(manager, key);
			InternalRemove(manager, key);
		}
	}
}
