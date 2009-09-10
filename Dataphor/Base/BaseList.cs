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
		public const int CDefaultInitialCapacity = 0;
		public const int CMinimumGrowth = 4;
		public const int CMaximumGrowth = 512;
		
		public BaseList() : base() { }
		public BaseList(int ACapacity) : base()
		{
			#if DEBUG
			if (ACapacity < 0)
				throw new ArgumentOutOfRangeException("ACapacity", "Capacity must be greater than or equal to zero.");
			#endif
			
			if (ACapacity > 0)
				SetCapacity(ACapacity);
		}
		
		private int FCount;
		public int Count { get { return FCount; } }

		private T[] FItems;
		
		public int Capacity 
		{ 
			get { return FItems == null ? 0 : FItems.Length; }
			set { SetCapacity(value); }
		}
		
		private void SetCapacity(int ACapacity)
		{
			if (ACapacity < FCount)
				throw new ArgumentException("Capacity cannot be set to a value lower than the number of items currently in the list.");

			if ((FItems == null) || (FItems.Length <= ACapacity))
			{
				T[] LNewItems = new T[ACapacity];
				if (FItems != null)
					Array.Copy(FItems, LNewItems, FItems.Length);
				FItems = LNewItems;
			}
		}
		
		private void EnsureCapacity(int ARequiredCapacity)
		{
			if (Capacity <= ARequiredCapacity)
			{
				int LAdditionalCapacity = Math.Min(Math.Max(Capacity, CMinimumGrowth), CMaximumGrowth);
				if (Capacity + LAdditionalCapacity < ARequiredCapacity)
					Capacity = ARequiredCapacity;
				else
					Capacity += LAdditionalCapacity;
			}
		}
		
		/// <summary> Adds an item to the end of the list. </summary>
		public int Add(T AValue)
		{
			int LIndex = Count;
			Insert(LIndex, AValue);
			return LIndex;
		}
		
		/// <summary> Adds a collection of items to the list. </summary>
		public void AddRange(IEnumerable<T> AItems)
		{
			foreach (T LItem in AItems)
				Add(LItem);
		}
		
		/// <summary> Inserts an item into the list. </summary>
		public void Insert(int AIndex, T AValue)
		{
			EnsureCapacity(FCount);
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = AValue;
			FCount++;
		}

		public void InsertRange(int AIndex, IEnumerable<T> AItems)
		{
			foreach (T LItem in AItems)
				Insert(AIndex++, LItem);
		}
		
		public void Remove(T AValue)
		{
		    RemoveAt(IndexOf(AValue));
		}

		/// <summary>Removes AValue if it is found in the list, does nothing otherwise.</summary>		
		public void SafeRemove(T AValue)
		{
			int LIndex = IndexOf(AValue);
			if (LIndex >= 0)
				RemoveAt(LIndex);
		}
		
		public T RemoveAt(int AIndex)
		{
			T LItem = this[AIndex];
			FCount--;
			for (int LIndex = AIndex; LIndex < FCount; LIndex++)
				FItems[LIndex] = FItems[LIndex + 1];
			FItems[FCount] = default(T); // Clear the last slot
			return LItem;
		}
		
		public void RemoveRange(int AIndex, int ACount)
		{
			for (int LIndex = 0; LIndex < ACount; LIndex++)
				RemoveAt(AIndex);
		}
		
		public void Clear()
		{
			while (Count > 0)
				RemoveAt(Count - 1);
		}
		
		public void SetRange(int AIndex, IEnumerable<T> AItems)
		{
			foreach (T LItem in AItems)
				this[AIndex++] = LItem;
		}
		
		public T this[int AIndex]
		{
			get 
			{ 
				#if DEBUG
				if ((AIndex < 0) || (AIndex >= Count))
					throw new IndexOutOfRangeException();
				#endif
				return FItems[AIndex]; 
			}
			set
			{
				#if DEBUG
				if ((AIndex < 0) || (AIndex >= Count))
					throw new IndexOutOfRangeException();
				#endif
				FItems[AIndex] = value;
			}
		}
		
		public int IndexOf(T AItem)
		{
			for (int LIndex = 0; LIndex < FCount; LIndex++)
				if (Object.Equals(this[LIndex], AItem))
					return LIndex;
					
			return -1;
		}
		
		public bool Contains(T AItem)
		{
			return IndexOf(AItem) >= 0;
		}

		public void CopyTo(int ASourceIndex, T[] AArray, int AArrayIndex, int ACount)
		{
			if (FItems != null)
				Array.Copy(FItems, ASourceIndex, AArray, AArrayIndex, ACount);
		}

		public void CopyTo(T[] AArray, int AArrayIndex)
		{
			CopyTo(0, AArray, AArrayIndex, FCount);
		}
		
		public void CopyTo(T[] AArray)
		{
			CopyTo(AArray, 0);
		}

		/// <summary> Changes the index of an item. </summary>
		public void Move(int AOldIndex, int ANewIndex)
		{
			Insert(ANewIndex, RemoveAt(AOldIndex));
		}
		
		#region IList<T> Members

		int IList<T>.IndexOf(T AItem)
		{
			return IndexOf(AItem);
		}

		void IList<T>.Insert(int AIndex, T AItem)
		{
			Insert(AIndex, AItem);
		}

		void IList<T>.RemoveAt(int AIndex)
		{
			RemoveAt(AIndex);
		}

		T IList<T>.this[int AIndex]
		{
			get { return this[AIndex]; }
			set { this[AIndex] = value; }
		}

		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T AItem)
		{
			Add(AItem);
		}

		void ICollection<T>.Clear()
		{
			Clear();
		}

		bool ICollection<T>.Contains(T AItem)
		{
			return Contains(AItem);
		}

		void ICollection<T>.CopyTo(T[] AArray, int AArrayIndex)
		{
			CopyTo(AArray, AArrayIndex);
		}

		int ICollection<T>.Count
		{
			get { return Count; }
		}

		bool ICollection<T>.IsReadOnly
		{
			get { return false; }
		}

		bool ICollection<T>.Remove(T AItem)
		{
			int LIndex = IndexOf(AItem);
			if (LIndex >= 0)
			{
				RemoveAt(LIndex);
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
            public BaseListEnumerator(BaseList<T> AList) : base()
            {
                FList = AList;
            }
            
            private int FCurrent = -1;
            private BaseList<T> FList;

            public T Current { get { return FList[FCurrent]; } }
            
            object IEnumerator.Current { get { return FList[FCurrent]; } }

            public void Reset()
            {
                FCurrent = -1;
            }

            public bool MoveNext()
            {
				FCurrent++;
				return FCurrent < FList.Count;
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

		int IList.Add(object AValue)
		{
			return Add((T)AValue);
		}

		void IList.Clear()
		{
			Clear();
		}

		bool IList.Contains(object AValue)
		{
			return Contains((T)AValue);
		}

		int IList.IndexOf(object AValue)
		{
			return IndexOf((T)AValue);
		}

		void IList.Insert(int AIndex, object AValue)
		{
			Insert(AIndex, (T)AValue);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		void IList.Remove(object AValue)
		{
			Remove((T)AValue);
		}

		void IList.RemoveAt(int AIndex)
		{
			RemoveAt(AIndex);
		}

		object IList.this[int AIndex]
		{
			get { return this[AIndex]; }
			set { this[AIndex] = (T)value; }
		}

		#endregion

		#region ICollection Members

		void ICollection.CopyTo(Array AArray, int AIndex)
		{
			((IList<T>)this).CopyTo((T[])AArray, AIndex);
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
		
		public List(bool AAllowNulls) : base(4)
		{
			FAllowNulls = AAllowNulls;
		}

		private bool FAllowNulls;
		/// <summary> <c>AllowNulls</c> determines whether the list can contain null references. </summary>
		/// <remarks>
		///		If this property is changes while items are in the list, the items 
		///		are validated.
		/// </remarks>
		public bool AllowNulls
		{
			get { return FAllowNulls; }
			set
			{
				if (FAllowNulls != value)
				{
					if (!value)
						foreach(object LItem in this)
							InternalValidate(LItem, false);
					FAllowNulls = value;
				}
			}
		}

		private void InternalValidate(object AValue, bool AAllowNulls)
		{
			if (!AAllowNulls && (AValue == null))
				throw new BaseException(BaseException.Codes.CannotAddNull);
		}
		
		/// <summary> Overrides the validation to check the nullability of the item. </summary>
		protected override void Validate(object AValue)
		{
			InternalValidate(AValue, AllowNulls);
			base.Validate(AValue);
		}
	}

	public class DisposableList : List, IDisposable
	{
		public DisposableList() : base(){}
		
		/// <summary> Allows the initialization of the ItemsOwned and AllowNulls properties. </summary>
		/// <param name="AItemsOwned"> See <see cref="DisposableList.ItemsOwned"/> </param>
		/// <param name="AAllowNulls"> See <see cref="List.AllowNulls"/> </param>
		public DisposableList(bool AItemsOwned, bool AAllowNulls) : base(AAllowNulls)
		{
			FItemsOwned = AItemsOwned;
		}
		
		protected bool FItemsOwned = true;
		/// <summary>
		///		ItemsOwned controls whether or not the List "owns" the contained items.  
		///		"Owns" means that if the item supports the IDisposable interface, the will be
		///		disposed when the list is disposed or when an item is removed.  
		///	</summary>
		public bool ItemsOwned
		{
			get { return FItemsOwned; }
			set { FItemsOwned = value; }
		}

		/// <summary> <c>ItemDispose</c> is called by contained items when they are disposed. </summary>
		/// <remarks>
		///		This method simply removes the item from the list.  <c>ItemDispose</c> is 
		///		only called if the item is not disposed by this list.
		///	</remarks>
		protected virtual void ItemDispose(object ASender, EventArgs AArgs)
		{
			Disown(ASender);
			//Remove(ASender);
		}
		
		///	<remarks> Hooks the Disposed event of the item if the item implements IDisposable. </remarks>
		protected override void Adding(object AValue, int AIndex)
		{
			if (AValue is IDisposableNotify)
				((IDisposableNotify)AValue).Disposed += new EventHandler(ItemDispose);
		}

		/// <remarks> If the item is owned, it is disposed. </remarks>
		protected override void Removing(object AValue, int AIndex)
		{
			if (AValue is IDisposableNotify)
        	    ((IDisposableNotify)AValue).Disposed -= new EventHandler(ItemDispose);

        	if (FItemsOwned && !(FDisowning) && (AValue is IDisposable))
		        ((IDisposable)AValue).Dispose();
		}

		public override void Move(int AOldIndex, int ANewIndex)
		{
			FDisowning = true;
			try
			{
				base.Move(AOldIndex, ANewIndex);
			}
			finally
			{
				FDisowning = false;
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

		protected virtual void Dispose(bool ADisposing)
		{
			FDisposed = true;
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);

			Exception LException = null;
			while (Count > 0)
				try
				{
					RemoveAt(0);
				}
				catch (Exception E)
				{
					LException = E;
				}
				
			if (LException != null)
				throw LException;
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

		protected bool FDisposed;
		public bool IsDisposed { get { return FDisposed; } }

		protected bool FDisowning;
		
		/// <summary> Removes the specified object without disposing it. </summary>
		public virtual object Disown(object AValue)
		{
			FDisowning = true;
			try
			{
				Remove(AValue);
				return AValue;
			}
			finally
			{
				FDisowning = false;
			}
		}
		
		/// <summary> Removes the specified object index without disposing it. </summary>
		public virtual object DisownAt(int AIndex)
		{
			object LValue = this[AIndex];
			FDisowning = true;
			try
			{
				RemoveAt(AIndex);
				return LValue;
			}
			finally
			{
				FDisowning = false;
			}
		}
	}

	/// <summary> A Dictionary descendent which implements the IList interface. </summary>
	public abstract class HashtableList<TKey, TValue> : Dictionary<TKey, TValue>, IList
	{
		public HashtableList() : base() { }
		public HashtableList(IDictionary<TKey, TValue> ADictionary) : base(ADictionary) { }
		public HashtableList(int ACapacity) : base(ACapacity) { }
		public HashtableList(IEqualityComparer<TKey> AComparer) : base(AComparer) { }
		public HashtableList(IDictionary<TKey, TValue> ADictionary, IEqualityComparer<TKey> AComparer) : base(ADictionary, AComparer) { }

		public abstract int Add(object AValue);

		public TValue this[int AIndex]
		{
			get
			{
				int LIndex = 0;
				foreach (TKey LObject in Keys)
				{
					if (LIndex == AIndex)
						return base[LObject];
					LIndex++;
				}
				throw new BaseException(BaseException.Codes.ObjectAtIndexNotFound, AIndex.ToString());
			}
			set
			{
				throw new BaseException(BaseException.Codes.InsertNotSupported);
			}
		}
		
		object IList.this[int AIndex]
		{
			get { return this[AIndex]; }
			set { this[AIndex] = (TValue)value; }
		}

		public int IndexOf(object AValue)
		{
			int LIndex = 0;
			foreach (TKey LObject in Keys)
			{
				if (LObject.Equals(AValue))
					return LIndex;
				LIndex++;
			}
			return -1;
		}

		public void Insert(int AIndex, object AValue)
		{
			throw new BaseException(BaseException.Codes.InsertNotSupported);
		}

		public void RemoveAt(int AIndex)
		{
			Remove(this[AIndex]);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Values.GetEnumerator();
		}

		public IDictionaryEnumerator GetDictionaryEnumerator()
		{
			return base.GetEnumerator();
		}

		public bool Contains(object AValue)
		{
			return ContainsKey((TKey)AValue);
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Remove(object AValue)
		{
			base.Remove((TKey)AValue);
		}
	}
}
