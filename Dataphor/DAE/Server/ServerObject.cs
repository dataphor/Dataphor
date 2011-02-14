/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerObject : IDisposableNotify
	{
		protected virtual void Dispose(bool disposing)
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			DoDispose();
		}

		#if USEFINALIZER
		~ServerObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
        
		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}
	
	public class ServerChildObject : IDisposableNotify
	{
		#if USEFINALIZER
		~ServerChildObject()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
        
		protected virtual void Dispose(bool disposing)
		{
			DoDispose();
		}
		
		public void Dispose()
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		public event EventHandler Disposed;
		protected void DoDispose()
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}
	}

	public class ServerChildObjects : Disposable, IList
	{
		private const int DefaultInitialCapacity = 8;
		private const int DefaultRolloverCount = 20;
		        
		private ServerChildObject[] _items;
		private int _count;
		private bool _isOwner = true;
		
		public ServerChildObjects() : base()
		{
			_items = new ServerChildObject[DefaultInitialCapacity];
		}
		
		public ServerChildObjects(bool isOwner)
		{
			_items = new ServerChildObject[DefaultInitialCapacity];
			_isOwner = isOwner;
		}
		
		public ServerChildObjects(int initialCapacity) : base()
		{
			_items = new ServerChildObject[initialCapacity];
		}
		
		public ServerChildObjects(int initialCapacity, bool isOwner) : base()
		{
			_items = new ServerChildObject[initialCapacity];
			_isOwner = isOwner;
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				Clear();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		public ServerChildObject this[int index] 
		{ 
			get
			{ 
				return _items[index];
			} 
			set
			{ 
				lock (this)
				{
					InternalRemoveAt(index);
					InternalInsert(index, value);
				}
			} 
		}
		
		protected int InternalIndexOf(ServerChildObject item)
		{
			for (int index = 0; index < _count; index++)
				if (_items[index] == item)
					return index;
			return -1;
		}
		
		public int IndexOf(ServerChildObject item)
		{
			lock (this)
			{
				return InternalIndexOf(item);
			}
		}
		
		public bool Contains(ServerChildObject item)
		{
			return IndexOf(item) >= 0;
		}
		
		public int Add(object item)
		{
			lock (this)
			{
				int index = _count;
				InternalInsert(index, item);
				return index;
			}
		}
		
		public void AddRange(ICollection collection)
		{
			foreach (object AObject in collection)
				Add(AObject);
		}
		
		private void InternalInsert(int index, object item)
		{
			ServerChildObject objectValue;
			if (item is ServerChildObject)
				objectValue = (ServerChildObject)item;
			else
				throw new ServerException(ServerException.Codes.ObjectContainer);
				
			Validate(objectValue);
				
			if (_count >= _items.Length)
				Capacity *= 2;
			for (int localIndex = _count - 1; localIndex >= index; localIndex--)
				_items[localIndex + 1] = _items[localIndex];
			_items[index] = objectValue;
			_count++;

			Adding(objectValue, index);
		}
		
		public void Insert(int index, object item)
		{
			lock (this)
			{
				InternalInsert(index, item);
			}
		}
		
		private void InternalSetCapacity(int tempValue)
		{
			if (_items.Length != tempValue)
			{
				ServerChildObject[] newItems = new ServerChildObject[tempValue];
				for (int index = 0; index < ((_count > tempValue) ? tempValue : _count); index++)
					newItems[index] = _items[index];

				if (_count > tempValue)						
					for (int index = _count - 1; index > tempValue; index--)
						InternalRemoveAt(index);
						
				_items = newItems;
			}
		}
		
		public int Capacity
		{
			get { return _items.Length; }
			set
			{
				lock (this)
				{
					InternalSetCapacity(value);
				}
			}
		}
		
		private void InternalRemoveAt(int index)
		{
			Removing(_items[index], index);

			_count--;			
			for (int localIndex = index; localIndex < _count; localIndex++)
				_items[localIndex] = _items[localIndex + 1];
			_items[_count] = null; // This clear must occur or the reference is still live 
		}
		
		public void RemoveAt(int index)
		{
			lock (this)
			{
				InternalRemoveAt(index);
			}
		}
		
		public void Remove(ServerChildObject tempValue)
		{
			lock (this)
			{
				InternalRemoveAt(InternalIndexOf(tempValue));
			}
		}
		
		public void Clear()
		{
			lock (this)
			{
				while (_count > 0)
					InternalRemoveAt(_count - 1);
			}
		}
		
		protected bool _disowning;
		
		public ServerChildObject Disown(ServerChildObject tempValue)
		{
			lock (this)
			{
				_disowning = true;
				try
				{
					InternalRemoveAt(InternalIndexOf(tempValue));
					return tempValue;
				}
				finally
				{
					_disowning = false;
				}
			}
		}

		public ServerChildObject SafeDisown(ServerChildObject tempValue)
		{
			lock (this)
			{
				_disowning = true;
				try
				{
					int index = InternalIndexOf(tempValue);
					if (index >= 0)
						InternalRemoveAt(index);
					return tempValue;
				}
				finally
				{
					_disowning = false;
				}
			}
		}

		public ServerChildObject DisownAt(int index)
		{
			lock (this)
			{
				ServerChildObject tempValue = _items[index];
				_disowning = true;
				try
				{
					InternalRemoveAt(index);
					return tempValue;
				}
				finally
				{
					_disowning = false;
				}
			}
		}

		public void DisownAll()
		{
			lock (this)
			{
				_disowning = true;
				try
				{
					while (_count > 0)
						InternalRemoveAt(_count - 1);
				}
				finally
				{
					_disowning = false;
				}
			}
		}

		protected virtual void ObjectDispose(object sender, EventArgs args)
		{
			Disown((ServerChildObject)sender);
		}
		
		protected virtual void Validate(ServerChildObject objectValue)
		{
		}
		
		protected virtual void Adding(ServerChildObject objectValue, int index)
		{
			objectValue.Disposed += new EventHandler(ObjectDispose);
		}
		
		protected virtual void Removing(ServerChildObject objectValue, int index)
		{
			objectValue.Disposed -= new EventHandler(ObjectDispose);

			if (_isOwner && !_disowning)
				objectValue.Dispose();
		}

		// IList
		object IList.this[int index] { get { return this[index]; } set { this[index] = (ServerChildObject)value; } }
		int IList.IndexOf(object item) { return (item is ServerChildObject) ? IndexOf((ServerChildObject)item) : -1; }
		bool IList.Contains(object item) { return (item is ServerChildObject) ? Contains((ServerChildObject)item) : false; }
		void IList.Remove(object item) { RemoveAt(IndexOf((ServerChildObject)item)); }
		bool IList.IsFixedSize { get { return false; } }
		bool IList.IsReadOnly { get { return false; } }
		
		// ICollection
		public int Count { get { return _count; } }
		public bool IsSynchronized { get { return true; } }
		public object SyncRoot { get { return this; } }
		public void CopyTo(Array array, int index)
		{
			lock (this)
			{
				IList localArray = (IList)array;
				for (int localIndex = 0; localIndex < Count; localIndex++)
					localArray[index + localIndex] = this[localIndex];
			}
		}

		// IEnumerable
		public ServerChildObjectEnumerator GetEnumerator()
		{
			return new ServerChildObjectEnumerator(this);
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public class ServerChildObjectEnumerator : IEnumerator
		{
			public ServerChildObjectEnumerator(ServerChildObjects objects) : base()
			{
				_objects = objects;
			}
			
			private ServerChildObjects _objects;
			private int _current =  -1;

			public ServerChildObject Current { get { return _objects[_current]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				_current++;
				return (_current < _objects.Count);
			}
			
			public void Reset()
			{
				_current = -1;
			}
		}
	}
}
