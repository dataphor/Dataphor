/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	/// <summary>
	/// Provides a generic list class that supports validation and notification behavior through virtual methods.
	/// </summary>
	/// <remarks>
	/// The base implementation performs no validation or notification.
	/// </remarks>
	public class ValidatingBaseList<T> : IList<T>, IList
	{
		public const int DefaultInitialCapacity = 0;
		public const int MinimumGrowth = 4;
		public const int MaximumGrowth = 512;
		
		public ValidatingBaseList() : base() { }
		public ValidatingBaseList(int capacity) : base()
		{
			#if DEBUG
			if (capacity < 0)
				throw new ArgumentOutOfRangeException("ACapacity", "Capacity must be greater than or equal to zero.");
			#endif
			
			if (capacity > 0)
				SetCapacity(capacity);
		}
		
		private int _count;
		public int Count { get { return _count; } }

		private T[] _items;
		
		public int Capacity 
		{ 
			get { return _items == null ? 0 : _items.Length; }
			set { SetCapacity(value); }
		}
		
		private void SetCapacity(int capacity)
		{
			if (capacity < _count)
				throw new ArgumentException("Capacity cannot be set to a value lower than the number of items currently in the list.");

			if ((_items == null) || (_items.Length <= capacity))
			{
				T[] newItems = new T[capacity];
				if (_items != null)
					Array.Copy(_items, newItems, _items.Length);
				_items = newItems;
			}
		}
		
		private void EnsureCapacity(int requiredCapacity)
		{
			if (Capacity <= requiredCapacity)
			{
				int additionalCapacity = Math.Min(Math.Max(Capacity, MinimumGrowth), MaximumGrowth);
				if (Capacity + additionalCapacity < requiredCapacity)
					Capacity = requiredCapacity;
				else
					Capacity += additionalCapacity;
			}
		}
		
		/// <summary> Validate is called before an item is added or set in a List </summary>
		/// <remarks>
		///		Override and throw an exception in order to perform item validation for
		///		the list.
		/// </remarks>
		protected virtual void Validate(T tempValue) { }
		
		/// <summary> Adding is called when an item is added to the list. </summary>
		/// <remarks>
		///		This should NOT be used for validation, but is a good place to put 
		///		code that interacts with items in the list.  
		///	</remarks>
		protected virtual void Adding(T tempValue, int index) { }
		
		/// <summary> Removing is called when an item is being removed from the List. </summary>
		protected virtual void Removing(T tempValue, int index) { }

		/// <summary> Removed is call <i>after</i> an item as been removed from the list. </summary>
		protected virtual void Removed(T tempValue, int index) { }
		
		/// <summary> Adds an item to the end of the list. </summary>
		public int Add(T tempValue)
		{
			int index = Count;
			Insert(index, tempValue);
			return index;
		}
		
		/// <summary> Adds a collection of items to the list. </summary>
		public void AddRange(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}
		
		/// <summary> Inserts an item into the list. </summary>
		public void Insert(int index, T tempValue)
		{
			Validate(tempValue);
			EnsureCapacity(_count);
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_items[localIndex + 1] = _items[localIndex];
			_items[index] = tempValue;
			_count++;
			Adding(tempValue, index);
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			foreach (T item in items)
				Insert(index++, item);
		}
		
		public void Remove(T tempValue)
		{
		    RemoveAt(IndexOf(tempValue));
		}

		/// <summary>Removes AValue if it is found in the list, does nothing otherwise.</summary>		
		public void SafeRemove(T tempValue)
		{
			int index = IndexOf(tempValue);
			if (index >= 0)
				RemoveAt(index);
		}
		
		public T RemoveAt(int index)
		{
			T item = this[index];
			try
			{
				Removing(item, index);
			}
			finally
			{
				_count--;
				for (int localIndex = index; localIndex < _count; localIndex++)
					_items[localIndex] = _items[localIndex + 1];
				_items[_count] = default(T); // Clear the last slot
			}
			Removed(item, index);
			return item;
		}
		
		public void RemoveRange(int index, int count)
		{
			for (int localIndex = 0; localIndex < count; localIndex++)
				RemoveAt(index);
		}
		
		public virtual void Clear()
		{
			while (Count > 0)
				RemoveAt(Count - 1);
		}
		
		public void SetRange(int index, IEnumerable<T> items)
		{
			foreach (T item in items)
				this[index++] = item;
		}
		
		public T this[int index]
		{
			get 
			{ 
				#if DEBUG
				if ((index < 0) || (index >= Count))
					throw new IndexOutOfRangeException();
				#endif
				return _items[index]; 
			}
			set
			{
				#if DEBUG
				if ((index < 0) || (index >= Count))
					throw new IndexOutOfRangeException();
				#endif
				T item = this[index];
				Removing(item, index);
				Validate(value);
				_items[index] = value;
				Removed(item, index);
				Adding(value, index);
			}
		}
		
		public virtual int IndexOf(T item)
		{
			for (int index = 0; index < _count; index++)
				if (Object.Equals(this[index], item))
					return index;
					
			return -1;
		}
		
		public bool Contains(T item)
		{
			return IndexOf(item) >= 0;
		}
		
		public void CopyTo(int sourceIndex, T[] array, int arrayIndex, int count)
		{
			if (_items != null)
				Array.Copy(_items, sourceIndex, array, arrayIndex, count);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(0, array, arrayIndex, _count);
		}
		
		public void CopyTo(T[] array)
		{
			CopyTo(array, 0);
		}

		#region IList<T> Members

		int IList<T>.IndexOf(T item)
		{
			return IndexOf(item);
		}

		void IList<T>.Insert(int index, T item)
		{
			Insert(index, item);
		}

		void IList<T>.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		T IList<T>.this[int index]
		{
			get { return this[index]; }
			set { this[index] = value; }
		}

		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		void ICollection<T>.Clear()
		{
			Clear();
		}

		bool ICollection<T>.Contains(T item)
		{
			return Contains(item);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			CopyTo(array, arrayIndex);
		}

		int ICollection<T>.Count
		{
			get { return Count; }
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<T>.Remove(T item)
		{
			int index = IndexOf(item);
			if (index >= 0)
			{
				RemoveAt(index);
				return true;
			}
			
			return false;
		}

		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return new BaseListEnumerator(this);
		}

        public class BaseListEnumerator : IEnumerator<T>
        {
            public BaseListEnumerator(ValidatingBaseList<T> list) : base()
            {
                _list = list;
            }
            
            private int _current = -1;
            private ValidatingBaseList<T> _list;

            public T Current { get { return _list[_current]; } }
            
            object IEnumerator.Current { get { return _list[_current]; } }

            public void Reset()
            {
                _current = -1;
            }

            public bool MoveNext()
            {
				_current++;
				return _current < _list.Count;
            }
            
            public void Dispose()
            {
				// nothing to do
            }
        }
        
		/// <summary> Changes the index of an item. </summary>
		public virtual void Move(int oldIndex, int newIndex)
		{
			Insert(newIndex, RemoveAt(oldIndex));
		}
		
		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

		#endregion

		#region IList Members

		int IList.Add(object tempValue)
		{
			return Add((T)tempValue);
		}

		void IList.Clear()
		{
			Clear();
		}

		bool IList.Contains(object tempValue)
		{
			return Contains((T)tempValue);
		}

		int IList.IndexOf(object tempValue)
		{
			return IndexOf((T)tempValue);
		}

		void IList.Insert(int index, object tempValue)
		{
			Insert(index, (T)tempValue);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		void IList.Remove(object tempValue)
		{
			Remove((T)tempValue);
		}

		void IList.RemoveAt(int index)
		{
			RemoveAt(index);
		}

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (T)value; }
		}

		#endregion

		#region ICollection Members

		void ICollection.CopyTo(Array array, int index)
		{
			((IList<T>)this).CopyTo((T[])array, index);
		}

		int ICollection.Count
		{
			get { return Count; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}
