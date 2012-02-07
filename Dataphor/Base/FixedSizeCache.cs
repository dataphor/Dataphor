/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

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
		public static readonly ProperFraction DefaultCutoff = new ProperFraction(0.33m);
		public const uint DefaultCorrelatedReferencePeriod = 30;

		/// <param name="size"> The size of the cache (in entries). </param>
		public FixedSizeCache(int size)
		{
			if (size < 2)
				throw new BaseException(BaseException.Codes.MinimumSize);
			Size = size;
			_entries = new Dictionary<TKey, Entry>(size);
			DefaultSettings();
		}

		private Dictionary<TKey, Entry> _entries;

		/// <summary> Logical time used to track recency of Entry usage. </summary>
		/// <remarks> Incremented with each Entry access (logical clock).  This may roll over. Note that this does not necessarily 
		/// indicate the relative index of a Entry in the LRU chain because there are multiple insertion points into the LRU. </remarks>
		private int _logicalTime;
				
		public int Size { get; private set; }

		/// <summary> The number of cache entries. </summary>
		public int Count
		{
			get { return _entries.Count; }
		}

		/// <summary> Gets or sets the settings for the cache. </summary>
		/// <remarks> Changing these settings will not affect the current state of the cache, but will be used for subsequent operations. </remarks>
		public FixedSizeCacheSettings Settings { get; set; }						

		/// <summary> Sets or resets the cache's settings to their defaults. </summary>
		public void DefaultSettings()
		{
			FixedSizeCacheSettings settings = new FixedSizeCacheSettings();   			
			settings.CorrelatedReferencePeriod = DefaultCorrelatedReferencePeriod;
			settings.Cutoff = DefaultCutoff; 			
			Settings = settings;
		}

		#region LRU Maintenance

		// TODO: Investigate splitting FCacheLatch into LRU and FFrames latches

		public class Entry
		{
			internal Entry(int lLogicalCreationTime)
			{
				_lastAccess = lLogicalCreationTime;
			}
			internal TKey _key;
			public TKey Key { get { return _key; } }
			internal TValue _value;
			public TValue Value { get { return _value; } }
			internal Entry _next;
			internal Entry _prior; 			
			internal bool _preCutoff;
			internal int _lastAccess;	 
		}

		/// <summary> Latch used to protect the LRU chain as well as the FFrames table. </summary>
		private object _cacheLatch = new object();

		/// <summary> Pointer to the head of the LRU chain. </summary>
		private Entry _lRUHead;
		/// <summary> Pointer to the cuttoff point within the LRU chain. </summary>
		private Entry _lRUCutoff;
		/// <summary> Pointer to the tail of the LRU chain. </summary>
		private Entry _lRUTail;
		/// <summary> The number of entries that occur before (exclusive of) the cutoff entry. </summary>
		private int _lRUPreCutoffCount;
		/// <summary> The total number of entries in the LRU chain. </summary>
		private int _lRUCount;

		/// <summary> Initializes the list. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynInitializeLRU(Entry entry)
		{
			_lRUCutoff = entry;
			_lRUHead = entry;
			_lRUTail = entry;
			entry._prior = null;
			entry._next = null;
			entry._preCutoff = false;
			_lRUCount = 1;
		}

		/// <summary> Detaches the Entry from the list. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynDetach(Entry entry)
		{ 					
			if (entry._prior != null)
				entry._prior._next = entry._next;

			if (entry._next != null)
				entry._next._prior = entry._prior;

			if (entry == _lRUHead)
				if (entry._prior != null)
					_lRUHead = entry._prior;
				else
					_lRUHead = null;

			if (entry == _lRUCutoff)
				if (entry._prior != null)
				{
					_lRUCutoff = entry._prior;	 		
				}
				else 
				{
					if (entry._next != null)
						SynShiftCutoff(-1);
					else
						_lRUCutoff = null;
				}

			if (entry == _lRUTail)
				if (entry._next != null)
				{
					if (_lRUTail._next == _lRUCutoff && _lRUCutoff._next != null)
					{
						_lRUCutoff = _lRUCutoff._next;
						SynShiftCutoff(-1);
					}
					_lRUTail = entry._next;
				}
				else
					_lRUTail = null;   
					
			entry._prior = null;
			entry._next = null;						
		}

		/// <summary> Places the entry at the head of the LRU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynPlaceAtHead(Entry entry)
		{ 			 						
			SynDetach(entry);
			_lRUHead._next = entry;
			entry._prior = _lRUHead; 		
			if (_lRUHead == _lRUCutoff)	 			
				_lRUCutoff = entry;  		
		
			if (entry._preCutoff == false && _lRUHead._preCutoff == true)
			{
				entry._preCutoff = true;  
				_lRUPreCutoffCount++;
			}						
			_lRUHead = entry;									
			SynUpdateCutoff();	 			
		}

		/// <summary> Places the entry at the cutoff point of the LRU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynPlaceAtCutoff(Entry entry)
		{
			if (_lRUHead == null)
			{
				SynInitializeLRU(entry);
				return;
			}

			entry._preCutoff = false;		
			entry._prior = _lRUCutoff;
			entry._next = _lRUCutoff._next;
			if (_lRUCutoff._next != null)
				_lRUCutoff._next._prior = entry;
			_lRUCutoff._next = entry;				
			if (_lRUCutoff == _lRUHead)
				_lRUHead = entry;
			_lRUCutoff = entry;			
			SynUpdateCutoff(); 							
		}  		 

		/// <summary> Removes the specified entry from the FLU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynLRURemove(Entry entry)
		{
			SynDetach(entry);			
			SynAdjustEntryCount(-1, entry._preCutoff);
			SynUpdateCutoff();
		}

		/// <summary> Maintains the total and cutoff LRU counts. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynAdjustEntryCount(int delta, bool preCutoff)
		{
			_lRUCount += delta;
			if (preCutoff)
				_lRUPreCutoffCount += delta;
		}

		/// <summary> Shifts the cutoff point to the point configured in the settings. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynUpdateCutoff()
		{
			SynShiftCutoff((_lRUCount * Settings.Cutoff) - _lRUPreCutoffCount);
		}

		/// <summary> Adjusts the cutoff point by the given delta. </summary>
		/// <remarks> This method assumes that the given delta will not adjust the cutoff point off of 
		/// the edge of the list.
		/// Locks-> Expects: FCacheLatch </remarks>
		private void SynShiftCutoff(int delta)
		{
			while (delta > 0)
			{  				
				_lRUCutoff._preCutoff = true;
				_lRUCutoff = _lRUCutoff._prior;			
				delta--;
				_lRUPreCutoffCount++;
			}
			while (delta < 0)
			{
				_lRUCutoff = _lRUCutoff._next; 		
				_lRUCutoff._preCutoff = false;
				delta++;
				_lRUPreCutoffCount--;
			}
		}

		/// <summary> Clears the LRU. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void SynLRUClear()
		{
			_lRUHead = null;
			_lRUCutoff = null;
			_lRUTail = null;
			_lRUPreCutoffCount = 0;
			_lRUCount = 0;
		}  	

		#endregion

		/// <summary> Submits a reference to the cache to be added or reprioritized. </summary>
		/// <param name="value"> The item to be added to the cache. </param>
		/// <returns> The value that was removed because the available size was completely allocated, or null if no item was removed. </returns>
		public TValue Reference(TKey key, TValue value)
		{
			TValue result = default(TValue);
			Entry entry; 

			// Increment the logical clock and remember the logical time of this access
			int logicalTime = Interlocked.Increment(ref _logicalTime);			
			_entries.TryGetValue(key, out entry);
			if (entry != null)
			{ 				
				if (SubtractTime(logicalTime, entry._lastAccess) > Settings.CorrelatedReferencePeriod)   			
				{		
				    if (entry != _lRUHead)
				        SynPlaceAtHead(entry); 									
				    entry._lastAccess = logicalTime;	
				}  						 			
			}
			else
			{							
				// If the list is full, remove and re-use the oldest; otherwise create a new entry					
				if (_entries.Count >= Size)
				{
					_entries.Remove(_lRUTail._key); 
					result = _lRUTail._value;
					entry = _lRUTail; 
					_lRUTail = _lRUTail._next;
					_lRUTail._prior = null;						 									
					entry._lastAccess = logicalTime; 											
				}
				else
				{
					entry = new Entry(logicalTime);
					if (_lRUHead != null) 												
						SynAdjustEntryCount(1, false);									     					 					
				}				
				entry._key = key;	  				
				SynPlaceAtCutoff(entry);					 
				_entries.Add(key, entry); 																			
			}

			entry._value = value;	
			return result;	 
		}

		#region IDictionary Members

		public bool TryGetValue(TKey key, out TValue value)
		{
			Entry entry;
			if (_entries.TryGetValue(key, out entry))
			{
				value = entry._value;
				return true;
			}
			else
			{
				value = default(TValue);
				return false;
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return ContainsKey(item.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		
		/// <remarks> Avoid this overload as it must allocate a KeyValuePair for each result. </remarks>
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			foreach (KeyValuePair<TKey, Entry> entry in _entries)
				yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value._value);
		}
					
		public TValue this[TKey key]
		{
			get
			{
				Entry entry;
				if (_entries.TryGetValue(key, out entry))
					return entry._value;
				else
					return default(TValue);
			}
			set
			{
				// Remove the old entry if it exists
				Entry entry;  				
				if (_entries.TryGetValue(key, out entry))	
				{
					_entries.Remove(key);
					SynLRURemove(entry);	
				}				
			
				// Add the new one if it is not null
				if (value != null)
					Add(key, value); 				
			}
		}

		public bool ContainsKey(TKey key)
		{
			return _entries.ContainsKey(key);
		}

		/// <summary> Adds the specified key/value to the cache. </summary>
		/// <remarks> Note that this may remove another item.  To determine what item is removed upon 
		///	entry, use Reference rather than Add. </remarks>
		public void Add(TKey key, TValue value)
		{
			Reference(key, value);
		}

		/// <summary> Removes the specified key from the cache. </summary>
		public bool Remove(TKey key)
		{
			Entry entry;
			if (_entries.TryGetValue(key, out entry))
			{
				SynLRURemove(entry);
				_entries.Remove(key);
				return true;
			}
			else
				return false;
		}

		/// <summary> Clears the cache of all entries. </summary>
		public void Clear()
		{	  			
			_entries.Clear();
			SynLRUClear(); 		
		}

		public ICollection<TKey> Keys
		{
			get { return _entries.Keys; }
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
			foreach (KeyValuePair<TKey, Entry> entry in _entries)
				yield return entry.Value;
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			foreach (KeyValuePair<TKey, Entry> entry in _entries)
				yield return entry.Value._value;
		}

		#endregion

		#region Static Utilities

		/// <summary> Subtracts one logical time from another accounting for rollover. </summary>
		private static uint SubtractTime(int minuend, int subtrahend)
		{
			long result = minuend - subtrahend;
			if (result >= 0)
				return (uint)result;
			else
				return (uint)((Int32.MaxValue - subtrahend) + minuend);
		}
		#endregion
	}
}
