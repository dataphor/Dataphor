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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;

namespace Alphora.Dataphor.DAE.Server
{
	public class RemoteServerObject : MarshalByRefObject, IDisposableNotify
	{
		public const int LeaseManagerPollTimeSeconds = 60;

		static RemoteServerObject()
		{
			LifetimeServices.LeaseManagerPollTime = TimeSpan.FromSeconds(LeaseManagerPollTimeSeconds);
			LifetimeServices.LeaseTime = TimeSpan.Zero; // default lease to infinity (overridden for session)
		}

		protected virtual void Dispose(bool disposing)
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			try
			{
				DoDispose();
			}
			finally
			{
				ILease lease = (ILease)GetLifetimeService();
				if (lease != null)
				{
					// Calling cancel on the lease will invoke disconnect on the lease and the server object
					lease.GetType().GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lease, null);
				}
				else
					RemotingServices.Disconnect(this);
			}
		}

		#if USEFINALIZER
		~RemoteServerObject()
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
	
	public class RemoteServerChildObject : MarshalByRefObject, IDisposableNotify
	{
		#if USEFINALIZER
		~RemoteServerChildObject()
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
			try
			{
				DoDispose();
			}
			finally
			{
				ILease lease = (ILease)GetLifetimeService();
				if (lease != null)
					lease.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(lease, null);
				RemotingServices.Disconnect(this);
			}
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

	[Serializable]	
	public class RemoteServerChildObjects : Disposable, IList
	{
		private const int DefaultInitialCapacity = 8;
		private const int DefaultRolloverCount = 20;
		        
		private RemoteServerChildObject[] _items;
		private int _count;
		private bool _isOwner = true;
		
		public RemoteServerChildObjects() : base()
		{
			_items = new RemoteServerChildObject[DefaultInitialCapacity];
		}
		
		public RemoteServerChildObjects(bool isOwner)
		{
			_items = new RemoteServerChildObject[DefaultInitialCapacity];
			_isOwner = isOwner;
		}
		
		public RemoteServerChildObjects(int initialCapacity) : base()
		{
			_items = new RemoteServerChildObject[initialCapacity];
		}
		
		public RemoteServerChildObjects(int initialCapacity, bool isOwner) : base()
		{
			_items = new RemoteServerChildObject[initialCapacity];
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
		
		public RemoteServerChildObject this[int index] 
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
		
		protected int InternalIndexOf(RemoteServerChildObject item)
		{
			for (int index = 0; index < _count; index++)
				if (_items[index] == item)
					return index;
			return -1;
		}
		
		public int IndexOf(RemoteServerChildObject item)
		{
			lock (this)
			{
				return InternalIndexOf(item);
			}
		}
		
		public bool Contains(RemoteServerChildObject item)
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
			RemoteServerChildObject objectValue;
			if (item is RemoteServerChildObject)
				objectValue = (RemoteServerChildObject)item;
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
				RemoteServerChildObject[] newItems = new RemoteServerChildObject[tempValue];
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
		
		public void Remove(RemoteServerChildObject tempValue)
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
		
		public RemoteServerChildObject Disown(RemoteServerChildObject tempValue)
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

		public RemoteServerChildObject SafeDisown(RemoteServerChildObject tempValue)
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

		public RemoteServerChildObject DisownAt(int index)
		{
			lock (this)
			{
				RemoteServerChildObject tempValue = _items[index];
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

		protected virtual void ObjectDispose(object sender, EventArgs args)
		{
			Disown((RemoteServerChildObject)sender);
		}
		
		protected virtual void Validate(RemoteServerChildObject objectValue)
		{
		}
		
		protected virtual void Adding(RemoteServerChildObject objectValue, int index)
		{
			objectValue.Disposed += new EventHandler(ObjectDispose);
		}
		
		protected virtual void Removing(RemoteServerChildObject objectValue, int index)
		{
			objectValue.Disposed -= new EventHandler(ObjectDispose);

			if (_isOwner && !_disowning)
				objectValue.Dispose();
		}

		// IList
		object IList.this[int index] { get { return this[index]; } set { this[index] = (RemoteServerChildObject)value; } }
		int IList.IndexOf(object item) { return (item is RemoteServerChildObject) ? IndexOf((RemoteServerChildObject)item) : -1; }
		bool IList.Contains(object item) { return (item is RemoteServerChildObject) ? Contains((RemoteServerChildObject)item) : false; }
		void IList.Remove(object item) { RemoveAt(IndexOf((RemoteServerChildObject)item)); }
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
		public RemoteServerChildObjectEnumerator GetEnumerator()
		{
			return new RemoteServerChildObjectEnumerator(this);
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public class RemoteServerChildObjectEnumerator : IEnumerator
		{
			public RemoteServerChildObjectEnumerator(RemoteServerChildObjects objects) : base()
			{
				_objects = objects;
			}
			
			private RemoteServerChildObjects _objects;
			private int _current =  -1;

			public RemoteServerChildObject Current { get { return _objects[_current]; } }
			
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
