/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	public class BaseList<T> : IList<T>, IList
	{
		public const int DefaultInitialCapacity = 0;
		public const int MinimumGrowth = 4;
		public const int MaximumGrowth = 512;
		
		public BaseList() : base() { }
		public BaseList(int capacity) : base()
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
		
		/// <summary> Adds an item to the end of the list. </summary>
		public int Add(T value)
		{
			int index = Count;
			Insert(index, value);
			return index;
		}
		
		/// <summary> Adds a collection of items to the list. </summary>
		public void AddRange(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}
		
		/// <summary> Inserts an item into the list. </summary>
		public void Insert(int index, T value)
		{
			EnsureCapacity(_count);
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_items[localIndex + 1] = _items[localIndex];
			_items[index] = value;
			_count++;
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			foreach (T item in items)
				Insert(index++, item);
		}
		
		public void Remove(T value)
		{
		    RemoveAt(IndexOf(value));
		}

		/// <summary>Removes AValue if it is found in the list, does nothing otherwise.</summary>		
		public void SafeRemove(T value)
		{
			int index = IndexOf(value);
			if (index >= 0)
				RemoveAt(index);
		}
		
		public T RemoveAt(int index)
		{
			T item = this[index];
			_count--;
			for (int localIndex = index; localIndex < _count; localIndex++)
				_items[localIndex] = _items[localIndex + 1];
			_items[_count] = default(T); // Clear the last slot
			return item;
		}
		
		public void RemoveRange(int index, int count)
		{
			for (int localIndex = 0; localIndex < count; localIndex++)
				RemoveAt(index);
		}
		
		public void Clear()
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
				_items[index] = value;
			}
		}
		
		public int IndexOf(T item)
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

		/// <summary> Changes the index of an item. </summary>
		public void Move(int oldIndex, int newIndex)
		{
			Insert(newIndex, RemoveAt(oldIndex));
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
            public BaseListEnumerator(BaseList<T> list) : base()
            {
                _list = list;
            }
            
            private int _current = -1;
            private BaseList<T> _list;

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
        
		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}

		#endregion

		#region IList Members

		int IList.Add(object value)
		{
			return Add((T)value);
		}

		void IList.Clear()
		{
			Clear();
		}

		bool IList.Contains(object value)
		{
			return Contains((T)value);
		}

		int IList.IndexOf(object value)
		{
			return IndexOf((T)value);
		}

		void IList.Insert(int index, object value)
		{
			Insert(index, (T)value);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		void IList.Remove(object value)
		{
			Remove((T)value);
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
	
	/// <summary>
	///		<c>List</c> of objects and provides internal null checking.
	///	</summary>
	/// <remarks>
	///		<c>List</c> can optionally disallow null items.
	///	</remarks>
	public class List : ValidatingBaseList<object>
	{
		public List() : base() {}
		
		public List(bool allowNulls) : base(4)
		{
			_allowNulls = allowNulls;
		}

		private bool _allowNulls;
		/// <summary> <c>AllowNulls</c> determines whether the list can contain null references. </summary>
		/// <remarks>
		///		If this property is changes while items are in the list, the items 
		///		are validated.
		/// </remarks>
		public bool AllowNulls
		{
			get { return _allowNulls; }
			set
			{
				if (_allowNulls != value)
				{
					if (!value)
						foreach(object item in this)
							InternalValidate(item, false);
					_allowNulls = value;
				}
			}
		}

		private void InternalValidate(object value, bool allowNulls)
		{
			if (!allowNulls && (value == null))
				throw new BaseException(BaseException.Codes.CannotAddNull);
		}
		
		/// <summary> Overrides the validation to check the nullability of the item. </summary>
		protected override void Validate(object value)
		{
			InternalValidate(value, AllowNulls);
			base.Validate(value);
		}
	}

	public class DisposableList : List, IDisposable
	{
		public DisposableList() : base(){}
		
		/// <summary> Allows the initialization of the ItemsOwned and AllowNulls properties. </summary>
		/// <param name="itemsOwned"> See <see cref="DisposableList.ItemsOwned"/> </param>
		/// <param name="allowNulls"> See <see cref="List.AllowNulls"/> </param>
		public DisposableList(bool itemsOwned, bool allowNulls) : base(allowNulls)
		{
			_itemsOwned = itemsOwned;
		}
		
		protected bool _itemsOwned = true;
		/// <summary>
		///		ItemsOwned controls whether or not the List "owns" the contained items.  
		///		"Owns" means that if the item supports the IDisposable interface, the will be
		///		disposed when the list is disposed or when an item is removed.  
		///	</summary>
		public bool ItemsOwned
		{
			get { return _itemsOwned; }
			set { _itemsOwned = value; }
		}

		/// <summary> <c>ItemDispose</c> is called by contained items when they are disposed. </summary>
		/// <remarks>
		///		This method simply removes the item from the list.  <c>ItemDispose</c> is 
		///		only called if the item is not disposed by this list.
		///	</remarks>
		protected virtual void ItemDispose(object sender, EventArgs args)
		{
			Disown(sender);
			//Remove(ASender);
		}
		
		///	<remarks> Hooks the Disposed event of the item if the item implements IDisposable. </remarks>
		protected override void Adding(object value, int index)
		{
			if (value is IDisposableNotify)
				((IDisposableNotify)value).Disposed += new EventHandler(ItemDispose);
		}

		/// <remarks> If the item is owned, it is disposed. </remarks>
		protected override void Removing(object value, int index)
		{
			if (value is IDisposableNotify)
        	    ((IDisposableNotify)value).Disposed -= new EventHandler(ItemDispose);

        	if (_itemsOwned && !(_disowning) && (value is IDisposable))
		        ((IDisposable)value).Dispose();
		}

		public override void Move(int oldIndex, int newIndex)
		{
			_disowning = true;
			try
			{
				base.Move(oldIndex, newIndex);
			}
			finally
			{
				_disowning = false;
			}
		}
		
		/// <summary> IDisposable implementation </summary>
		public event EventHandler Disposed;

		/// <summary> IDisposable implementation </summary>
		public void Dispose()
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			_disposed = true;
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);

			Exception exception = null;
			while (Count > 0)
				try
				{
					RemoveAt(0);
				}
				catch (Exception E)
				{
					exception = E;
				}
				
			if (exception != null)
				throw exception;
		}

		#if USEFINALIZER
		~DisposableList()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose();
			#endif
		}
		#endif

		protected bool _disposed;
		public bool IsDisposed { get { return _disposed; } }

		protected bool _disowning;
		
		/// <summary> Removes the specified object without disposing it. </summary>
		public virtual object Disown(object value)
		{
			_disowning = true;
			try
			{
				Remove(value);
				return value;
			}
			finally
			{
				_disowning = false;
			}
		}
		
		/// <summary> Removes the specified object index without disposing it. </summary>
		public virtual object DisownAt(int index)
		{
			object value = this[index];
			_disowning = true;
			try
			{
				RemoveAt(index);
				return value;
			}
			finally
			{
				_disowning = false;
			}
		}
	}

	/// <summary> A Dictionary descendent which implements the IList interface. </summary>
	public abstract class HashtableList<TKey, TValue> : Dictionary<TKey, TValue>, IList
	{
		public HashtableList() : base() { }
		public HashtableList(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		public HashtableList(int capacity) : base(capacity) { }
		public HashtableList(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public HashtableList(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }

		public abstract int Add(object value);

		public TValue this[int index]
		{
			get
			{
				int localIndex = 0;
				foreach (TKey objectValue in Keys)
				{
					if (localIndex == index)
						return base[objectValue];
					localIndex++;
				}
				throw new BaseException(BaseException.Codes.ObjectAtIndexNotFound, index.ToString());
			}
			set
			{
				throw new BaseException(BaseException.Codes.InsertNotSupported);
			}
		}
		
		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (TValue)value; }
		}

		public int IndexOf(object value)
		{
			int index = 0;
			foreach (TKey objectValue in Keys)
			{
				if (objectValue.Equals(value))
					return index;
				index++;
			}
			return -1;
		}

		public void Insert(int index, object value)
		{
			throw new BaseException(BaseException.Codes.InsertNotSupported);
		}

		public void RemoveAt(int index)
		{
			Remove(this[index]);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		public IDictionaryEnumerator GetDictionaryEnumerator()
		{
			return base.GetEnumerator();
		}

		public bool Contains(object value)
		{
			return ContainsKey((TKey)value);
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Remove(object value)
		{
			base.Remove((TKey)value);
		}
	}
}
