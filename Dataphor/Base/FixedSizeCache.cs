using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	/// <summary> Fixed size cache list. </summary>
	/// <remarks> Currently implemented as a LRU (Least Recently Used) algorithm. </remarks>
	public class FixedSizeCache<TKey, TValue> : IDictionary<TKey, TValue>, IEnumerable<TValue>
	{
		/// <param name="ASize"> The size of the cache (in entries). </param>
		public FixedSizeCache(int ASize)
		{
			if (ASize < 2)
				throw new BaseException(BaseException.Codes.MinimumSize);
			FSize = ASize;
			FEntries = new Dictionary<TKey, Entry>(ASize);
		}

		private Dictionary<TKey, Entry> FEntries;
		private int FSize;
		public int Size { get { return FSize; } }

		/// <summary> The number of cache entries. </summary>
		public int Count
		{
			get { return FEntries.Count; }
		}

		#region LRU maintenance

		internal class Entry
		{
			internal Entry FNext;
			internal Entry FPrior;
			internal TKey FKey;
			internal TValue FValue;
		}

		private Entry FHead;
		private Entry FTail;

		private void RemoveEntry(Entry AEntry)
		{
			if (AEntry.FPrior != null)
				AEntry.FPrior.FNext = AEntry.FNext;
			if (AEntry.FNext != null)
				AEntry.FNext.FPrior = AEntry.FPrior;
			if (AEntry == FTail)
				FTail = AEntry.FNext;
			if (AEntry == FHead)
				FHead = AEntry.FPrior;
			AEntry.FPrior = null;
			AEntry.FNext = null;
		}

		private void AddEntry(Entry AEntry)
		{
			AEntry.FPrior = FHead;
			AEntry.FNext = null;
			if (FHead != null)
				FHead.FNext = AEntry;
			else
				FTail = AEntry;
			FHead = AEntry;
		}

		private void ClearEntries()
		{
			FHead = null;
			FTail = null;
		}

		#endregion

		/// <summary> Submits a reference to the cache to be added or reprioritized. </summary>
		/// <param name="AValue"> The item to be added to the cache. </param>
		/// <returns> The value that was removed because the available size was completely allocated, or null if no item was removed. </returns>
		public TValue Reference(TKey AKey, TValue AValue)
		{
			TValue LResult = default(TValue);
			Entry LEntry;
			if (FEntries.TryGetValue(AKey, out LEntry))
			{
				// Move the item to the head of the MRU
				RemoveEntry(LEntry);
				AddEntry(LEntry);
			}
			else
			{
				// If the list is full, remove and re-use the oldest; otherwise create a new entry
				if (FEntries.Count >= FSize)
				{
					LEntry = FTail;
					LResult = FTail.FValue;
					FEntries.Remove(FTail.FKey);
					RemoveEntry(FTail);
				}
				else
					LEntry = new Entry();

				LEntry.FKey = AKey;
				AddEntry(LEntry);
				FEntries.Add(AKey, LEntry);
			}
			// Adjust the entry's value
			LEntry.FValue = AValue;
			return LResult;
		}

		#region IDictionary Members

		public bool TryGetValue(TKey AKey, out TValue AValue)
		{
			Entry LEntry;
			if (FEntries.TryGetValue(AKey, out LEntry))
			{
				AValue = LEntry.FValue;
				return true;
			}
			else
			{
				AValue = default(TValue);
				return false;
			}
		}

		public void Add(KeyValuePair<TKey, TValue> AItem)
		{
			Add(AItem.Key, AItem.Value);
		}

		public bool Contains(KeyValuePair<TKey, TValue> AItem)
		{
			return ContainsKey(AItem.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] AArray, int AArrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(KeyValuePair<TKey, TValue> AItem)
		{
			return Remove(AItem.Key);
		}

		/// <remarks> Avoid this overload as it must allocate a KeyValuePair for each result. </remarks>
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			foreach (KeyValuePair<TKey, Entry> LEntry in FEntries)
				yield return new KeyValuePair<TKey, TValue>(LEntry.Key, LEntry.Value.FValue);
		}

	
		public TValue this[TKey AKey]
		{
			get
			{
				Entry LEntry;
				if (FEntries.TryGetValue(AKey, out LEntry))
					return LEntry.FValue;
				else
					return default(TValue);
			}
			set
			{
				// Remove the old entry if it exists
				Entry LEntry;
				if (FEntries.TryGetValue(AKey, out LEntry))
				{
					FEntries.Remove(AKey);
					RemoveEntry(LEntry);
				}
				// Add the new one if it is not null
				if (value != null)
					Add(AKey, value);
			}
		}

		public bool ContainsKey(TKey AKey)
		{
			return FEntries.ContainsKey(AKey);
		}

		/// <summary> Adds the specified key/value to the cache. </summary>
		/// <remarks> Note that this may remove another item.  To determine what item is removed upon 
		///	entry, use Reference rather than Add. </remarks>
		public void Add(TKey AKey, TValue AValue)
		{
			Reference(AKey, AValue);
		}

		/// <summary> Removes the specified key from the cache. </summary>
		public bool Remove(TKey AKey)
		{
			Entry LEntry;
			if (FEntries.TryGetValue(AKey, out LEntry))
			{
				RemoveEntry(LEntry);
				FEntries.Remove(AKey);
				return true;
			}
			else
				return false;
		}

		/// <summary> Clears the cache of all entries. </summary>
		public void Clear()
		{
			FEntries.Clear();
			ClearEntries();
		}

		public ICollection<TKey> Keys
		{
			get { return FEntries.Keys; }
		}

		/// <remarks> Unimplemented. </remarks>
		public ICollection<TValue> Values
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<TKey, Entry> LEntry in FEntries)
				yield return LEntry.Value;
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			foreach (KeyValuePair<TKey, Entry> LEntry in FEntries)
				yield return LEntry.Value.FValue;
		}

		#endregion
	}
}
