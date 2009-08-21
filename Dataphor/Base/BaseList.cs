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
	///		<c>List</c> which can "own" it's chidren and provides internal validate and
	///		notification of add/remove operations
	///	</summary>
	/// <remarks>
	///		<c>List</c> can manage the life of it's contained items by taking "ownership"
	///		of them.  If an item is added to the list that implements 
	///		<see cref="IDisposable"><c>IDisposable</c></see>, it will be "disposed" when the
	///		list is disposed.  <c>List</c> can optionally disallow null items.
	///	</remarks>
	/// <seealso cref="DisposableList.ItemsOwned"/>
	[Serializable]
	public class List : ArrayList
	{
		public List() : base() {}
		
		public List(bool AAllowNulls) : base(4)
		{
			FAllowNulls = AAllowNulls;
		}

		protected bool FAllowNulls;
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
		
		/// <summary> Changes the index of an item. </summary>
		public virtual void Move(int AOldIndex, int ANewIndex)
		{
			Insert(ANewIndex, RemoveItemAt(AOldIndex));
		}
		
		/// <summary>
		///		<c>InternalValidate</c> is used internally by List to test potential validation
		///		scenarios against the list.
		///	</summary>
		protected void InternalValidate(object AValue, bool AAllowNulls)
		{
			if (!AAllowNulls && (AValue == null))
				throw new BaseException(BaseException.Codes.CannotAddNull);
		}
		
		/// <summary> Validate is called before an item is added or set in a List </summary>
		/// <remarks>
		///		The default behavior is to throw on an attempt to add a null reference.  Override 
		///		this method and throw an exception in order to perform other item validation for
		///		the list.
		/// </remarks>
		protected virtual void Validate(object AValue)
		{
			InternalValidate(AValue, FAllowNulls);
		}
		
		/// <summary> Adding is called when an item is added to the list </summary>
		/// <remarks>
		///		This should NOT be used for validation, but is a good place to put 
		///		code that interacts with items in the list.  
		///	</remarks>
		protected virtual void Adding(object AValue, int AIndex){}
		
		/// <summary> Removing is called when an item is being removed from the List </summary>
		protected virtual void Removing(object AValue, int AIndex){}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override int Add(object AValue)
		{
			int LIndex = Count;
			Insert(LIndex, AValue);
			return LIndex;
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void AddRange(ICollection ACollection)
		{
			foreach (object LObject in ACollection)
				Add(LObject);
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void Insert(int AIndex, object AValue)
		{
			Validate(AValue);
			base.Insert(AIndex, AValue);
			Adding(AValue, AIndex);
		}

		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void InsertRange(int AIndex, ICollection ACollection)
		{
			for (int LIndex = 0; LIndex < ACollection.Count; LIndex++)
				Insert(AIndex + LIndex, ((IList)ACollection)[LIndex]);
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void Remove(object AValue)
		{
		    RemoveAt(IndexOf(AValue));
		}

		/// <summary>Removes AValue if it is found in the list, does nothing otherwise.</summary>		
		public void SafeRemove(object AValue)
		{
			int LIndex = IndexOf(AValue);
			if (LIndex >= 0)
				Remove(AValue);
		}
		
		public virtual object RemoveItemAt(int AIndex)
		{
			object LObject = base[AIndex];
			try
			{
				Removing(LObject, AIndex);
			}
			finally
			{
				base.RemoveAt(AIndex);
			}
			return LObject;
		}

		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void RemoveAt(int AIndex)
		{
			RemoveItemAt(AIndex);
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void RemoveRange(int AIndex, int ACount)
		{
			for (int LIndex = 0; LIndex < ACount; LIndex++)
				RemoveAt(AIndex);
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void Clear()
		{
			while (Count > 0)
				RemoveAt(0);
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override void SetRange(int AIndex, ICollection ACollection)
		{
			for (int LIndex = 0; LIndex < ACollection.Count; LIndex++)
				this[LIndex + AIndex] = ((IList)ACollection)[LIndex];
		}
		
		/// <summary> ArrayList override - captures notification/validation </summary>
		public override object this[int AIndex]
		{
			get { return base[AIndex]; }
			set
			{
				RemoveAt(AIndex);
				Insert(AIndex, value);
			}
		}
	}
	
	[Serializable]
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

	public delegate void ListEventHandler(object ASender, object AItem);
	
	/// <summary> Specifies a list who can notify of item changes </summary>
	public interface INotifyList : IList
	{
		event ListEventHandler OnAdding;
		event ListEventHandler OnRemoving;
		event ListEventHandler OnValidate;
		event ListEventHandler OnChanged;
	}
	
	public delegate void ListItemEventHandler(object ASender);
	
	public interface INotifyListItem
	{
		event ListItemEventHandler OnChanged;
	}
	
	/// <summary> A <c>List</c> descendant that implements <see cref="INotifyList">INotifyList</see> </summary>
	public class NotifyList : List, INotifyList
	{
		public NotifyList() : base(){}
		public NotifyList(bool AAllowNulls) : base(AAllowNulls){}
		
		public event ListEventHandler OnAdding;
		public event ListEventHandler OnRemoving;
		public event ListEventHandler OnValidate;
		public event ListEventHandler OnChanged;
		
		protected override void Adding(object AObject, int AIndex)
		{
			if (OnAdding != null)
				OnAdding(this, AObject);
			base.Adding(AObject, AIndex);
			Changed(AObject);
			INotifyListItem LItem = AObject as INotifyListItem;
			if (LItem != null)
				LItem.OnChanged += new ListItemEventHandler(ItemChanged);
		}
		
		protected override void Removing(object AObject, int AIndex)
		{
			INotifyListItem LItem = AObject as INotifyListItem;
			if (LItem != null)
				LItem.OnChanged -= new ListItemEventHandler(ItemChanged);
			if (OnRemoving != null)
				OnRemoving(this, AObject);
			base.Removing(AObject, AIndex);
			Changed(AObject);
		}
		
		protected override void Validate(object AObject)
		{
			if (OnValidate != null)
				OnValidate(this, AObject);
			base.Validate(AObject);
		}
		
		protected virtual void Changed(object AObject)
		{
			if (OnChanged != null)
				OnChanged(this, AObject);
		}
		
		protected virtual void ItemChanged(object ASender)
		{
			Changed(ASender);
		}
	}	
	
	public class DisposableNotifyList : DisposableList, INotifyList
	{
		public DisposableNotifyList() : base(){}
		public DisposableNotifyList(bool AItemsOwned, bool AAllowNulls) : base(AItemsOwned, AAllowNulls){}
		
		public event ListEventHandler OnAdding;
		public event ListEventHandler OnRemoving;
		public event ListEventHandler OnValidate;
		public event ListEventHandler OnChanged;
		
		protected override void Adding(object AObject, int AIndex)
		{
			if (OnAdding != null)
				OnAdding(this, AObject);
			base.Adding(AObject, AIndex);
			Changed(AObject);
		}
		
		protected override void Removing(object AObject, int AIndex)
		{
			if (OnRemoving != null)
				OnRemoving(this, AObject);
			base.Removing(AObject, AIndex);
			Changed(AObject);
		}
		
		protected override void Validate(object AObject)
		{
			if (OnValidate != null)
				OnValidate(this, AObject);
			base.Validate(AObject);
		}
		
		protected virtual void Changed(object AObject)
		{
			if (OnChanged != null)
				OnChanged(this, AObject);
		}
	}
	
	/// <summary> A class that validates the type of each item against a specified type </summary>
	public class TypedList : List
	{
		public TypedList() : base() {}
		
		public TypedList(Type AItemType) : base()
		{
			FItemType = AItemType;
		}
	
		/// <summary> This constructor allows the initialization of the TypedList's properties </summary>
		public TypedList(Type AItemType, bool AAllowNulls) : base(AAllowNulls)
		{
			FItemType = AItemType;
		}
		
		protected Type FItemType;
		/// <summary> Determines the type of items "allowed" in this list. </summary>
		/// <remarks>
		///		In order to be successfully added to this list, items must be this type or a 
		///		descendant thereof.  If <c>ItemType == null</c> then any type is allowed in the list.
		///		When this property is set, existing items (if any) are validated to ensure they 
		///		are of the proper type.
		///	</remarks>
		public Type ItemType
		{
			get { return FItemType; }
			set
			{
				if (value != FItemType)
				{
					foreach (object LItem in this)
						InternalValidate(LItem, value);
					FItemType = value;
				}
			}
		}
		
		/// <summary>
		///		<c>InternalValidate</c> is used internally to test potential validation
		///		scenarios against the list.
		///	</summary>
		protected void InternalValidate(object AValue, Type AItemType)
		{
			if ((AValue != null) && (AItemType != null) && !(AItemType.IsInstanceOfType(AValue) || AValue.GetType().IsSubclassOf(AItemType)))
				throw new BaseException(BaseException.Codes.CollectionOfType, AValue.GetType(), AItemType.ToString());
		}
		
		/// <summary>
		///		<c>TypedList</c> overrides the Validate method to ensure that items in the 
		///		list are of the appropriate type (see <see cref="TypedList.ItemType">ItemType</see>).
		///	</summary>
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			InternalValidate(AValue, FItemType);
		}
	}
	
	[Serializable]
	public class DisposableTypedList : DisposableList
	{
		public DisposableTypedList() : base(){}
		public DisposableTypedList(Type AItemType) : base()
		{
			FItemType = AItemType;
		}
		
		public DisposableTypedList(Type AItemType, bool AItemsOwned, bool AAllowNulls) : base(AItemsOwned, AAllowNulls)
		{
			FItemType = AItemType;
		}

		protected Type FItemType;
		/// <summary> Determines the type of items "allowed" in this list. </summary>
		/// <remarks>
		///		In order to be successfully added to this list, items must be this type or a 
		///		descendant thereof.  If <c>ItemType == null</c> then any type is allowed in the list.
		///		When this property is set, existing items (if any) are validated to ensure they 
		///		are of the proper type.
		///	</remarks>
		public Type ItemType
		{
			get { return FItemType; }
			set
			{
				if (value != FItemType)
				{
					foreach (object LItem in this)
						InternalValidate(LItem, value);
					FItemType = value;
				}
			}
		}
		
		/// <summary>
		///		<c>InternalValidate</c> is used internally to test potential validation
		///		scenarios against the list.
		///	</summary>
		protected void InternalValidate(object AValue, Type AItemType)
		{
			if ((AItemType != null) && !(AItemType.IsInstanceOfType(AValue) || AValue.GetType().IsSubclassOf(AItemType)))
				throw new BaseException(BaseException.Codes.CollectionOfType, AValue.GetType(), AItemType.ToString());
		}
		
		/// <summary>
		///		<c>TypedList</c> overrides the Validate method to ensure that items in the 
		///		list are of the appropriate type (see <see cref="TypedList.ItemType">ItemType</see>).
		///	</summary>
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			InternalValidate(AValue, FItemType);
		}
	}

    /// <summary>
    ///     Provides a list which accesses the information in another list, 
    ///     limited to the members of that list of a given Type.
    /// </summary>    
	public class ListCover : IList, ICollection, IEnumerable
    {
        public ListCover(IList AList, Type AType) : base()
        {
            FList = AList;
            FType = AType;
        }

        protected IList FList;
        protected Type FType;
        
        // IList

		public object this[int AIndex]
        {
            get
            {
                int LIndex = AIndex;
                object LReturnObject = null;
                foreach(object LObject in FList)
                {
                    if (FType.IsInstanceOfType(LObject) || LObject.GetType().IsSubclassOf(FType))
                    {
                        LIndex--;
                        if (LIndex < 0)
                        {
                            LReturnObject = LObject;
                            break;
                        }
                    }
                }
                if (LReturnObject == null)
                    throw new BaseException(BaseException.Codes.InvalidListIndex, AIndex.ToString());
                return LReturnObject;
            }
            set
            {
                throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
            }
        }

		public int Add(object AValue)
		{
			throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
		}

        public void Clear()
        {
			throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
		}
        
		public bool Contains(object AValue)
        {
            foreach(object LObject in FList)
            {
                if ((FType.IsInstanceOfType(LObject) || LObject.GetType().IsSubclassOf(FType)) && (LObject.Equals(AValue)))
                    return true;
            }
            return false;
        }

        public int IndexOf(object AValue)
        {
            int LIndex = 0;
            foreach(object LObject in FList)
            {
                if (FType.IsInstanceOfType(LObject) || LObject.GetType().IsSubclassOf(FType))
                {
                    if (LObject.Equals(AValue))
                        return LIndex;
                    LIndex++;
                }
            }
            return -1;
        }

        public void Insert(int AIndex, object AValue)
        {
			throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
		}

        public void Remove(object AValue)
        {
			throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
		}

        public void RemoveAt(int AIndex)
        {
			throw new BaseException(BaseException.Codes.ListCoverIsReadOnly);
		}
        
        // IList interface
        public int Count
        {
            get
            {
                int LIndex = 0;
                foreach(object LObject in FList)
                {
                    if (FType.IsInstanceOfType(LObject) || LObject.GetType().IsSubclassOf(FType))
                        LIndex ++;
                }
                return LIndex;
            }
        }

        public bool IsReadOnly
        {
            get { return true; }    
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

		public bool IsFixedSize
		{
			get { return false; }
		}

        public object SyncRoot
        {
            get { return this; }
        }

        public void CopyTo(Array AArray, int AIndex)
        {
            foreach(object LObject in FList)
            {
                if (FType.IsInstanceOfType(LObject) || LObject.GetType().IsSubclassOf(FType))
                {
                    ((IList)AArray)[AIndex] = LObject;
                    AIndex++;
                }
            }
        }
        
        // IEnumerable

        public IEnumerator GetEnumerator()
        {
            return new ListCoverEnumerator(this);
        }

		public class ListCoverEnumerator : IEnumerator
        {
            public ListCoverEnumerator(ListCover AListCover) : base()
            {
                FListCover = AListCover;
            }
            
            private int FCurrent = -1;
            private ListCover FListCover;

            public object Current
            {
                get { return FListCover[FCurrent]; }
            }

            public void Reset()
            {
                FCurrent = -1;
            }

            public bool MoveNext()
            {
				FCurrent++;
				return FCurrent < FListCover.Count;
            }
        }
    }

	/// <summary>A Hashtable descendent which implements the IList interface.</summary>
	public abstract class HashtableList : Hashtable, IList
    {
		public HashtableList() : base() {}
		public HashtableList(IDictionary ADictionary) : base(ADictionary) {}
		public HashtableList(int ACapacity) : base(ACapacity) {}
		public HashtableList(IDictionary ADictionary, float ALoadFactor) : base(ADictionary, ALoadFactor) {}
		public HashtableList(IEqualityComparer AComparer) : base(AComparer) {}
		public HashtableList(int ACapacity, float ALoadFactor) : base(ACapacity, ALoadFactor) {}
		public HashtableList(IDictionary ADictionary, IEqualityComparer AComparer) : base (ADictionary, AComparer) {}

		public abstract int Add(object AValue);
		
		public object this[int AIndex]
		{
			get 
			{
				int LIndex = 0;
				foreach (object LObject in Keys)
				{
					if (LIndex == AIndex)
						return this[LObject];
					LIndex++;
				}
				throw new BaseException(BaseException.Codes.ObjectAtIndexNotFound, AIndex.ToString());
			}
			set 
			{
				throw new BaseException(BaseException.Codes.InsertNotSupported);
			}
		}

		public int IndexOf(object AValue)
		{
			int LIndex = 0;
			foreach (object LObject in Keys)
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

		public new HashtableListEnumerator GetEnumerator()
		{
			return new HashtableListEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new HashtableListEnumerator(this);
		}

		public IDictionaryEnumerator GetDictionaryEnumerator()
		{
			return base.GetEnumerator();
		}

		public class HashtableListEnumerator : IEnumerator
		{
			public HashtableListEnumerator(HashtableList AHashtableList) : base()
			{
				FEnumerator = AHashtableList.GetDictionaryEnumerator();
			}
        
			private IDictionaryEnumerator FEnumerator;

			object IEnumerator.Current
			{
				get { return FEnumerator.Entry.Value; }
			}

            public object Current
            {
                get { return FEnumerator.Entry.Value; }
            }

			public void Reset()
			{
				FEnumerator.Reset();
			}

			public bool MoveNext()
			{
				return FEnumerator.MoveNext();
			}
		}
	}

	/// <summary> This class must be implemented by objects stored as children of <see cref="LinkedCollection"/>. </summary>
	/// <remarks>
	///		Classes implementing this interface are not to modify the members introduced by 
	///		this interface, and they cannot be added to multiple LinkedCollection lists.
	///		Note that these restrictions are not enforced and their violation will result
	///		in the LinkedCollection entering an invalid state.  In short... you will break
	///		the list(s)!
	/// </remarks>
	public interface ILinkedItem
	{
		ILinkedItem Prior { get; set; }
		ILinkedItem Next { get; set; }
	}

	/// <summary> A list that is optimized for adding, removing and enumerating. </summary>
	/// <remarks>
	///		Index operations on this list are order n (slow).  Additionally, this collection
	///		requires that it's children implement ILinkedItem.  This indirectly means that
	///		children cannot be a part of more than one LinkedCollection and that part of the
	///		"contract" of being part of this list means that the children are on scouts honor
	///		not to modify their <see cref="ILinkedItem"/> members; they should let the LinkedCollection
	///		completely handle their ILinkedItem members..
	/// </remarks>
	public class LinkedCollection : IList
	{
		private ILinkedItem FFirstItem;
		public ILinkedItem FirstItem
		{
			get { return FFirstItem; }
		}

		private ILinkedItem FLastItem;
		public ILinkedItem LastItem
		{
			get { return FLastItem; }
		}

		// IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new LinkedEnumerator(this);
		}

		public LinkedEnumerator GetEnumerator()
		{
			return new LinkedEnumerator(this);
		}

		public class LinkedEnumerator : IEnumerator
		{
			public LinkedEnumerator(LinkedCollection ACollection)
			{
				FCollection = ACollection;
				FCurrent = FCollection.FirstItem;
			}

			private ILinkedItem FCurrent;
			private LinkedCollection FCollection;
			private bool FInitialized;

			object IEnumerator.Current
			{
				get { return FCurrent; }
			}
			
			public ILinkedItem Current
			{
				get { return FCurrent; }
			}

			public bool MoveNext()
			{
				if (!FInitialized)
					FInitialized = true;
				else
				{
					if (FCurrent != null)
						FCurrent = FCurrent.Next;
				}
				return FCurrent != null;
			}

			public void Reset()
			{
				FCurrent = FCollection.FirstItem;
			}
		}

		// ICollection

		public int Count
		{
			get
			{
				int LResult = 0;
				ILinkedItem FCurrent = FFirstItem;
				while (FCurrent != null)
				{
					LResult++;
					FCurrent = FCurrent.Next;
				}
				return LResult;
			}
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { return this; }
		}

		public void CopyTo(Array ATarget, int AStartIndex)
		{
			ILinkedItem FCurrent = FFirstItem;
			while (FCurrent != null)
			{
				ATarget.SetValue(FCurrent, AStartIndex);
				AStartIndex++;
				FCurrent = FCurrent.Next;
			}
		}

		// IList

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		object IList.this[int AIndex]
		{
			get { return this[AIndex]; }
			set { this[AIndex] = (ILinkedItem)value; }
		}

		public ILinkedItem this[int AIndex]
		{
			get
			{
				int LCount = 0;
				ILinkedItem FCurrent = FFirstItem;
				while ((FCurrent != null) && (LCount < AIndex))
				{
					LCount++;
					FCurrent = FCurrent.Next;
				}
				if ((LCount < AIndex) || (FCurrent == null))
					throw new BaseException(BaseException.Codes.IndexOutOfBounds, AIndex, LCount);
				return FCurrent;
			}
			set
			{
				RemoveAt(AIndex);
				Insert(AIndex, value);
			}
		}

		int IList.Add(object AValue)
		{
			Add((ILinkedItem)AValue);
			return Count - 1;
		}

		public void Add(ILinkedItem AValue)
		{
			if (FLastItem != null)
			{
				FLastItem.Next = AValue;
				AValue.Prior = FLastItem;
				AValue.Next = null;
				FLastItem = AValue;
			}
			else
			{
				FFirstItem = AValue;
				FLastItem = AValue;
				AValue.Next = null;
				AValue.Prior = null;
			}
		}

		public void Clear()
		{
			FFirstItem = null;
			FLastItem = null;
		}

		public bool Contains(object AValue)
		{
			ILinkedItem LCurrent = FFirstItem;
			while (LCurrent != null)
			{
				if (LCurrent == AValue)
					return true;
				LCurrent = LCurrent.Next;
			}
			return false;
		}

		public int IndexOf(object AValue)
		{
			ILinkedItem LCurrent = FFirstItem;
			int LIndex = 0;
			while (LCurrent != null)
			{
				if (LCurrent == AValue)
					return LIndex;
				LCurrent = LCurrent.Next;
			}
			return -1;
		}

		void IList.Insert(int AIndex, object AValue)
		{
			Insert(AIndex, (ILinkedItem)AValue);
		}

		public void Insert(int AIndex, ILinkedItem AValue)
		{
			ILinkedItem LCurrent = FFirstItem;
			while ((LCurrent != null) && (AIndex > 0))
			{
				AIndex--;
				LCurrent = LCurrent.Next;
			}
			if (AIndex != 0)
				throw new BaseException(BaseException.Codes.IndexOutOfBounds, String.Empty, String.Empty);
			if (LCurrent == null)
			{
				AValue.Next = null;
				AValue.Prior = null;
				FFirstItem = AValue;
				FLastItem = AValue;
			}
			else
			{
				if (LCurrent == FFirstItem)
					FFirstItem = AValue;
				else
					LCurrent.Prior.Next = AValue;
				AValue.Prior = LCurrent.Prior;
				LCurrent.Prior = AValue;
				AValue.Next = LCurrent;
			}
		}

		void IList.Remove(object AValue)
		{
			Remove((ILinkedItem)AValue);
		}

		public void Remove(ILinkedItem AValue)
		{
			if (AValue == FFirstItem)
				FFirstItem = AValue.Next;
			else
				AValue.Prior.Next = AValue.Next;
			if (AValue == FLastItem)
				FLastItem = AValue.Prior;
			else
				AValue.Next.Prior = AValue.Prior;
		}

		public void RemoveAt(int AIndex)
		{
			Remove(this[AIndex]);
		}
	}
	
	public static class CollectionUtility
	{
		/// <summary> Finds the first item in the given collection that matches the specified predicate. </summary>
		public static bool FindMatch<T>(ICollection ACollection, Predicate<T> APredicate, out T LMatch) where T : class
		{
			foreach (object LItem in ACollection)
			{
				T LTypedItem = LItem as T;
				if ((LTypedItem != null) && APredicate(LTypedItem))
				{
					LMatch = LTypedItem;
					return true;
				}
			}
			LMatch = null;
			return false;
		}
	}
}
