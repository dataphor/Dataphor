/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerObject : MarshalByRefObject, IDisposableNotify
	{
		public const int CLeaseManagerPollTimeSeconds = 60;

		static ServerObject()
		{
			LifetimeServices.LeaseManagerPollTime = TimeSpan.FromSeconds(CLeaseManagerPollTimeSeconds);
			LifetimeServices.LeaseTime = TimeSpan.Zero; // default lease to infinity (overridden for session)
		}

		protected virtual void Dispose(bool ADisposing)
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
				ILease LLease = (ILease)GetLifetimeService();
				if (LLease != null)
				{
					// Calling cancel on the lease will invoke disconnect on the lease and the server object
					LLease.GetType().GetMethod("Cancel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(LLease, null);
				}
				else
					RemotingServices.Disconnect(this);
			}
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
	
	public class ServerChildObject : MarshalByRefObject, IDisposableNotify
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
        
		protected virtual void Dispose(bool ADisposing)
		{
			try
			{
				DoDispose();
			}
			finally
			{
				ILease LLease = (ILease)GetLifetimeService();
				if (LLease != null)
					LLease.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(LLease, null);
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
	public class ServerChildObjects : Disposable, IList
	{
		private const int CDefaultInitialCapacity = 8;
		private const int CDefaultRolloverCount = 20;
		        
		private ServerChildObject[] FItems;
		private int FCount;
		private bool FIsOwner = true;
		
		public ServerChildObjects() : base()
		{
			FItems = new ServerChildObject[CDefaultInitialCapacity];
		}
		
		public ServerChildObjects(bool AIsOwner)
		{
			FItems = new ServerChildObject[CDefaultInitialCapacity];
			FIsOwner = AIsOwner;
		}
		
		public ServerChildObjects(int AInitialCapacity) : base()
		{
			FItems = new ServerChildObject[AInitialCapacity];
		}
		
		public ServerChildObjects(int AInitialCapacity, bool AIsOwner) : base()
		{
			FItems = new ServerChildObject[AInitialCapacity];
			FIsOwner = AIsOwner;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				Clear();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public ServerChildObject this[int AIndex] 
		{ 
			get
			{ 
				return FItems[AIndex];
			} 
			set
			{ 
				lock (this)
				{
					InternalRemoveAt(AIndex);
					InternalInsert(AIndex, value);
				}
			} 
		}
		
		protected int InternalIndexOf(ServerChildObject AItem)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (FItems[LIndex] == AItem)
					return LIndex;
			return -1;
		}
		
		public int IndexOf(ServerChildObject AItem)
		{
			lock (this)
			{
				return InternalIndexOf(AItem);
			}
		}
		
		public bool Contains(ServerChildObject AItem)
		{
			return IndexOf(AItem) >= 0;
		}
		
		public int Add(object AItem)
		{
			lock (this)
			{
				int LIndex = FCount;
				InternalInsert(LIndex, AItem);
				return LIndex;
			}
		}
		
		public void AddRange(ICollection ACollection)
		{
			foreach (object AObject in ACollection)
				Add(AObject);
		}
		
		private void InternalInsert(int AIndex, object AItem)
		{
			ServerChildObject LObject;
			if (AItem is ServerChildObject)
				LObject = (ServerChildObject)AItem;
			else
				throw new ServerException(ServerException.Codes.ObjectContainer);
				
			Validate(LObject);
				
			if (FCount >= FItems.Length)
				Capacity *= 2;
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = LObject;
			FCount++;

			Adding(LObject, AIndex);
		}
		
		public void Insert(int AIndex, object AItem)
		{
			lock (this)
			{
				InternalInsert(AIndex, AItem);
			}
		}
		
		private void InternalSetCapacity(int AValue)
		{
			if (FItems.Length != AValue)
			{
				ServerChildObject[] LNewItems = new ServerChildObject[AValue];
				for (int LIndex = 0; LIndex < ((FCount > AValue) ? AValue : FCount); LIndex++)
					LNewItems[LIndex] = FItems[LIndex];

				if (FCount > AValue)						
					for (int LIndex = FCount - 1; LIndex > AValue; LIndex--)
						InternalRemoveAt(LIndex);
						
				FItems = LNewItems;
			}
		}
		
		public int Capacity
		{
			get { return FItems.Length; }
			set
			{
				lock (this)
				{
					InternalSetCapacity(value);
				}
			}
		}
		
		private void InternalRemoveAt(int AIndex)
		{
			Removing(FItems[AIndex], AIndex);

			FCount--;			
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			FItems[FCount] = null; // This clear must occur or the reference is still live 
		}
		
		public void RemoveAt(int AIndex)
		{
			lock (this)
			{
				InternalRemoveAt(AIndex);
			}
		}
		
		public void Remove(ServerChildObject AValue)
		{
			lock (this)
			{
				InternalRemoveAt(InternalIndexOf(AValue));
			}
		}
		
		public void Clear()
		{
			lock (this)
			{
				while (FCount > 0)
					InternalRemoveAt(FCount - 1);
			}
		}
		
		protected bool FDisowning;
		
		public ServerChildObject Disown(ServerChildObject AValue)
		{
			lock (this)
			{
				FDisowning = true;
				try
				{
					InternalRemoveAt(InternalIndexOf(AValue));
					return AValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		public ServerChildObject SafeDisown(ServerChildObject AValue)
		{
			lock (this)
			{
				FDisowning = true;
				try
				{
					int LIndex = InternalIndexOf(AValue);
					if (LIndex >= 0)
						InternalRemoveAt(LIndex);
					return AValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		public ServerChildObject DisownAt(int AIndex)
		{
			lock (this)
			{
				ServerChildObject LValue = FItems[AIndex];
				FDisowning = true;
				try
				{
					InternalRemoveAt(AIndex);
					return LValue;
				}
				finally
				{
					FDisowning = false;
				}
			}
		}

		protected virtual void ObjectDispose(object ASender, EventArgs AArgs)
		{
			Disown((ServerChildObject)ASender);
		}
		
		protected virtual void Validate(ServerChildObject AObject)
		{
		}
		
		protected virtual void Adding(ServerChildObject AObject, int AIndex)
		{
			AObject.Disposed += new EventHandler(ObjectDispose);
		}
		
		protected virtual void Removing(ServerChildObject AObject, int AIndex)
		{
			AObject.Disposed -= new EventHandler(ObjectDispose);

			if (FIsOwner && !FDisowning)
				AObject.Dispose();
		}

		// IList
		object IList.this[int AIndex] { get { return this[AIndex]; } set { this[AIndex] = (ServerChildObject)value; } }
		int IList.IndexOf(object AItem) { return (AItem is ServerChildObject) ? IndexOf((ServerChildObject)AItem) : -1; }
		bool IList.Contains(object AItem) { return (AItem is ServerChildObject) ? Contains((ServerChildObject)AItem) : false; }
		void IList.Remove(object AItem) { RemoveAt(IndexOf((ServerChildObject)AItem)); }
		bool IList.IsFixedSize { get { return false; } }
		bool IList.IsReadOnly { get { return false; } }
		
		// ICollection
		public int Count { get { return FCount; } }
		public bool IsSynchronized { get { return true; } }
		public object SyncRoot { get { return this; } }
		public void CopyTo(Array AArray, int AIndex)
		{
			lock (this)
			{
				IList LArray = (IList)AArray;
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LArray[AIndex + LIndex] = this[LIndex];
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
			public ServerChildObjectEnumerator(ServerChildObjects AObjects) : base()
			{
				FObjects = AObjects;
			}
			
			private ServerChildObjects FObjects;
			private int FCurrent =  -1;

			public ServerChildObject Current { get { return FObjects[FCurrent]; } }
			
			object IEnumerator.Current { get { return Current; } }
			
			public bool MoveNext()
			{
				FCurrent++;
				return (FCurrent < FObjects.Count);
			}
			
			public void Reset()
			{
				FCurrent = -1;
			}
		}
	}
}
