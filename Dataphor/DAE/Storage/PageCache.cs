/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Threading;
using System.Text;

namespace Alphora.Dataphor.DAE.Storage
{
	/*
		Page Cache:
		
		Interface ->
			-Page size must be fixed per PageCache
			
		Page Pool Implementation ->
			-The page pool is composed of:
				-Doubly-linked page frames with head/tail/cutoff pointer - only for non-fixed pages
				-Hashtable of page frames by ID - containing all pages involved in any way
			-A logical clock is incremented with each page access and each frame has the logical time it was last accessed
			-When an access is made:
				if a page frame is found: 
					if the frame's logical time is not very recent (within the correllated reference period), or the frame is above the cutoff:
						the frame is moved to the head of the linked list
					else
						the frame is brought up to the cutoff position
				else
					if the list is at capacity:
						a replacement victim is located and removed (see replacement selection)
					a frame is added to the cutoff point of the list
				TODO: Possible optimization: Remember the most recent time that the buffer was moved (as opposed to last accessed) and only move after a larger period
			-Replacement Selection:
				The last n frames (starting from the tail) are scanned for the first non-fixed, non-dirty occurrence
				If none are located, the first non-fixed frame (starting from the tail) is taken
				If none are located, the buffer is forcibly expanded
			-PreCutoff flag:
				-Each frame contains a PreCutoff flag
				-The PreCutoff flag is set for all frames from the head (inclusive) to the cutoff point (exclusive).
					-The flag is set when (if) the frame is placed at the head of the list (because the frame was found in the list) or when the cutoff point is moved forward
					-The flag is cleared for the item pointed to by the cutoff link (as it is adjusted) and thereby clear for all frames that follow
				-When an item is removed from the list (for replacement or promotion) the PreCutoff flag incidates whether the removal affects the cutoff link's offset
			-The cutoff point is a configurable fraction of the entire buffer space
				Q: Could the logical age of the frames possibly be used for automatic cutoff point determination?
			-Buffer management:
				-Free page frames are maintained using a single-linked list formed using the existing Prior pointer within the PageFrame and a special free frame anchor
				-Buffer growth causes a new buffer extent allocation and the construction of a frame for each page within the new extent.  The free frame anchor is then set to this new chain.
			-Locking/Latching strategy:
				

		// TODO: Add Prefetch Capability (w/ scatter/gather and page sorting)

	*/

//	/// <summary> Manages the buffered access to pages. </summary>
//	public class PageCache : Disposable
//	{
//		// Settings defaults
//		public const uint CDefaultMaximumFrames = 0x2000;
//		public const uint CDefaultMaximumWriters = 8;
//		public const uint CDefaultGrowBy = 0x200;
//		public static readonly ProperFraction CDefaultCutoff = new ProperFraction(0.33m);
//		public const uint CDefaultCorrelatedReferencePeriod = 30;
//		public const uint CDefaultMaximumSingleWritePages = 16;
//		public const int CDefaultMaximumUnmodifiedVictimScan = 50;	// TUNING: Scan this number of frames for an unmodified, unfixed buffer (beyond this, just take the first unfixed frame we found)
//
//		#region Constructor and Dispose
//
//		/// <summary> Initializes a new page cache of the given size and using the given FileGroup. </summary>
//		/// <remarks> This class does not take ownership of the given FileGroup (and therefore does not dispose it). </remarks>
//		public PageCache(FileGroup AFileGroup, int APageSize)
//		{
//			FPageSize = APageSize;
//			FFileGroup = AFileGroup;
//			DefaultSettings();
//			FFrames = new Hashtable((int)FSettings.GrowBy);
//			FBuffer = new PageBuffer(FPageSize);
//		}
//
//		protected override void Dispose(bool ADisposing)
//		{
//			try
//			{
//				FlushAll();	// The recovery manager should have already checkpointed by now, so this shouldn't be necessary.  But...
//			}
//			finally
//			{
//				try
//				{
//					FBuffer.Dispose();
//				}
//				finally
//				{
//					base.Dispose(ADisposing);
//				}
//			}
//		}
//
//		#endregion
//
//		#region Properties and Statistics
//
//		private int FPageSize;
//		/// <summary> Gets the page size for this cache. </summary>
//		public int PageSize { get { return FPageSize; } }
//
//		private FileGroup FFileGroup;
//		/// <summary> Gets the data files manager for this cache. </summary>
//		public FileGroup FileGroup
//		{
//			get { return FFileGroup; }
//		}
//
//		private PageCacheSettings FSettings;
//		/// <summary> Gets or sets the settings for the cache. </summary>
//		/// <remarks> Changing these settings will not affect the current state of the cache, but will be used for subsequent operations. </remarks>
//		public PageCacheSettings Settings 
//		{ 
//			get { return FSettings; }
//			set
//			{
//				if (value.GrowBy <= 0)
//					throw new StorageException(StorageException.Codes.GrowByMustBeGreaterThanZero);
//				if (value.MaximumSingleWritePages < 1)
//					throw new StorageException(StorageException.Codes.InvalidMaximumSingleWritePages);
//				lock (FWriterLatch)
//				{
//					if ((FPageArrayPool == null) || (FSettings.MaximumSingleWritePages != value.MaximumSingleWritePages) || (FSettings.MaximumWriters != value.MaximumWriters))
//					{
//						FPageArrayPool = new IntPtr[value.MaximumSingleWritePages, value.MaximumWriters];
//						FFreePageArrayCount = Math.Max(0, FFreePageArrayCount + (value.MaximumWriters - FSettings.MaximumWriters));
//					}
//
//					FSettings = value;
//					FShrinkerSignal.Set();	// Awaken the writer deamon in case there is buffer reduction work to be done
//				}
//			}
//		}
//
//		/// <summary> Sets or resets the cache's settings to their defaults. </summary>
//		public void DefaultSettings()
//		{
//			PageCacheSettings LSettings = new PageCacheSettings();
//			LSettings.MaximumFrames = CDefaultMaximumFrames;
//			LSettings.GrowBy = CDefaultGrowBy;
//			LSettings.CorrelatedReferencePeriod = CDefaultCorrelatedReferencePeriod;
//			LSettings.Cutoff = CDefaultCutoff;
//			LSettings.MaximumWriters = CDefaultMaximumWriters;
//			LSettings.MaximumSingleWritePages = CDefaultMaximumSingleWritePages;
//			LSettings.MaximumUnmodifiedVictimScan = CDefaultMaximumUnmodifiedVictimScan;
//			Settings = LSettings;
//		}
//
//		private int FHitCount;
//		/// <summary> The number of hits (page references found in the cache) that have occurred since the cache was started. </summary>
//		public int HitCount { get { return FHitCount; } }
//
//		private int FMissCount;
//		/// <summary> The number of misses (page references not found in the cache and loaded from the disk) that have occurred since the cache was started. </summary>
//		public int MissCount { get { return FMissCount; } }
//
//		#endregion
//
//		#region Page Fixing and Unfixing
//
//		/// <summary> Logical time used to track recency of page frame usage. </summary>
//		/// <remarks> Incremented with each page access (logical clock).  This may roll over. Note that this does not necessarily 
//		/// indicate the relative index of a frame in the LRU chain because there are multiple insertion points into the LRU. </remarks>
//		private int FLogicalTime;
//
//		/// <summary> Table of frame's by PageID. </summary>
//		private Hashtable FFrames;
//
//		/// <summary> Acquires a fix on the specified page, as well as the specified lock type. </summary>
//		/// <remarks> Fixing a page will find or read in the page and keep it in the buffer until the page is unfixed.  
//		/// This call is counted, and the page remains fixed until an unfix has been made for every fix. </remarks>
//		public PageFrame Fix(PageID APageID, LockType ALockType)
//		{
//			return InternalFix(APageID, ALockType, true);
//		}
//
//		/// <summary> Acquires a fix on the specified page, as well as the specified lock type, but does not request that the page be read if it isn't already in the cache. </summary>
//		/// <remarks> Fixing a page will find or initialize the page and keep it in the buffer until the page is unfixed.  
//		/// This call is counted, and the page remains fixed until an unfix has been made for every fix.  This method is like Fix, except 
//		/// that no attempt will be made to read the page if it is not located in the cache. </remarks>
//		public PageFrame EmptyFix(PageID APageID, LockType ALockType)
//		{
//			return InternalFix(APageID, ALockType, false);
//		}
//
//		private PageFrame InternalFix(PageID APageID, LockType ALockType, bool APerformRead)
//		{
//			PageFrame LFrame;
//
//			// Increment the logical clock and remember the logical time of this access
//			int LLogicalTime = Interlocked.Increment(ref FLogicalTime);
//
//			for (;;)	// Restart point
//			{
//				Monitor.Enter(FCacheLatch);
//				LFrame = (PageFrame)FFrames[APageID];
//				if (LFrame != null)
//				{
//					if (!Monitor.TryEnter(LFrame))
//					{
//						Monitor.Exit(FCacheLatch);
//						continue;
//					}
//					try
//					{
//						try
//						{
//							if (((LFrame.FFlags & PageFrameFlags.Replacing) != 0) && (LFrame.PageID != APageID)) // This page has been choosen as a replacement victim
//								continue;
//							if (LFrame.FFixCount == 0)
//								Remove(LFrame);						// Remove from the LRU list until last unfix
//						}
//						finally
//						{
//							Monitor.Exit(FCacheLatch);
//						}
//						LFrame.FFixCount++;							// Fix the page now before releasing the frame latch
//						if (SubtractTime(LLogicalTime, LFrame.FLastAccess) > FSettings.CorrelatedReferencePeriod)
//							LFrame.FFlags |= PageFrameFlags.PreCutoff;	// Incidate that this page was found in the buffer (it is "popular")
//						LFrame.FLastAccess = LLogicalTime;
//					}
//					finally
//					{
//						Monitor.Exit(LFrame);
//					}
//
//					Interlocked.Increment(ref FHitCount);
//				}
//				else
//				{
//					LFrame = LocateFrame();
//					if (!Monitor.TryEnter(LFrame))
//					{
//						Monitor.Exit(FCacheLatch);
//						continue;
//					}
//					LFrame.FFixCount++;
//
//					Interlocked.Increment(ref FMissCount);
//
//					PlaceAtCutoff(LFrame);
//					LFrame.FIsModified = false;
//					LFrame.FPageID = APageID;
//					if (APerformRead)
//						FFileGroup.ReadPage(LFrame.FPage, APageID);
//					FFrames.Add(APageID, LFrame);
//				}
//				break;
//			}
//			switch (ALockType)
//			{
//				case LockType.Read : LFrame.FLock.AcquireReaderLock(Timeout.Infinite); break;
//				case LockType.Write : LFrame.FLock.AcquireWriterLock(Timeout.Infinite); break;
//			}
//			return LFrame;
//		}
//
//		/// <summary> Unfixes (dereferences) the specified page frame, and releases the specified lock. </summary>
//		public void Unfix(PageFrame AFrame, LockType AReleaseType)
//		{
//			for (;;)
//			{
//				Monitor.Enter(AFrame);
//				if (AFrame.FFixCount == 1)
//				{
//					if (!Monitor.TryEnter(FCacheLatch))
//					{
//						Monitor.Exit(AFrame);
//						continue;
//					}
//					AFrame.FFixCount--;
//					if (LFrame.FPreCutoff)
//						PlaceAtHead(LFrame);
//					else
//						PlaceAtCutoff(LFrame);
//				}
//				switch (AReleaseType)
//				{
//					case LockType.Read : AFrame.FLock.ReleaseReaderLock(); break;
//					case LockType.Write : AFrame.FLock.ReleaseWriterLock(); break;
//				}
//			}
//		}
//
//		#endregion
//
//		#region LRU Maintenance
//
//		// TODO: Investigate splitting FCacheLatch into LRU and FFrames latches
//
//		/// <summary> Latch used to protect the LRU chain as well as the FFrames table. </summary>
//		private object FCacheLatch = new object();
//
//		/// <summary> Pointer to the head of the LRU chain. </summary>
//		private PageFrame FLRUHead;
//		/// <summary> Pointer to the cuttoff point within the LRU chain. </summary>
//		private PageFrame FLRUCutoff;
//		/// <summary> Pointer to the tail of the LRU chain. </summary>
//		private PageFrame FLRUTail;
//		/// <summary> The number of frames that occur before (exclusive of) the cutoff frame. </summary>
//		private int FLRUPreCutoffCount;
//		/// <summary> The total number of frames in the LRU chain. </summary>
//		private int FLRUCount;
//
//		/// <summary> Places the frame at the head of the LRU chain. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
//		private void PlaceAtHead(PageFrame AFrame)
//		{
//			AFrame.FPrior = FLRUHead;
//			AFrame.FNext = null;
//			if (FLRUHead == null)
//			{
//				AFrame.FPreCutoff = false;
//				FLRUHead = AFrame;
//				FLRUTail = AFrame;
//				FLRUCutoff = AFrame;
//				AdjustFrameCount(1, false);
//			}
//			else
//			{
//				AFrame.FPreCutoff = true;
//				FLRUHead.FNext = AFrame;
//				FLRUHead = AFrame;
//				AdjustFrameCount(1, true);
//			}
//			UpdateCutoff();
//		}
//
//		/// <summary> Places the frame at the cutoff point of the LRU chain. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
//		private void PlaceAtCutoff(PageFrame AFrame)
//		{
//			if (FLRUCutoff == null)
//			{
//				FLRUCutoff = AFrame;
//				FLRUHead = AFrame;
//				FLRUTail = AFrame;
//				AFrame.FPrior = null;
//				AFrame.FNext = null;
//			}
//			else
//			{
//				if (FLRUCutoff.FNext != null)
//					FLRUCutoff.FNext.FPrior = AFrame;
//				AFrame.FNext = FLRUCutoff.FNext;
//				AFrame.FPrior = FLRUCutoff;
//				FLRUCutoff.FNext = AFrame;
//				if (FLRUCutoff == FLRUHead)
//					FLRUHead = AFrame;
//				FLRUCutoff = AFrame;
//			}
//			AFrame.FPreCutoff = false;
//			AdjustFrameCount(1, false);
//			UpdateCutoff();
//		}
//
//		/// <summary> Removes the specified frame from the FLU chain. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
//		private void Remove(PageFrame AFrame)
//		{
//			if (AFrame.FPrior != null)
//				AFrame.FPrior.FNext = AFrame.FNext;
//			if (AFrame.FNext != null)
//				AFrame.FNext.FPrior = AFrame.FPrior;
//			if (AFrame == FLRUHead)
//				FLRUHead = AFrame.FPrior;
//			if (AFrame == FLRUCutoff)
//				if (AFrame.FPrior != null)
//					FLRUCutoff = AFrame.FPrior;
//				else
//				{
//					if (AFrame.FNext != null)
//						ShiftCutoff(-1);
//					else
//						FLRUCutoff = null;
//				}
//			if (AFrame == FLRUTail)
//				FLRUTail = AFrame.FNext;
//			AdjustFrameCount(-1, AFrame.FPreCutoff);
//			UpdateCutoff();
//		}
//
//		/// <summary> Maintains the total and cutoff LRU counts. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
//		private void AdjustFrameCount(int ADelta, bool APreCutoff)
//		{
//			FLRUCount += ADelta;
//			if (APreCutoff)
//				FLRUPreCutoffCount += ADelta;
//		}
//
//		/// <summary> Shifts the cutoff point to the point configured in the settings. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
//		private void UpdateCutoff()
//		{
//			ShiftCutoff((FLRUCount * FSettings.Cutoff) - FLRUPreCutoffCount);
//		}
//
//		/// <summary> Adjusts the cutoff point by the given delta. </summary>
//		/// <remarks> This method assumes that the given delta will not adjust the cutoff point off of 
//		/// the edge of the list.
//		/// Locks-> Expects: FCacheLatch </remarks>
//		private void ShiftCutoff(int ADelta)
//		{
//			while (ADelta > 0)
//			{
//				FLRUCutoff.FPreCutoff = true;
//				FLRUCutoff = FLRUCutoff.FPrior;
//				ADelta--;
//				FLRUPreCutoffCount++;
//			}
//			while (ADelta < 0)
//			{
//				FLRUCutoff = FLRUCutoff.FNext;
//				FLRUCutoff.FPreCutoff = false;
//				ADelta++;
//				FLRUPreCutoffCount--;
//			}
//		}
//
//		#endregion
//
//		#region Frame Aquisition, Releasing, and Replacement
//
//		/// <summary> Latch used to protect the free list. </summary>
//		private object FFreeLatch = new Object();
//
//		/// <summary> The anchor of the free frame chain (a singly-linked list)</summary>
//		private PageFrame FFreeHead;
//
//		/// <summary> The number of frames in the free frame chain. </summary>
//		private int FFreeCount;
//
//		/// <summary> The page buffer. </summary>
//		private PageBuffer FBuffer;
//
//		/// <summary> Makes a frame available through replacement or acquisition. </summary>
//		/// <remarks> Caller should have a latch on FCacheLatch. 
//		/// Locks-> Expects: FCacheLatch; Takes: LFrame; Leaves: LFrame </remarks>
//		private PageFrame LocateFrame()
//		{
//			PageFrame LFrame;
//			if (FLRUCount >= FSettings.MaximumFrames)
//			{
//				LFrame = FindVictim();
//				if (LFrame != null)
//				{
//					Remove(LFrame);
//					FFrames.Remove(LFrame.FPageID);
//				}
//				else
//					LFrame = AcquireFrame();
//			}
//			else
//				LFrame = AcquireFrame();
//			return LFrame;
//		}
//
//		/// <summary> Finds and removes a page replacement victim. </summary>
//		/// <remarks> Locks-> Expects: FCacheLatch; Takes: LFrame; Leaves: none </remarks>
//		private PageFrame FindVictim()
//		{
//			PageFrame LBest = null;
//			PageFrame LFrame = FLRUTail;
//			int LCount = 0;
//
//			// Scan up to n frames from the tail until an unmodified page is found
//			while ((LFrame != null) && (LCount < FSettings.MaximumUnmodifiedVictimScan))
//			{
//				LCount++;
//				if (!Monitor.TryEnter(LFrame))
//					continue;
//				if (LBest != null)
//					Monitor.Exit(LBest);
//				LBest = LFrame;
//				if ((LFrame.FFlags & PageFrameFlags.Modified) == 0)
//					break;
//				LFrame = LFrame.FNext;
//			}
//			
//			if (LBest != null)
//			{
//				if ((LBest.FFlags & PageFrameFlags.Modified) != 0)
//				{
//					LBest.FFixCount = 1;
//					if ((LBest.FFlags & PageFrameFlags.Writing) == 0)
//						WritePage(LBest);
//				}
//				if ((LBest.FFlags & PageFrameFlags.Modified) != 0)
//					lock(FWriterList.SyncRoot)
//					{
//						FWriterList.
//						InternalFlush(LBest);
//					}
//			}
//
//			return LBest;
//		}
//
//		/// <summary> Obtains an unused frame from the list of free frames (allocating more if necessary). </summary>
//		/// <remarks> Locks-> Expects: none; Takes: FFreeLatch; Leaves: none </remarks>
//		private PageFrame AcquireFrame()
//		{
//			PageFrame LFrame = null;
//			lock (FFreeLatch)
//			{
//				if (FFreeHead == null)
//				{
//					// Allocate a new extent and assocate a PageFrame with each page within the extent
//					int LNewPages = (int)FSettings.GrowBy;		// Copy this value as we do not want it changing as we go
//					IntPtr LData = FBuffer.Allocate(LNewPages);
//					try
//					{
//						PageFrame LOldFrame = null;
//						for (int i = LNewPages - 1; i >= 0; i--)
//						{
//							LFrame = new PageFrame();
//							LFrame.FPrior = LOldFrame;
//							LFrame.FPage = (IntPtr)((uint)LData + (i * FPageSize));
//							LOldFrame = LFrame;
//						}
//					}
//					catch
//					{
//						FBuffer.Deallocate();
//						throw;
//					}
//					FFreeHead = LFrame;
//					FFreeCount = LNewPages;
//				}
//				else
//					LFrame = FFreeHead;
//				FFreeHead = FFreeHead.FPrior;
//				FFreeCount--;
//			}
//			return LFrame;
//		}
//
//		/// <summary> Releases a no longer used frame back to the list of unused frames. </summary>
//		/// <remarks> Locks-> Expects: none; Takes: FFreeLatch; Leaves: none </remarks>
//		private void ReleaseFrame(PageFrame AFrame)
//		{
//			lock (FFreeLatch)
//			{
//				AFrame.FPrior = FFreeHead;
//				FFreeHead = AFrame;
//				FFreeCount++;
//			}
//		}
//
//		#endregion
//
//		#region Page Writing
//
//		/// <summary> Synchronously flushes all modified pages. </summary>
//		public void FlushAll()
//		{
//			FWriterSignal.WaitOne();
//		}
//
//		// TODO: Replace the SortedList with something more scalable such as a B+Tree
//
//		/// <summary> Latch used to protect the writer list and accompanying data. </summary>
//		public Object FWriterLatch = new Object();
//
//		/// <summary> A queue of modified page frames. </summary>
//		/// <remarks> This list only contains pages that need to be, but are not yet being written. </remarks>
//		private IndexList FWriterList = new IndexList();
//
//		/// <summary> Signal used to indicate that there are modified pages. </summary>
//		/// <remarks> This is set unless all pages are written and no longer modified. </remarks>
//		private ManualResetEvent FWriterSignal = new ManualResetEvent(false);
//
//		/// <summary> The number of currently active writers. </summary>
//		private int FWriterCount;
//
//		/// <summary> The page ID of the last page for which writing was started. </summary>
//		private PageID FLastWrittenPage;
//
//		/// <summary> The direction of the sweeping of the writer list. </summary>
//		private int FWriterDirection = 1;
//
//		/// <summary> Marks the specified page frame as modified and makes it eligable for writing. </summary>
//		/// <remarks> This should be called only after making a change to the buffer and while an exclusive lock is held on the frame. </remarks>
//		public void Modified(PageFrame AFrame)
//		{
//			int LBecomingModified;
//			lock (AFrame)
//			{
//				LBecomingModified = ((AFrame.FFlags & PageFrameFlags.Modified) == 0);
//				AFrame.FFlags |= PageFrameFlags.Modified;
//			}
//
//			if (LBecomingModified)
//			{
//				lock (FWriterLatch)
//					FWriterList.Add(AFrame.PageID, AFrame);
//				StartWriters();
//			}
//		}
//
//		private IntPtr[][] FPageArrayPool;
//		private int FFreePageArrayCount;
//
//		private IntPtr[] AcquirePageArray()
//		{
//			FFreePageArrayCount--;
//			return FPageArrayPool[FFreePageArrayCount];
//		}
//
//		private void RelinquishPageArray(IntPtr[] AArray)
//		{
//			// Only return the page array to the pool if the sizes are the same (in case of a resize)
//			if ((AArray.Length == FSettings.MaximumSingleWritePages) && (FFreePageArrayCount < FPageArrayPool.Length))
//			{
//				FPageArrayPool[FFreePageArrayCount] = AArray;
//				FFreePageArrayCount++;
//			}
//		}
//
//		/// <summary> Starts up to the max desired writes if there are any pending. </summary>
//		/// <remarks> Locks-> Expects: FWriterList; </remarks>
//		private void StartWriters()
//		{
//			Monitor.Enter(FWriterLatch);
//			while ((FWriterCount < FSettings.MaximumWriters) && (FWriterList.Count > 0))
//			{
//				PageID LPageID = FLastWrittenPage + FWriterDirection;
//				object LNearest;
//				FWriterList.Find(LPageID, FWriterDirection > 0, out LNearest);
//				if (LNearest == null)
//				{
//					FWriterDirection *= -1;
//					FWriterList.Find(LPageID, FWriterDirection > 0, out LNearest);
//				}
//				Error.DebugAssertFail(LNearest != null, "Unable to find a page in the FWriterList");
//
//				PageFrame LFrame = (PageFrame)LNearest;
//				LPageID = LFrame.PageID;
//
//				if (FSettings.MaximumSingleWritePages > 1)
//				{
//					IntPtr[] LPages = AcquirePageArray();
//					int LCount = 1;
//					PageID LMin = LPageID;
//					LPages[0] = LFrame.Page;
//					// Look for a contiguous page
//					LPageID += FWriterDirection;
//					while (FWriterList.Find(LPageID, FWriterDirection > 0, out LNearest))
//					{
//						if (LPageID < LMin)
//							LMin = LPageID;
//						LPages[LCount] = ((PageFrame)LNearest).FPage;
//						if (BeforeWritePage != null)
//							BeforeWritePage(this, LPages[LCount]);	// Hook for LSN flush
//						LCount++;
//						LPageID += FWriterDirection;
//					}
//					// Terminate the list with a null
//					if (LCount < LPages.Length)
//						LPages[LCount] = null;
//					FFileGroup.BeginWritePages(LPages, LCount, LMin, new AsyncCallback(MultiWriterCallback), LPages);
//				}
//				else
//				{
//					if (BeforeWritePage != null)
//						BeforeWritePage(this, LFrame);	// Hook for LSN flush
//					FFileGroup.BeginWritePage(LFrame.FPage, LFrame.FPageID, new AsyncCallback(WriterCallback), LFrame);
//				}
//			}
//		}
//
//		private void WritePage(PageFrame AFrame)
//		{
//			Monitor.Enter(FWriterList.SyncRoot);
//			FWriterList.Remove(AFrame.PageID);
//			Monitor.Exit(FWriterList.SyncRoot);
//
//			if (BeforeWritePage != null)
//				BeforeWritePage(this, AFrame);	// Hook for LSN flush
//			FFileGroup.BeginWritePage(AFrame.FPage, AFrame.FPageID, new AsyncCallback(WriterCallback), AFrame);
//			LFrame.FFlags |= PageFrameFlags.Writing;
//		}
//
//		public event PageFrameHandler BeforeWritePage;
//
//		private void WriterCallback(IAsyncResult AResult)
//		{
//			PageFrame LFrame = (PageFrame)AResult.AsyncState;
//			lock(LFrame)
//			{
//				LFrame.FFlags &= ~(PageFrameFlags.Writing | PageFrameFlags.Modified);
//
//			}
//		}
//
//		private void MultiWriterCallback(IAsyncResult AResult)
//		{
//			AResult.AsyncState;
//		}
//
//		#endregion
//
//		#region Shrinker
//
//		/// <summary> Shrinker deamon thread. </summary>
//		private Thread FShrinkerDeamon = new Thread(new ThreadStart(ShrinkerDeamon));
//
//		/// <summary> Signal to incidate that the Writer Daemon is to be active. </summary>
//		private ManualResetEvent FShrinkerSignal = new ManualResetEvent(false);
//
//		public void ShrinkerDeamon()
//		{
//			for (;;)
//			{
//				// Do nothing until awoken
//				FShrinkerSignal.WaitOne();
//				// TODO: Shrinking
//				FShrinkerSignal.Reset();
//			}		
//		}
//
//		#endregion
//
//		#region Static Utilities
//
//		/// <summary> Acquires a lock with no timeout (immediate) and returns true if said lock was successful. </summary>
//		private static bool AcquireReaderLockBounce(ReaderWriterLock ALock)
//		{
//			try
//			{
//				ALock.AcquireReaderLock(0 /* immediate */);
//				return true;
//			}
//	        catch (ApplicationException)
//			{				
//				return false;					  
//			}
//		}
//
//		/// <summary> Acquires a lock with no timeout (immediate) and returns true if said lock was successful. </summary>
//		private static bool AcquireWriterLockBounce(ReaderWriterLock ALock)
//		{
//			try
//			{
//				ALock.AcquireWriterLock(0 /* immediate */);
//				return true;
//			}
//	        catch (ApplicationException)
//			{				
//				return false;					  
//			}
//		}
//
//		/// <summary> Subtracts one logical time from another accounting for rollover. </summary>
//		private static uint SubtractTime(int AMinuend, int ASubtrahend)
//		{
//			long LResult = AMinuend - ASubtrahend;
//			if (LResult >= 0)
//				return (uint)LResult;
//			else
//				return (uint)((Int32.MaxValue - ASubtrahend) + AMinuend);
//		}
//
//		private static void SafeLatch(object AObject)
//		{
//			if (!Monitor.TryEnter(AObject))
//				throw new RestartException();
//		}
//
//		#endregion
//	}
//
//	[Flags]
//	public enum PageFrameFlags : byte
//	{
//		/// <summary> Modified is set when the page first is modified (before the exclusive lock on the frame 
//		/// is released) and is released after the page has been intirely written to disk. </summary>
//		Modified = 1,
//
//		/// <remarks> While the page is in the LRU chain, PreCutoff indicates that the the frame is found before 
//		/// the cutoff point (exclusive). When the frame is not in the LRU chain (frame is fixed), this indicates 
//		/// that the page was in the cache (outside of the correllated reference period) when fixed.  In other 
//		/// words this indicates that the page is "popular" and should be placed at the head of the list rather
//		/// than at the cutoff point. </remarks>
//		PreCutoff = 2,
//
//		/// <summary> Indicates that the page is currently being read. </summary>
//		Reading = 3,
//
//		/// <summary> Indicates that the page is currently being written. </summary>
//		Writing = 4,
//
//		/// <summary> Indicates that the frame has been replaced and a fix attempt should look at the page ID to 
//		/// see whether this page is being replaced or is replacing the victim. </summary>
//		Replacing = 8
//	}
//
//	/// <summary> Manages a single page's buffer. </summary>
//	/// <remarks> Acts as both an access control block and a buffer control block. </remarks>
//	public class PageFrame : IPageAddressing
//	{
//		internal PageID FPageID;
//		public PageID PageID { get { return FPageID; } }
//
//		internal IntPtr FPage;
//		public IntPtr Page { get { return FPage; } }
//
//		internal PageFrameFlags FFlags;
//		public bool IsModified { get { return (FFlags & PageFrameFlags.Modified) != 0; } }
//
//		internal bool FPreCutoff;
//		internal int FLastAccess;
//		internal PageFrame FNext;
//		internal PageFrame FPrior;
//		internal int FFixCount;
//
//		// internal LSN FFormInLSN;
//		internal ReaderWriterLock FLock = new ReaderWriterLock();	// Note: this takes at least 52 bytes, we may be able to replace this with lighter structure
//	}
//
//	public struct PageCacheSettings
//	{
//		/// <summary> The maximum size of buffer (in pages). </summary>
//		/// <remarks> That actual number of pages can exceed this if more pages are required due to the number of fixed pages. </remarks>
//		public uint MaximumFrames;
//
//		/// <summary> The number of pages that the buffer is expanded by (when expansion is necessary). </summary>
//		/// <remarks> This must be greater than 0. </remarks>
//		public uint GrowBy;
//
//		/// <summary> The vector of the cutoff point within the buffer. </summary>
//		/// <remarks> If this value is 0 (0%), the caching algorithm reverts to a traditional LRU.  If this value is 1 (100%) 
//		/// the algorithm behaves like a stack (FIFO) for non-recurring accesses.  Basically this fraction dictates how much 
//		/// space is given to keeping popular pages (pre-cutoff) vs, unfamiliar pages (cutoff and later). </remarks>
//		public ProperFraction Cutoff;
//
//		/// <summary> The logical time (number of requests), within which page references are considered correlated. </summary>
//		/// <remarks> Correllated references are not considered separate references, so page frames within 
//		/// FLogicalTime - FLastAccess &lt;= FCorrelatedReferencePeriod will not make the page "popular".  
//		/// A change to this property will not affect the positioning of existing frames.  A value of 0 will
//		/// effectively turn off the correlated reference optimization and all accesses to the cache for pages
//		/// that are already there will make said pages "popular". </remarks>
//		public uint CorrelatedReferencePeriod;
//
//		/// <summary> The maximum number of concurrent writers to spawn. </summary>
//		public uint MaximumWriters;
//
//		/// <summary> The maximum number of pages to write within a single write operation.  </summary>
//		/// <remarks> Setting this to 1 effectively disables scatter/gather IO.  This value cannot be less than 1. </remarks>
//		public uint MaximumSingleWritePages;
//
//		/// <summary> The maximum number of pages to scan (from the tail) in search of an unmodified victim, before just waiting for a write. </summary>
//		public uint MaximumUnmodifiedVictimScan;
//	}
//
//	public enum LockType { None, Read, Write };
//
//	public delegate void PageFrameHandler(PageCache ACache, PageFrame AFrame);
}
