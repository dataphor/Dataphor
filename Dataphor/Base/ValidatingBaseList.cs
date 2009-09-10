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
		public const int CDefaultInitialCapacity = 0;
		public const int CMinimumGrowth = 4;
		public const int CMaximumGrowth = 512;
		
		public ValidatingBaseList() : base() { }
		public ValidatingBaseList(int ACapacity) : base()
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
		
		/// <summary> Validate is called before an item is added or set in a List </summary>
		/// <remarks>
		///		Override and throw an exception in order to perform item validation for
		///		the list.
		/// </remarks>
		protected virtual void Validate(T AValue) { }
		
		/// <summary> Adding is called when an item is added to the list. </summary>
		/// <remarks>
		///		This should NOT be used for validation, but is a good place to put 
		///		code that interacts with items in the list.  
		///	</remarks>
		protected virtual void Adding(T AValue, int AIndex) { }
		
		/// <summary> Removing is called when an item is being removed from the List. </summary>
		protected virtual void Removing(T AValue, int AIndex) { }

		/// <summary> Removed is call <i>after</i> an item as been removed from the list. </summary>
		protected virtual void Removed(T AValue, int AIndex) { }
		
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
			Validate(AValue);
			EnsureCapacity(FCount);
			for (int LIndex = FCount - 1; LIndex >= AIndex; LIndex--)
				FItems[LIndex + 1] = FItems[LIndex];
			FItems[AIndex] = AValue;
			FCount++;
			Adding(AValue, AIndex);
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
			try
			{
				Removing(LItem, AIndex);
			}
			finally
			{
				FCount--;
				for (int LIndex = AIndex; LIndex < FCount; LIndex++)
					FItems[LIndex] = FItems[LIndex + 1];
				FItems[FCount] = default(T); // Clear the last slot
			}
			Removed(LItem, AIndex);
			return LItem;
		}
		
		public void RemoveRange(int AIndex, int ACount)
		{
			for (int LIndex = 0; LIndex < ACount; LIndex++)
				RemoveAt(AIndex);
		}
		
		public virtual void Clear()
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
				T LItem = this[AIndex];
				Removing(LItem, AIndex);
				Validate(value);
				FItems[AIndex] = value;
				Removed(LItem, AIndex);
				Adding(value, AIndex);
			}
		}
		
		public virtual int IndexOf(T AItem)
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
            public BaseListEnumerator(ValidatingBaseList<T> AList) : base()
            {
                FList = AList;
            }
            
            private int FCurrent = -1;
            private ValidatingBaseList<T> FList;

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
        
		/// <summary> Changes the index of an item. </summary>
		public virtual void Move(int AOldIndex, int ANewIndex)
		{
			Insert(ANewIndex, RemoveAt(AOldIndex));
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
}
