/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

/*

Transaction Log:

	The transansaction log is implemented on top of 1 or more data files organizated using 
	the usual FileGroup PageID addressing.  The logical addressing of the log is as one long (64bit)
	stream.  The physical files are divided into segments, with each segment being assigned one
	portion of the logically addressible space.
	
	Allocation ->
		When a new segment is needed, the file of the current segment is first inspected for an unused 
		segment.  If a segment is found, it is used; otherwise a search of the next file 
		(recursive, looped) is performed for an unused segment.  If no unused segment is located, and 
		attempt is made to grow the current file.  If the current file is at capacity (as specified 
		by a per-file setting), a grow is attempted on the next file in-turn.  If no growth is possible,
		and circular logging is allowed, then the earliest segment is replaced.  If circular logging is 
		not allowed, then the allocation fails.
	

*/
namespace Alphora.Dataphor.DAE.Storage
{
/*
	public class TransactionLog
	{
		public TransactionLog(FileGroup AFileGroup)
		{
			FFileGroup = AFileGroup;
			FCache = new LogCache(FFileGroup, AFileGroup[1].PageSize);
		}

		#region Append

		/// <summary> Protects the append related structures. </summary>
		private object FAppendLatch = new Object();

		/// <summary> The computed page number whithin which the latest log record ends. </summary>
		/// <remarks> Locks-> Takes: FAppendLatch; Leaves: None; </remarks>
		private long GetLastPage()
		{
			lock (FAppendLatch)
				return FNextOffset / FPageSize;
		}

		/// <summary> Most recently appended record offset. </summary>
		private long FPriorOffset;
		/// <summary> End of the latest log record that has been appended. </summary>
		private long FNextOffset;

		/// <summary> The set of frames that are being appended to. </summary>
		/// <remarks> The 0th entry in this array is always kept, even when not actively appending. </remarks>
		private LogFrame[] FAppendFrames;
		/// <summary> The position (within the FAppendFrames[FAppendIndex] frame) of current append write activity. </summary>
		private int FAppendPosition;
		/// <summary> The index of frame into which appending is currently taking place. </summary>
		private int FAppendIndex = -1;
		/// <summary> The total length in bytes of the data portion of the currently active log appendature. </summary>
		private int FAppendLength;

		private long FCheckpointOffset;		// Most recent checkpoint

		/// <summary> The underlying log cache. </summary>
		private LogCache FCache;
		public LogCache Cache { get { return FCache; } }

		/// <summary> Begins the processes of appending a record to the log. </summary>
		/// <remarks> The append latch is kept until EndAppend is called, so the duration between BeginAppend and EndAppend should be kept minimal. </remarks>
		public void BeginAppend(IResourceManager AManager)
		{
			Error.DebugAssertFail(FAppendIndex < 0, "BeginAppend called without EndAppend within the same thread.");

			// TODO: Ensure allocation well in advance of the next page (before taking latch in case this requires I/O)

			Monitor.Enter(FAppendLatch);
			
			// Prepare to append
			FAppendIndex = 0;
			FAppendPosition = FNextOffset % FPageSize;
			FAppendLength = 0;

			// It is assumed that prior append left us in a page that has at least enough room for the header
			LogRecordHeader* LHeader = ((LogRecordHeader*)((byte*)FAppendFrames[0].Page + FAppendPosition));

			// Write most of the record header
			LHeader->DateTime = DateTime.Now();
			LHeader->Offset = FNextOffset;
			LHeader->PriorOffset = FPriorOffset;
			LHeader->ResourceManagerID = AManager.ID;
		}

		/// <summary> Advances to the next frame to append to. </summary>
		private void AdvanceAppendFrame()
		{
			int LPageNumber = (FNextOffset / FPageSize) + FAppendIndex + 1;
			FAppendIndex++;
			if ((LPageNumber & LogCache.CLogSegmentMask) != 0)	// Space remaining in this segment?
				// Space is available, simply use the next page in the segment
				FAppendFrames[FAppendIndex] = FCache.EmptyFix(FAppendFrames[FAppendIndex - 1].PageID + 1);
			else
				// Find a page in the next segment
				FAppendFrames[FAppendIndex] = FCache.EmptyFix(PageIDFromPageNumber(LPageNumber));
			FAppendPosition = DataFile.CPageHeaderSize;
		}

		/// <summary> Appends data to the log. </summary>
		/// <remarks> BeginAppend must be called before calling this method.  When all appending is complete, EndAppend must be called. </remarks>
		public unsafe void Append(IntPtr ABuffer, int ACount)
		{
			Error.DebugAssertFail(FAppendIndex >= 0, "TransactionLog.Append() called before BeginAppend or after EndAppend");
			Error.DebugAssertFail(ACount >= 0, "TransactionLog.Append() was called with a negative count");

			while (ACount > 0)
			{
				// Determine the amount to apply to the current page
				int LPageCount = Math.Min(FPageSize - (FAppendPosition + 1), ACount);

				// If there is no more space, advance to the next frame
				if (LPageCount = 0)
				{
					AdvanceAppendFrame();
					LPageCount = Math.Min(FPageSize - (FAppendPosition + 1), ACount);;
				}

				// Move the appropriate portion of the buffer
				MemoryUtility.Move(ABuffer, (IntPtr)((byte*)FAppendFrames[FAppendIndex].Page + FAppendPosition), LPageCount);

				FAppendPosition += LPageCount;
				ACount -= LPageCount;
			}

			FAppendLength += ACount;
		}

		/// <summary> Completes the log appendage. </summary>
		/// <returns> The offset of the beginning of the next log record. </returns>
		public long EndAppend()
		{
			Error.DebugAssertFail(FAppendIndex >= 0, "TransactionLog.EndAppend() called without first calling BeginAppend.");

			// Leave off on a page with at least enough space for the header
			if (FAppendPosition >= (FPageSize - sizeof(LogRecordHeader)))
				AdvanceAppendFrame();

			// Compute the new next offset
			long LResult = (((FNextOffset / FPageSize) + FAppendIndex) * FPageSize) + FAppendPosition;
			FPriorOffset = FNextOffset;
			FNextOffset = LResult;

			// Update the record header
			LogRecordHeader* LHeader = ((LogRecordHeader*)((byte*)FAppendFrames[0].Page + FAppendPosition));
			LHeader->NextOffset = FNextOffset;
			LHeader->Length = FAppendLength;

			// Release all but the last frame
			for (int i = 0; i < FAppendIndex; i++)
				Unfix(FAppendFrames[i]);

			// Put the last frame into the zero slot for the next write
			FAppendFrames[0] = FAppendFrames[FAppendIndex];	

			// Indicate that we are no longer appending
			FAppendIndex = -1;

			Monitor.Exit(FAppendLatch);

			return LResult;
		}

		#endregion

		#region Logical Directory

		private void PageIDFromPageNumber(long APageNumber)
		{
			
		}

		#endregion
	}

	public class LogCache
	{
		public const uint CDefaultMaximumFrames = 0x100;
		public const uint CDefaultGrowBy = 0x40;
		public const uint CDefaultMaximumFlushPages = 16;

		public const int CInitialSignalPoolCapacity = 20;
		public const int CInitialReaderSignalsCapacity = 20;

		public const int CLogSegmentPageCount = 128;
		public const int CLogSegmentShift = 7;
		public const int CLogSegmentMask = 0x7F;

		#region Constructor & Dispose

		public LogCache(FileGroup AFileGroup, int APageSize)
		{
			FPageSize = APageSize;
			FFileGroup = AFileGroup;
			DefaultSettings();
			FFrames = new Hashtable((int)CDefaultGrowBy);
			FBuffer = new PageBuffer(FPageSize);
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				// FlushAll();
			}
			finally
			{
				try
				{
					FBuffer.Dispose();
				}
				finally
				{
					base.Dispose(ADisposing);
				}
			}
		}

		#endregion

		#region Properties and Statistics

		private int FPageSize;
		/// <summary> Gets the page size for this cache. </summary>
		public int PageSize { get { return FPageSize; } }

		private FileGroup FFileGroup;
		/// <summary> Gets the data files manager for this cache. </summary>
		public FileGroup FileGroup
		{
			get { return FFileGroup; }
		}

		private LogCacheSettings FSettings;
		/// <summary> Gets or sets the settings for the cache. </summary>
		/// <remarks> Changing these settings will not affect the current state of the cache, but will be used for subsequent operations. </remarks>
		public LogCacheSettings Settings 
		{ 
			get { return FSettings; }
			set
			{
				if (value.GrowBy <= 0)
					throw new StorageException(StorageException.Codes.GrowByMustBeGreaterThanZero);
				if (value.MaximumSingleWritePages < 1)
					throw new StorageException(StorageException.Codes.InvalidMaximumSingleWritePages);
				lock (FFlushLatch)
				{
					if ((FFlushPageBuffers == null) || (FFlushPageBuffers.Length < value.MaximumFlushPages))
					{
						FFlushPageBuffers = new IntPtr[value.MaximumFlushPages];
						FFlushPageFrames = new LogFrame[value.MaximumFlushPages];
					}
					FSettings = value;
				}
			}
		}

		/// <summary> Sets or resets the cache's settings to their defaults. </summary>
		public void DefaultSettings()
		{
			LogCacheSettings LSettings = new LogCacheSettings();
			LSettings.MaximumFrames = CDefaultMaximumFrames;
			LSettings.GrowBy = CDefaultGrowBy;
			LSettings.MaximumSingleWritePages = CDefaultMaximumSingleWritePages;
			Settings = LSettings;
		}

		private int FHitCount;
		/// <summary> The number of hits (page references found in the cache) that have occurred since the cache was started. </summary>
		public int HitCount { get { return FHitCount; } }

		private int FMissCount;
		/// <summary> The number of misses (page references not found in the cache and loaded from the disk) that have occurred since the cache was started. </summary>
		public int MissCount { get { return FMissCount; } }

		#endregion

		#region Fix & Unfix

		/// <summary> Complete list of all non-freed log frame pages. </summary>
		private Hashtable FFrames;

		/// <summary> Dictionary of signals (by page ID) for threads waiting on page reads. </summary>
		private Hashtable ReaderSignals = new Hashtable(CInitialReaderSignalsCapacity);

		/// <summary> Fixes a page in the buffer, reading the page if necessary. </summary>
		/// <remarks> The frame's modified flag is initially false. </remarks>
		public LogFrame Fix(PageID APageID)
		{
			InternalFix(APageID, true);
		}

		/// <summary> Fixes a page in the buffer, but will not read the page if it is not already there. </summary>
		/// <remarks> The contents of the resulting buffer is undefined. The frame's modified flag is initially true. </remarks>
		public LogFrame EmptyFix(PageID APageID)
		{
			InternalFix(APageID, false);
		}

		public LogFrame InternalFix(PageID APageID, bool APerformRead)
		{
			for (;;)
			{
				// Lookup the frame
				Monitor.Enter(FCacheLatch);
				LogFrame LFrame = FFrames[APageID];
				if (LFrame != null)
				{
					// Attempt to grab the frame latch
					if (!Monitor.TryEnter(LFrame))
					{
						Monitor.Exit(FCacheLatch);
						continue;	// Restart
					}
					try
					{
						// If reading, put a signal out to wait for completion
						if (LFrame.IsReading)
						{
							ManualResetEvent LSignal = (ManualResetEvent)FReaderSignals[APageID];
							bool LSignalAdded = (LEvent == null);
							if (LSignalAdded)
							{
								LSignal = AcquireSignal();
								FReaderSignals.Add(APageID, LSignal);
							}
							Monitor.Exit(LFrame);
							Monitor.Exit(FCacheLatch);
							try
							{
								LSignal.WaitOne();
							}
							finally
							{
								if (LSignalAdded)
								{
									lock (FCacheLatch)
										FReaderSignals.Remove(APageID);
									RelinquishSignal(LSignal);
								}
							}
							continue;	// Restart
						}

						// Remove the frame from the LRU list (making it ineligable for replacement) if this is the first (only) fix
						if (LFrame.FFixCount == 0)
							Remove(LFrame);

						// Release the cache latch as soon as possible
						Monitor.Exit(FCacheLatch);

						// Update the pages fix count
						LFrame.FFixCount++;
					}
					finally
					{
						Monitor.Exit(LFrame);
					}

					// Account for a successful cache hit
					Interlocked.Increment(ref FHitCount);
				}
				else	// page not found
				{
					// Search for an unused buffer, or replace an existing one
					LFrame = LocateFrame();
					if (LFrame == null)		// Are all remaining availabe pages are locked or modified?
					{
						Monitor.Exit(FCacheLatch);

						// Find an offset to request a bit of writing to
						long LOffset;
						lock (FFlushLatch)
							LOffset = FFirstDirtyPage + FSettings.GroupWriteCount;	// Note: Access to FFirstDirtyPage directly would not be atomic because operations on >32bit things are not guaranteed to be atomic in the CLR
	
						// Request some flushing to avail a frame
						RequestFlush(LOffset);
						FlushWait(LOffset);

						continue;	// Restart
					}

					// Prepare the frame
					Monitor.Enter(LFrame);
					LFrame.FFixCount = 1;
					LFrame.FFlags = (APerformRead ? LogFrameFlags.Reading : LogFrameFlags.Modified);
					LFrame.FPageID = APageID;
					Monitor.Exit(LFrame);

					// Update the cache with the frame
					FFrames.Add(APageID, LFrame);
					Monitor.Exit(FCacheLatch);
					
					if (APerformRead)
					{
						FFileGroup.ReadPage(LFrame.FPage, APageID);
						
						// Update the reading flag on the page
						Monitor.Enter(LFrame);
						LFrame.FFlags &= ~LogFrameFlags.Reading;
						Monitor.Exit(LFrame);

						// Send a signal to any threads waiting for the read
						ManualResetEvent LSignal;
						lock (FCacheLatch)
							LSignal = (ManualResetEvent)FReaderSignals[APageID];
						if (LSignal != null)
							LSignal.Set();
					}

					// Account for a cache miss
					Interlocked.Increment(ref FMissCount);
				}
				break;
			}
		}

		/// <summary> Releases the reference of the specified page frame. </summary>
		public void Unfix(LogFrame AFrame)
		{
			for (;;)
			{
				Monitor.Enter(AFrame);
				try
				{
					if (AFrame.FFixCount == 1)	// Last unfix must place the frame back into the LRU list
					{
						if (!Monitor.TryEnter(FCacheLatch))
							continue;	// Restart
						PlaceAtHead(AFrame);
						Monitor.Exit(FCacheLatch);
					}
					AFrame.FFixCount--;
				}
				finally
				{
					Monitor.Exit(AFrame);
				}
			}
		}

		#endregion

		#region LRU Maintenance

		/// <summary> Latch used to protect the LRU chain as well as the FFrames table. </summary>
		public object FCacheLatch = new Object();

		/// <summary> Pointer to the head of the LRU chain. </summary>
		private LogFrame FLRUHead;
		/// <summary> Pointer to the tail of the LRU chain. </summary>
		private LogFrame FLRUTail;
		/// <summary> The total number of frames in the LRU chain. </summary>
		private int FLRUCount;

		/// <summary> Places the frame at the head of the LRU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void PlaceAtHead(LogFrame AFrame)
		{
			AFrame.FPrior = FLRUHead;
			AFrame.FNext = null;
			if (FLRUHead == null)
				FLRUTail = AFrame;
			else
				FLRUHead.FNext = AFrame;
			FLRUCount++;
			FLRUHead = AFrame;
		}

		/// <summary> Removes the specified frame from the FLU chain. </summary>
		/// <remarks> Locks-> Expects: FCacheLatch </remarks>
		private void Remove(LogFrame AFrame)
		{
			if (AFrame.FPrior != null)
				AFrame.FPrior.FNext = AFrame.FNext;
			if (AFrame.FNext != null)
				AFrame.FNext.FPrior = AFrame.FPrior;
			if (AFrame == FLRUHead)
				FLRUHead = AFrame.FPrior;
			if (AFrame == FLRUTail)
				FLRUTail = AFrame.FNext;
			FLRUCount--;
		}

		#endregion

		#region Frame Aquisition & Release

		/// <summary> Latch used to protect the free list. </summary>
		private object FFreeLatch;

		/// <summary> The anchor of the free frame chain (a singly-linked list)</summary>
		private LogFrame FFreeHead;

		/// <summary> The number of frames in the free frame chain. </summary>
		private int FFreeCount;

		/// <summary> The page buffer. </summary>
		private PageBuffer FBuffer;

		/// <summary> Makes a frame available through replacement or acquisition. </summary>
		/// <remarks> If null is returned, this indicates that all remaining frames are locked or modified
		/// Locks-> Expects: FCacheLatch; Takes: LFrame, FCacheLatch; Releases: FCacheLatch; Leaves: FCacheLatch </remarks>
		private LogFrame LocateFrame()
		{
			LogFrame LFrame;
			if ((FLRUCount >= FSettings.MaximumFrames) && (FLRUTail != null))
				return FindVictim();	// Only called if there are available pages
			else
				return AcquireFrame();
		}

		/// <summary> Finds and removes a page replacement victim. </summary>
		/// <remarks> If null is returned, this incidates that there are no available pages, or that all available pages are locked or modified
		/// Locks-> Expects: FCacheLatch; Takes: LFrame; Leaves: FCacheLatch </remarks>
		private LogFrame FindVictim()
		{
			LogFrame LFrame = FLRUTail;

			// Scan frames from the tail for an unmodified page
			while (LFrame != null)
			{
				try
				{
					if (!Monitor.TryEnter(LFrame))
						continue;	// Skip this one.
					try
					{
						if (!LFrame.IsModified)
						{
							// If we find an unmodified buffer, take it!
							FFrames.Remove(LFrame.FPageID);
							Remove(LFrame);
							return LFrame;
						}
					}
					finally
					{
						Monitor.Exit(LFrame);
					}
				}
				finally
				{
					LFrame = LFrame.FNext;
				}
			}

			return null;
		}

		/// <summary> Obtains an unused frame from the list of free frames (allocating more if necessary). </summary>
		/// <remarks> Locks-> Expects: none; Takes: FFreeLatch; Leaves: none </remarks>
		private LogFrame AcquireFrame()
		{
			LogFrame LFrame = null;
			lock (FFreeLatch)
			{
				if (FFreeHead == null)
				{
					// Allocate a new extent and assocate a LogFrame with each page within the extent
					int LNewPages = (int)FSettings.GrowBy;		// Copy this value as we do not want it changing as we go
					IntPtr LData = FBuffer.Allocate(LNewPages);
					try
					{
						LogFrame LOldFrame = null;
						for (int i = LNewPages - 1; i >= 0; i--)
						{
							LFrame = new LogFrame();
							LFrame.FPrior = LOldFrame;
							LFrame.FPage = (IntPtr)((uint)LData + (i * FPageSize));
							LOldFrame = LFrame;
						}
					}
					catch
					{
						FBuffer.Deallocate();
						throw;
					}
					FFreeHead = LFrame;
					FFreeCount = LNewPages;
				}
				else
					LFrame = FFreeHead;
				FFreeHead = FFreeHead.FPrior;
				FFreeCount--;
			}
			return LFrame;
		}

		/// <summary> Releases a no longer used frame back to the list of unused frames. </summary>
		/// <remarks> Locks-> Expects: none; Takes: FFreeLatch; Leaves: none </remarks>
		private void ReleaseFrame(LogFrame AFrame)
		{
			lock (FFreeLatch)
			{
				AFrame.FPrior = FFreeHead;
				FFreeHead = AFrame;
				FFreeCount++;
			}
		}

		#endregion

		#region Flushing

		/// <summary> Offset through which a flush has been requested. </summary>
		private long FRequestedOffset;

		/// <summary> Protects the flushing related structures. </summary>
		private object FFlushLatch = new Object();

		/// <summary> Table of pages that are being waited for flushing. </summary>
		private Hashtable FFlushSignals = new Hashtable(10);

		/// <summary> Logical page number of the earliest log page that is not flushed. </summary>
		/// <remarks> The first dirty page is usually FWrittenPage + 1, but this is not the case when the last page (after it is written) becomes dirty again. </remarks>
		private long FFirstDirtyPage;
		/// <summary> Logical page number of latest log page that has been scheduled for flushing. </summary>
		private long FPendingPage;
		/// <summary> Logical page number of latest written. </summary>
		/// <remarks> This may differ from the pending page due the reverting of the pendingpage when a change is made to the last page. </remarks>
		private long FWrittenPage;
		/// <summary> Flag indicating that flushing operation is underway. </summary>
		private bool FIsPending;

		/// <summary> Scratch pad array used for buffers by the flushing process. </summary>
		/// <remarks> Protected by FIsPending. </remarks>
		private IntPtr[] FFlushPageBuffers;
		/// <summary> Scratch pad array used for frames by the flushing process. </summary>
		/// <remarks> Protected by FIsPending. </remarks>
		private LogFrame[] FFlushPageFrames;

		/// <summary> Starts a single flush IO request if there isn't one progressing. </summary>
		/// <remarks> Latches-> Expects: None; Takes: FFlushLatch; Leaves: None; </remarks>
		private void BeginFlush()
		{
			Error.DebugAssertFail(FMaxPending >= FMinDirty, "BeginFlush: FMaxPending should never be less than FMinDirty");

			// Determine the last page before taking the flush latch
			long LLastPage = GetLastPage();

			Monitor.Enter(FFlushLatch);
			if (!FIsPending && (FPendingPage < LLastPage))	// Only start an I/O operation if there isn't one started and flushing is not complete
			{
				FIsPending = true;
				long LMinPage = FPendingPage + 1;	// LMinPage represents the first page from which to begin flushing

				if ((LMinPage == LLastPage) && (FWrittenPage > FPendingPage))	// Check for rewrite of last page... do special ping-pong write in this case
				{
					long LPingPongPage = LMinPage + 1;

					FPendingPage++;

					// Release the flush latch here before we do any I/O
					Monitor.Exit(FFlushLatch);

					try
					{
						// Ensure that we have enough space to write the next page
						EnsureAllocationThrough(LPingPongPage);

						// Get the appropriate page to write
						LogFrame LFrame;
						lock (FCacheLatch)
							LFrame = (LogFrame)FFrames[PageIDFromPageNumber(LMinPage)];

						// Special ping-ponged write for last page
						FFileGroup.BeginWritePage(LFrame.FPage, PageIDFromPageNumber(LPingPongPage), new AsyncCallback(PingPongWriteCompleted), LFrame);
					}
					catch
					{
						// Restore prior state
						lock (FFlushLatch)
						{
							FPendingPage = FFirstDirtyPage - 1;
							FIsPending = false;
						}
						// TODO: Log or otherwise handle this condition
						throw;
					}
				}
				else
				{
					// Determine the maximum size of a contiguous write.  Note: a segment must be in a single file and allocation takes place in whole segments
					long LMaxPage =		// Inclusive, Lesser of: max pages to write setting, end of segment, last page or page before last depending on ping pong requirement
						Math.Min
						(
							LMinPage + (FSettings.MaximumFlushPages - 1), 
							Math.Min
							(
								(((LMinPage / CLogSegmentPageCount) + 1) * CLogSegmentPageCount) - 1, 
								(FWrittenPage > FPendingPage ? LLastPage - 1 : LLastPage)
							)
						);

					FPendingPage = LMaxPage;

					Monitor.Exit(FFlushLatch);	// release the flush latch - everything from here should be protected by the FIsPending

					try
					{
						// Get the page ID of the beginning of the write
						PageID LStartID = PageIDFromPageNumber(FFirstDirtyPage);
						PageID LEndID = LStartID + (LMaxPage - LMinPage);

						// Build the page frame and buffer lists
						int LCount = 0;
						lock (FCacheLatch)
							for (PageID LCurrentID = LStartID; LCurrentID <= LEndID; LCurrentID++)
							{
								// Get the page
								LogFrame LFrame = (LogFrame)FFrames[LCurrentID];

								// Add the page to the flush and buffer address lists
								FFlushPageBuffers[LCount] = LFrame.FPage;
								FFlushPageFrames[LCount] = LFrame;
								LCount++;
							}

						// Write the pages
						FFileGroup.BeginWritePages(FFlushPageBuffers, LCount, LStartID, new AsyncCallback(FlushWriteCompleted), FFlushPageFrames[0]);
					}
					catch
					{
						lock (FFlushLatch)
						{
							FPendingPage = FFirstDirtyPage - 1;
							FIsPending = false;
						}
						// TODO: Log or otherwise handle this condition
						throw;
					}
				}
			}
			else
				Monitor.Exit(FFlushLatch);
		}

		/// <summary> Callback from special ping-pong write request. </summary>
		private void PingPongWriteCompleted(IAsyncResult AResult)
		{
			try
			{
				// Get the frame being written (before calling EndWritePage)
				LogFrame LFrame = (LogFrame)AResult.AsyncState;
	
				// Complete the I/O, report any errors
				FFileGroup.EndWritePage(AResult);

				// Now that the page is safely written elsewhere, write the page to its proper place
				FFileGroup.BeginWritePage(LFrame.FPage, LFrame.FPageID, new AsyncCallback(FlushWriteCompleted), LFrame);
			}
			catch
			{
				lock (FFlushLock)
				{
					FPendingPage--;
					FIsPending = false;
				}
				// TODO: Log or otherwise handle this condition
			}
		}

		/// <summary> Callback from asynchronous flush write request. </summary>
		private void FlushWriteCompleted(IAsyncResult AResult)
		{
			try
			{
				// Complete the I/O, report any errors
				FFileGroup.EndWritePage(AResult);
			}
			catch
			{
				lock (FFlushLatch)
				{
					FPendingPage = FFirstDirtyPage - 1;
					FIsPending = false;
				}
				// TODO: Log or otherwise handle this condition
				throw;
			}

			// Update the modified state of the frames
			int LThrough = FPendingPage - FFirstDirtyPage;
			for (int i = 0; i <= LThrough; i++)
			{
				LogFrame LFrame = (LogFrame)FFlushPageFrames[i];
				lock (LFrame)
					LFrame.FFlags &= ~LogFrameFlags.Modified;
			}

			Monitor.Enter(FFlushLatch);

			FIsPending = false;
			FFirstDirtyPage = FPendingPage + 1;
			FWrittenPage = FPendingPage;

			// Notify any waiting threads of the flush
			for (long i = FFirstDirtyPage; i <= FPendingPage; i++)
			{
				ManualResetEvent LSignal = (ManualResetEvent)FFlushSignals[i];
				if (LSignal != null)
					LSignal.Set();
			}

			// Continue flushing if requested
			if (FFirstDirtyPage < FRequestedPage)
			{
				Monitor.Exit(FFlushLatch);
				StartBeginFlush(null);
			}
			else
				Monitor.Exit(FFlushLatch);
		}

		/// <summary> Waits the current thread wait until the log is flushed through the specified offset. </summary>
		/// <remarks> If the log is already flushed passed the specified offset, the method returns immediately.
		/// Locks -> Expects: None; Takes: FFlushLatch; Leaves: None; </remarks>
		public void FlushWait(long AOffset)
		{
			long LPageNumber = AOffset / FPageSize;
			Monitor.Enter(FFlushLatch);
			if (LPageNumber >= FFirstDirtyPage)
			{
				ManualResetEvent LSignal;
				bool LAddedSignal;
				try
				{
					// Look for an existing request for notification for the page
					LSignal = (ManualResetEvent)FFlushSignals[LPageNumber];

					// Remember if this thread added the signal
					LAddedSignal = (LSignal == null);

					// Add the signal if necessary
					if (LAddedSignal)
					{
						LSignal = AcquireSignal();
						FFlushSignals.Add(LPageNumber, LSignal);
					}
				}
				finally
				{
					Monitor.Exit(FFlushLatch);
				}
				try
				{
					LSignal.WaitOne();
				}
				finally
				{
					// Remove the signal if it was this thread that added it
					if (LAddedSignal)
					{
						lock (FFlushLatch)
						{
							FFlushSignals.Remove(LPageNumber);
							RelinquishSignal(LSignal);
						}
					}
				}
			}
			else
				Monitor.Exit(FFlushLatch);
		}

		/// <summary> Method for asynchronously beginning flush operation. </summary>
		private void StartBeginFlush(object AState)
		{
			try
			{
				BeginFlush();
			}
			catch
			{
				// TODO: Log or otherwise notify of error
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		/// <summary> Requests that flushing occur through the specified offset. </summary>
		public void RequestFlush(long AOffset)
		{
			long LLastPage = GetLastPage();
			long LPageNumber = Math.Min(LLastPage, AOffset / FPageSize);

			Monitor.Enter(FFlushLatch);
			if (LPageNumber > FRequestedPage)
			{
				FRequestedPage = LPageNumber;
				Monitor.Exit(FFlushLatch);
				ThreadPool.QueueUserWorkItem(new WaitCallback(StartBeginFlush));
			}
			else
				Monitor.Exit(FFlushLatch);
		}

		/// <summary> Sets invalidates </summary>
		/// <param name="APageNumber"></param>
		private void SetLastPageDirty(long APageNumber)
		{
			lock (FFlushLatch)
			{
				if (FPendingPage >= APageNumber)
					FPendingPage = APageNumber - 1;
				FFirstDirtyPage = APageNumber;
			}
		}

		#endregion

		#region Signal Pool

		private ManualResetEvent[] FSignalPool = new ManualResetEvent[CInitialSignalPoolCapacity];
		private int FSignalPoolCount;
		private int FSignalPoolInUse;

		private ManualResetEvent AcquireSignal()
		{
			for (;;)
			{
				// Spin until we have exlusive
				if (Interlocked.CompareExchange(ref FSignalPoolInUse, 1, 0) == 1)
					continue;

				if (FSignalPoolCount > 0)
				{
					try
					{
						FSignalPoolCount--;
						ManualResetEvent LSignal = FSignalPool[FSignalPoolCount];
					}
					finally
					{
						Interlocked.Decrement(ref FSignalPoolInUse);
					}
					LSignal.Reset();
					return LSignal;
				}
				else
				{
					Interlocked.Decrement(ref FSignalPoolInUse);
					return new ManualResetEvent(false);
				}
			}
		}

		private void RelinquishSignal(ManualResetEvent AEvent)
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref FSignalPoolInUse, 1, 0) == 1);
			try
			{
				// Grow the capacity if necessary
				if (FSignalPool.Length <= FSignalPoolCount)
				{
					ManualResetEvent[] LNewList = new ManualResetEvent[Math.Min(FSignalPool.Length * 2, FSignalPool.Length + 512)];
					Array.Copy(FSignalPool, 0, LNewList, 0, FSignalPool.Length);
					FSignalPool = LNewList;
				}

				// Add to the pool
				FSignalPool[FSignalPoolCount] = AEvent;
				FSignalPoolCount++;
			}
			finally
			{
				Interlocked.Decrement(ref FSignalPoolInUse);
			}
		}

		#endregion
	}

	public struct LogCacheSettings
	{
		/// <summary> The maximum size of buffer (in pages). </summary>
		/// <remarks> That actual number of pages can exceed this if more pages are required due to the number of fixed pages. </remarks>
		public uint MaximumFrames;

		/// <summary> The number of pages that the buffer is expanded by (when expansion is necessary). </summary>
		/// <remarks> This must be greater than 0. </remarks>
		public uint GrowBy;

		/// <summary> The maximum number of pages to write within a single write operation.  </summary>
		/// <remarks> Setting this to 1 effectively disables scatter/gather IO.  This value cannot be less than 1. </remarks>
		public uint MaximumFlushPages;

		/// <summary> Number of pages to accumulate before doing a group write. </summary>
		/// <remarks> This must be greater than 0.  Unless specifically instructed to flush to a certain point, the log manager waits until 
		/// this many pages have accumulated before performing a flush. </remarks>
		public uint GroupWriteCount;
	}

	[Flags]
	public enum LogFrameFlags : byte
	{
		None = 0,

		/// <summary> Modified is set when the page first is modified and is released after the page has been intirely written to disk. </summary>
		Modified = 1,

		/// <summary> Indicates that the page is currently being read. </summary>
		Reading = 2,
	}

	public class LogFrame : IPageAddressing
	{
		internal PageID FPageID;
		public PageID PageID { get { return FPageID; } }

		internal IntPtr FPage;
		public IntPtr Page { get { return FPage; } }

		internal LogFrameFlags FFlags;
		public bool IsModified { get { return (FFlags & LogFrameFlags.Modified) != 0; } }
		public bool IsReading { get { return (FFlags & LogFrameFlags.Reading) != 0; } }

		internal LogFrame FNext;
		internal LogFrame FPrior;
		internal int FFixCount;
	}

	[StructLayout(LayoutKind.Explicit, Size=37)]
	public struct LogRecordHeader
	{
		[FieldOffset(0)]	public long Offset;
		[FieldOffset(8)]	public long PriorOffset;
		[FieldOffset(16)]	public long NextOffset;
		[FieldOffset(24)]	public DateTime DateTime; // 8 bytes
		[FieldOffset(32)]	public int Length;
		[FieldOffset(36)]	public byte ResourceManagerID;	// Place byte last to keep things aligned
	}

	[StructLayout(LayoutKind.Explicit, Size=4)]
	public struct LogPageHeader
	{
		[FieldOffset(0)]	public uint CRC32;
	}
*/
}
