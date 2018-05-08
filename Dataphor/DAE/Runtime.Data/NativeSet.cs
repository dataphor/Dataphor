/*
	Dataphor
	© Copyright 2000-2018 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class NativeSet : System.Object
	{
		public NativeSet(Schema.IListType listType, Schema.Sort equalitySort) : base()
		{
			_listType = listType;
			_equalitySort = equalitySort;
			_equalityComparer = new NativeSetEqualityComparer(this);
			_elements = new HashSet<object>(_equalityComparer);
		}

		private Schema.IListType _listType;
		public Schema.IListType ListType { get { return _listType; } }

		private Schema.Sort _equalitySort;
		public Schema.Sort EqualitySort { get { return _equalitySort; } }

		private class NativeSetEqualityComparer : IEqualityComparer<object>
		{
			public NativeSetEqualityComparer(NativeSet nativeSet)
			{
				_nativeSet = nativeSet;
			}

			private NativeSet _nativeSet;

			public new bool Equals(object left, object right)
			{
				return _nativeSet.CompareEqual(_nativeSet._manager, left, right);
			}

			public int GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
		}

		private NativeSetEqualityComparer _equalityComparer;
		private HashSet<object> _elements;
		
		internal IValueManager _manager; // Only used during the Add and Remove calls
		
		public bool CompareEqual(IValueManager manager, object index, object compare)
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

			return manager.EvaluateEqualitySort(_equalitySort, index, compare);

			//using (var indexValue = DataValue.FromNative(manager, _listType.ElementType, index))
			//{
			//	using (var compareValue = DataValue.FromNative(manager, _listType.ElementType, compare))
			//	{
			//		return manager.EvaluateEqualitySort(_equalitySort, indexValue, compareValue);
			//	}
			//}
		}
		
		public void Drop(IValueManager manager)
		{
			foreach (var element in _elements)
			{
				DisposeValue(manager, element);
			}
		}

		private void InternalAdd(IValueManager manager, object value)
		{
			_manager = manager;
			try
			{
				_elements.Add(value);
			}
			finally
			{
				_manager = null;
			}
		}
		
		private void InternalRemove(IValueManager manager, object value)
		{
			_manager = manager;
			try
			{
				_elements.Remove(value);
			}
			finally
			{
				_manager = null;
			}
		}

		private object InternalGet(IValueManager manager, object value)
		{
			_manager = manager;
			try
			{
				if (_elements.Contains(value))
				{
					return value;
				}

				return null;
			}
			finally
			{
				_manager = null;
			}
		}

		public HashSet<object>.Enumerator GetEnumerator()
		{
			return _elements.GetEnumerator();
		}

		public int Count()
		{
			return _elements.Count;
		}

		public bool Contains(IValueManager manager, object value)
		{
			return InternalGet(manager, value) != null;
		}

		public void Insert(IValueManager manager, object value)
		{
			InternalAdd(manager, value);
		}

		public void Update(IValueManager manager, object oldValue, object newValue)
		{
			DisposeValue(manager, oldValue);
			InternalRemove(manager, oldValue);

			if (newValue != null)
			{
				InternalAdd(manager, newValue);
			}
		}

		public void Delete(IValueManager manager, object value)
		{
			DisposeValue(manager, value);
			InternalRemove(manager, value);
		}

		public void DisposeValue(IValueManager manager, object value)
		{
			DataValue.DisposeNative(manager, _listType.ElementType, value);
		}
	}
}
