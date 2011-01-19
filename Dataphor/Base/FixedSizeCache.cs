using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Alphora.Dataphor
{
	public struct FixedSizeCacheSettings
	{ 
		public ProperFraction Cutoff { get; set; } 	
		public uint CorrelatedReferencePeriod { get; set; } 		
	}
		
	/// <summary> Fixed size cache list. </summary>
	/// <remarks> Currently implemented as a LRU (Least Recently Used) algorithm. </remarks>
	/// Note: this class uses the convention of prefixing methods where synchronization is a concern with "Syn".
	/// The premise is: a method prefixed with "Syn" can only be called by another method prefixed with "Syn"
	/// or within a protected block.
	public class FixedSizeCache<TKey, TValue> : IDictionary<TKey, TValue>, IEnumerable<TValue>
	{
		public static readonly ProperFraction CDefaultCutoff = new ProperFraction(0.33m);
		public const uint CDefaultCorrelatedReferencePeriod = 30;

		/// <param name="ASize"> The size of the cache (in entries). </param>
		public FixedSizeCache(int ASize)
		{
			if (ASize < 2)
				throw new BaseException(BaseException.Codes.MinimumSize);
			Size = ASize;
			FEntries = new Dictionary<TKey, Entry>(ASize);
			DefaultSettings();
		}

		private Dictionary<TKey, Entry> FEntries;

		/// <summary> Logical time used to track recency of Entry usage. </summary>
		/// <remarks> Incremented with each Entry access (logical clock).  This may roll over. Note that this does not necessarily 
		/// indicate the relative index of a Entry in the LRU chain because there are multiple insertion points into the LRU. </remarks>
		private int FLogicalTime;
				
		public int Size { get; private set; }

		/// <summary> The number of cache entries. </summary>
		public int Count
		{
			get { return FEntries.Count; }
		}

		/// <summary> Gets or sets the settings for the cache. </summary>
		/// <remarks> Changing these settings will not affect the current state of the cache, but will be used for subsequent operations. </remarks>
		public FixedSizeCacheSettings Settings { get; set; }						

		/// <summary> Sets or resets the cache's settings to their defaults. </summary>
		public void DefaultSettings()
		{
			FixedSizeCacheSettings LSettings = new FixedSizeCacheSettings();   			
			LSettings.CorrelatedReferencePeriod = CDefaultCorrelatedReferencePeriod;
			LSettings.Cutoff = CDefaultCutoff; 			
			Settings = LSettings;
		}

		#region LRU Maintenance

		// TODO: Investigate splitting FCacheLatch into LRU and FFrames latches

		public class Entry
		{
			internal Entry(int ALLogicalCreationTime)
			{
				FLastAccess = ALLogicalCreationTime;
			}
			internal TKey FKey;
			public TKey Key { get { return FKey; } }
			internal TValue FValue;
			public TValue Value { get { return FValue; } }
			internal Entry FNext;
			internal Entry FPrior; 			
			internal bool FPreCutoff;
			internal int FLastAccess;	 
		}

		/// <summary> Latch used to protect the LRU chain as well as the FFrames table. </summary>
		private object FCacheLatch = new object();

		/// <summary> Pointer to the head of the LRU chain. </summary>
		private Entry FLRUHead;
		/// <summary> Pointer to the cuttoff point within the LRU chain. </summary>
		private Entry FLRUCutoff;
		/// <summary> Pointer to the tail of the LRU chain. </summary>
		private Entry FLRUTail;
		/// <summary> The number of entries that occur before (exclusive of) the cutoff entry. </summary>
		private int FLRUPreCutoffCount;
		/// <summary> The total number of entries in the LRU chain. </summary>
		private int FLRUCount;

		/// <summary> Initializes the list. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynInitializeLRU(Entry AEntry)
		{
			FLRUCutoff = AEntry;
			FLRUHead = AEntry;
			FLRUTail = AEntry;
			AEntry.FPrior = null;
			AEntry.FNext = null;
			AEntry.FPreCutoff = false;
			SynAdjustEntryCount(1, false);
		}

		/// <summary> Detaches the Entry from the list. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynDetach(Entry AEntry)
		{  			
			if (AEntry.FPrior != null)
				AEntry.FPrior.FNext = AEntry.FNext;

			if (AEntry.FNext != null)
				AEntry.FNext.FPrior = AEntry.FPrior;

			if (AEntry == FLRUHead && AEntry.FPrior != null)
				FLRUHead = AEntry.FPrior;

			if (AEntry == FLRUCutoff)
				if (AEntry.FPrior != null)
					FLRUCutoff = AEntry.FPrior;
				else
				{
					if (AEntry.FNext != null)
						SynShiftCutoff(-1);
					else
						FLRUCutoff = null;
				}

			if (AEntry == FLRUTail && AEntry.FNext != null)
				FLRUTail = AEntry.FNext;  							
		}

		/// <summary> Places the entry at the head of the LRU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynPlaceAtHead(Entry AEntry)
		{ 			
			if (AEntry == FLRUHead)
				return;
			if (FLRUHead == null)
				SynInitializeLRU(AEntry);
			else
			{  						
				SynDetach(AEntry);
				FLRUHead.FNext = AEntry;
				AEntry.FPrior = FLRUHead;
				AEntry.FNext = null;
				AEntry.FPreCutoff = true;
				FLRUPreCutoffCount++;			
				FLRUHead = AEntry;									
			}
			SynUpdateCutoff();
		}

		/// <summary> Places the entry at the cutoff point of the LRU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynPlaceAtCutoff(Entry AEntry)
		{
			if (AEntry == FLRUCutoff)
				return;
			if (FLRUCutoff == null)
				SynInitializeLRU(AEntry);
			else
			{
				if (FLRUCutoff == FLRUHead)
					FLRUHead = AEntry;

				if (FLRUCutoff.FNext != null)
					FLRUCutoff.FNext.FPrior = AEntry;

				AEntry.FNext = FLRUCutoff.FNext;
				AEntry.FPrior = FLRUCutoff;
				FLRUCutoff.FNext = AEntry;				
				FLRUCutoff = AEntry;
				AEntry.FPreCutoff = false;
				SynAdjustEntryCount(1, false);
			}			
			SynUpdateCutoff();
		}  		 

		/// <summary> Removes the specified entry from the FLU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynLRURemove(Entry AEntry)
		{
			SynDetach(AEntry);
			AEntry.FPrior = null;
			AEntry.FNext = null;
			SynAdjustEntryCount(-1, AEntry.FPreCutoff);
			SynUpdateCutoff();
		}

		/// <summary> Maintains the total and cutoff LRU counts. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynAdjustEntryCount(int ADelta, bool APreCutoff)
		{
			FLRUCount += ADelta;
			if (APreCutoff)
				FLRUPreCutoffCount += ADelta;
		}

		/// <summary> Shifts the cutoff point to the point configured in the settings. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynUpdateCutoff()
		{
			SynShiftCutoff((FLRUCount * Settings.Cutoff) - FLRUPreCutoffCount);
		}

		/// <summary> Adjusts the cutoff point by the given delta. </summary>
		/// <remarks> This method assumes that the given delta will not adjust the cutoff point off of 
		/// the edge of the list.
		/// Locks-> Expects: FCacheLatch </remarks>
		private void SynShiftCutoff(int ADelta)
		{
			while (ADelta > 0)
			{
				FLRUCutoff.FPreCutoff = true;
				FLRUCutoff = FLRUCutoff.FPrior;
				ADelta--;
				FLRUPreCutoffCount++;
			}
			while (ADelta < 0)
			{
				FLRUCutoff = FLRUCutoff.FNext;
				FLRUCutoff.FPreCutoff = false;
				ADelta++;
				FLRUPreCutoffCount--;
			}
		}

		/// <summary> Clears the LRU. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynLRUClear()
		{
			FLRUHead = null;
			FLRUCutoff = null;
			FLRUTail = null;
			FLRUPreCutoffCount = 0;
			FLRUCount = 0;
		}

		#endregion

		/// <summary> Submits a reference to the cache to be added or reprioritized. </summary>
		/// <param name="AValue"> The item to be added to the cache. </param>
		/// <returns> The value that was removed because the available size was completely allocated, or null if no item was removed. </returns>
		public TValue Reference(TKey AKey, TValue AValue)
		{
			TValue LResult = default(TValue);
			Entry LEntry; 

			// Increment the logical clock and remember the logical time of this access
			int LLogicalTime = Interlocked.Increment(ref FLogicalTime);

			//for (; ; )	// Restart point
			//{
				//Monitor.Enter(FCacheLatch);
				FEntries.TryGetValue(AKey, out LEntry);
				if (LEntry != null)
				{
					//if (!Monitor.TryEnter(LEntry))
					//{
					//    Monitor.Exit(FCacheLatch);
					//    continue;
					//}
					//try
					//{ 						
						//try
						//{
							if (SubtractTime(LLogicalTime, LEntry.FLastAccess) > Settings.CorrelatedReferencePeriod)   			
							{		
								SynPlaceAtHead(LEntry); 									
								LEntry.FLastAccess = LLogicalTime;	
							}  							
						//}
						//finally
						//{
						//	Monitor.Exit(FCacheLatch);
						//}					 					
					//}
					//finally
					//{
					//    Monitor.Exit(LEntry);
					//}	 			
				}
				else
				{							
					// If the list is full, remove and re-use the oldest; otherwise create a new entry
					//try
					//{
						if (FEntries.Count >= Size)
						{
							//if (!Monitor.TryEnter(FLRUTail))
							//{
							//    Monitor.Exit(FCacheLatch);
							//    continue;
							//}
							//try
							//{
								LEntry = FLRUTail;
								LResult = FLRUTail.FValue;
								SynDetach(LEntry);
								FEntries.Remove(FLRUTail.FKey);								
								LEntry.FLastAccess = LLogicalTime;
							//}
							//finally
							//{
							//    Monitor.Exit(FLRUTail);
							//}
						}
						else
							LEntry = new Entry(LLogicalTime);  
					   
						LEntry.FKey = AKey;		 					
						SynPlaceAtCutoff(LEntry);
						FEntries.Add(AKey, LEntry);	
					//}
					//finally
					//{
					//	Monitor.Exit(FCacheLatch);
					//}									
				}
				//break;
			//}

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

		private void SynInternalRemove(TKey AKey, Entry AEntry)
		{
			//for (; ; )	// Restart point
			//{
			//Monitor.Enter(FCacheLatch);
			//if (!Monitor.TryEnter(AEntry))
			//{
			//   Monitor.Exit(FCacheLatch);
			//   continue;
			//}
			//try
			//{
		//	try
		//	{
				SynLRURemove(AEntry);
				FEntries.Remove(AKey);
		//	}
		//	finally
		//	{
		//		Monitor.Exit(FCacheLatch);
		//	}
			//}
			//finally
			//{
			//	Monitor.Exit(AEntry);
			//}
			//break;
			//}			
		}
		
		/// <summary> Removes the specified key from the cache. </summary>
		public bool Remove(TKey AKey)
		{
			Entry LEntry;
			//Monitor.Enter(FCacheLatch);
			//try
			//{
				if (FEntries.TryGetValue(AKey, out LEntry))
				{
					SynInternalRemove(AKey, LEntry);
					return true;
				}
				else
					return false;
			//}
			//finally
			//{
			//	Monitor.Exit(FCacheLatch);
			//}
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
				//Monitor.Enter(FCacheLatch);
				//try
				//{
					if (FEntries.TryGetValue(AKey, out LEntry))
					{
						SynInternalRemove(AKey, LEntry);
					}
					// Add the new one if it is not null
					if (value != null)
						Add(AKey, value);
				//}
				//finally
				//{
				//	Monitor.Exit(FCacheLatch);
				//}
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

		/// <summary> Clears the cache of all entries. </summary>
		public void Clear()
		{
			//Monitor.Enter(FCacheLatch); 
			//try
			//{
				FEntries.Clear();
				SynLRUClear();
			//}
			//finally
			//{
			//	Monitor.Exit(FCacheLatch);
			//}
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

		#region Static Utilities

		/// <summary> Subtracts one logical time from another accounting for rollover. </summary>
		private static uint SubtractTime(int AMinuend, int ASubtrahend)
		{
			long LResult = AMinuend - ASubtrahend;
			if (LResult >= 0)
				return (uint)LResult;
			else
				return (uint)((Int32.MaxValue - ASubtrahend) + AMinuend);
		}
		#endregion
	}
}
