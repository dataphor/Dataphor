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
using System.Text;

using Microsoft.Win32.SafeHandles;

/*
	The FastFile class provides overlapped (asyncronous), unbuffered file IO with forced write through.  The .NET
	FileStream class is not ideal for database file random access because it: a) performs buffering; b) has lots of 
	overhead for supporting many different access methods; c) file position is an unnecessary concept; and d) because 
	it does not give detailed access to the Windows API.
	Note that reads and write must be performed in increments of the storage sector size.
	
	*Windows NT/2000 or greater is required for overlapped IO.  
	TODO: Alternative implementation for Win9x support.
*/

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> A buffered, non-positioned, block transer based file wrapper. </summary>
	/// <remarks> This class is safe for multi-threaded operations. </remarks>
	public class FastFile : Disposable
	{
		public const int CInitialAsyncResultPoolCapacity = 64;

		#region Constructors, Dispose, & Properties

		/// <remarks> Static constructor prepares the files system callback and gets the page size. </remarks>
		static unsafe FastFile()
		{
			FIOCallback = new IOCompletionCallback(FastFile.AsyncIOCallback);

			SYSTEM_INFO LInfo = new SYSTEM_INFO();
			GetSystemInfo(out LInfo);

			FPageSize = (int)LInfo.dwPageSize;
		}

		/// <summary> Opens the specified file in the specified mode. </summary>
		/// <remarks> If the file does not exist and AReadOnly is false, the file will be created. </remarks>
		public FastFile(string AFileName, bool AReadOnly)
		{
			FReadOnly = AReadOnly;
			FFileName = Path.GetFullPath(AFileName);

			uint LRights = AReadOnly ? 0x80000000 : 0x10000000;
			FileMode LMode = AReadOnly ? FileMode.Open : FileMode.OpenOrCreate;

			SafeFileHandle LHandle = CreateFile
			(
				FFileName, 
				LRights, 
				FileShare.None, 
				IntPtr.Zero, 
				LMode,
				0x00002000 /* CONTENT_NOT_INDEXED */ | 0x40000000 /* OVERLAPPED */ | 0x20000000 /* NO_BUFFERING */ | 0x80000000 /* WRITE_THROUGH */,
				IntPtr.Zero
			);
            // TODO: Check for non-files

			if (LHandle.IsInvalid)
				ThrowWinIOError(FFileName);

			// Registers the handle with the thread pool, so that callbacks are performed on a pooled thread
            if (!ThreadPool.BindHandle(LHandle))
                LHandle.Close();

            FHandle = LHandle;
		}

		/// <remarks> Be sure to finish all IO before disposing this object. </remarks>
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if ((FHandle != null) && (!FHandle.IsClosed))
                        FHandle.Dispose();
				}
				finally
				{
					ClearAsyncResultPool();
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private bool FReadOnly;
		/// <summary> Indicates whether the file was opened in read-only mode. </summary>
		public bool ReadOnly { get { return FReadOnly; } }

		private string FFileName;
		/// <summary> Gets the full file name of the file. </summary>
		public string FileName { get { return FFileName; } }

		private static int FPageSize;
		/// <summary> Gets the system page size. </summary>
		/// <remarks> All IO must be done in blocks that are multiples of this. </remarks>
		public static int PageSize { get { return FPageSize; } }

		#endregion

		#region Callback

		/// <summary> Callback from the file system. </summary>
		private static unsafe void AsyncIOCallback(uint AErrorCode, uint ANumBytes, NativeOverlapped* AOverlappedPointer)
		{
			// Extract the async result info
			Overlapped LOverlapped = Overlapped.Unpack(AOverlappedPointer);		// This is an unpooled allocation, but unfortunately this would be difficult to work around due to the framework's protection of the asyncresult member.
			FastFile.AsyncResult LResult = (FastFile.AsyncResult)LOverlapped.AsyncResult;
			LResult.FSize = (int)ANumBytes;
			LResult.FErrorCode = (int)AErrorCode;

			// Signal completion
			if ((LResult.AsyncWaitHandle != null) && !LResult.AsyncWaitHandle.Set())
				ThrowWinIOError(LResult.FFileName);

			LResult.FIsCompleted = true;
			
			// Invoke the callback
			AsyncCallback LCallback = LResult.FUserCallback;
			if (LCallback != null)
				LCallback(LResult);
		}

		/// <summary> The static file system callback. </summary>
		private static readonly IOCompletionCallback FIOCallback;

		/// <summary> Asynchronous callback state container. </summary>
		internal class AsyncResult : IAsyncResult
		{
			internal AsyncCallback FUserCallback;
			/// <summary> Invokes the user callback on another thread. </summary>
			/// <remarks> This is used when execution completes syncronously.  This does not affect the CompletedSynchronously property 
			/// because the callback is still asynchronously invoked. </remarks>
			internal void CallUserCallback()
			{
				if (FUserCallback != null)
					FUserCallback.BeginInvoke(this, null, null);
			}

			internal object FUserData;
			public object AsyncState { get { return FUserData; } }		// Unintuitive name for user data to satisfy the IAsyncResult interface

			internal ManualResetEvent FAsyncWaitHandle;
			/// <summary> Wait handle becomes signaled when IO complete. </summary>
			public ManualResetEvent AsyncWaitHandle { get { return FAsyncWaitHandle; } }
            WaitHandle IAsyncResult.AsyncWaitHandle { get { return FAsyncWaitHandle; } }

			/// <summary> Unused (Required as part of IAsyncResult). </summary>
			public bool CompletedSynchronously { get { return false; } }

			internal bool FIsCompleted;
			/// <summary> Indicates that the IO call completed. </summary>
			public bool IsCompleted { get { return FIsCompleted; } set { FIsCompleted = value; } }

			internal int FEndCalled;
			internal int FErrorCode;
			internal bool FIsWrite;
			internal int FSize;
			internal string FFileName;
			internal unsafe NativeOverlapped* FOverlapped;
		}

		#endregion

		#region Handle

		private SafeFileHandle FHandle;

		public bool IsOpen 
		{ 
			get 
			{
				return (FHandle != null) && !FHandle.IsClosed; 
			} 
		}

		public void CheckOpen()
		{
			if (!IsOpen)
				throw new StorageException(StorageException.Codes.FileNotOpen, FFileName);
		}

		#endregion

		#region AsyncResult Pool

		private FastFile.AsyncResult[] FAsyncResultPool = new FastFile.AsyncResult[CInitialAsyncResultPoolCapacity];
		private int FAsyncResultPoolCount;
		private int FAsyncResultPoolInUse;

		/// <summary> Re-uses or allocates an AsyncResult structure. </summary>
		/// <remarks> This method is thread-safe. </remarks>
		private unsafe FastFile.AsyncResult AcquireAsyncResult(long AOffset)
		{
			for (;;)
			{
				// Spin until we have exlusive
				if (Interlocked.CompareExchange(ref FAsyncResultPoolInUse, 1, 0) == 1)
					continue;

				FastFile.AsyncResult LResult;
				if (FAsyncResultPoolCount == 0)
				{
					// No existing items, allocate a new one

					// Unlock, we don't need the pool any more
					Interlocked.Decrement(ref FAsyncResultPoolInUse);

					// Allocate and prepare the new item
					LResult = new FastFile.AsyncResult();
					Overlapped LOverlapped = new Overlapped(0, 0, IntPtr.Zero, LResult);
					LOverlapped.OffsetLow = (int)AOffset;
					LOverlapped.OffsetHigh = (int)(AOffset >> 0x20);
					#pragma warning disable 618
					LResult.FOverlapped = LOverlapped.Pack(FastFile.FIOCallback);	// warning CS0618: 'System.Threading.Overlapped.Pack(System.Threading.IOCompletionCallback)' is obsolete: 'This method is not safe.  Use Pack (iocb, userData) instead.  http://go.microsoft.com/fwlink/?linkid=14202'
					#pragma warning restore 618
					LResult.FAsyncWaitHandle = new ManualResetEvent(false);
				}
				else
				{
					try
					{
						// Remove the last item
						FAsyncResultPoolCount--;
						LResult = FAsyncResultPool[FAsyncResultPoolCount];
					}
					finally
					{
						// Unlock
						Interlocked.Decrement(ref FAsyncResultPoolInUse);
					}

					// Prepare the item
					LResult.FOverlapped->OffsetLow = (int)AOffset;
					LResult.FOverlapped->OffsetHigh = (int)(AOffset >> 0x20);
					LResult.FAsyncWaitHandle.Reset();
				}

				return LResult;
			}
		}

		/// <summary> Relinquishes the AsyncResult structure back into the pool. </summary>
		/// <remarks> This method is thread-safe. </remarks>
		private void RelinquishAsyncResult(FastFile.AsyncResult AValue)
		{
			// Spin until we have exlusive
			while (Interlocked.CompareExchange(ref FAsyncResultPoolInUse, 1, 0) == 1);
			try
			{
				// Grow the pool capacity if necessary
				if (FAsyncResultPoolCount >= FAsyncResultPool.Length)
				{
					FastFile.AsyncResult[] LNewList = new FastFile.AsyncResult[Math.Max(FAsyncResultPool.Length * 2, FAsyncResultPool.Length + 512)];
					Array.Copy(FAsyncResultPool, 0, LNewList, 0, FAsyncResultPool.Length);
					FAsyncResultPool = LNewList;
				}

				// Add to the pool
				FAsyncResultPool[FAsyncResultPoolCount] = AValue;
				FAsyncResultPoolCount++;
			}
			finally
			{
				// Unlock
				Interlocked.Decrement(ref FAsyncResultPoolInUse);
			}
		}

		private unsafe void ClearAsyncResultPool()
		{
			for (;;)
			{
				// Spin until we have exlusive
				if (Interlocked.CompareExchange(ref FAsyncResultPoolInUse, 1, 0) == 1)
					continue;
				try
				{
					while (FAsyncResultPoolCount > 0)
					{
						FAsyncResultPoolCount--;
						FastFile.AsyncResult LResult = FAsyncResultPool[FAsyncResultPoolCount];
						LResult.AsyncWaitHandle.Close();
						Overlapped.Free(LResult.FOverlapped);
					}
				}
				finally
				{
					// Unlock
					Interlocked.Decrement(ref FAsyncResultPoolInUse);
				}

				break;
			}
		}

		#endregion

		#region Reading & Writing Common

		/// <summary> Prepares the asynchronous IO state object. </summary>
		private unsafe FastFile.AsyncResult PrepareAsyncResult(int ASize, long AOffset, AsyncCallback ACallback, object AUserData, bool AIsWrite)
		{
			if (AOffset < 0)
				throw new ArgumentOutOfRangeException("AOffset");
			if (ASize < 0)
				throw new ArgumentOutOfRangeException("ASize");

			CheckOpen();

			AsyncResult LResult = AcquireAsyncResult(AOffset);
			LResult.FUserCallback = ACallback;
			LResult.FUserData = AUserData;
			LResult.FSize = ASize;
			LResult.FFileName = FFileName;
			LResult.FIsWrite = AIsWrite;

			if (ASize == 0)
			{
				LResult.FIsCompleted = true;
				LResult.CallUserCallback();
			}
			else
				LResult.FIsCompleted = false;

			return LResult;
		}

		#endregion

		#region Writing

		/// <summary> Writes ASize bytes from ABuffer into the file starting at offset AOffset. </summary>
		/// <remarks> The write count must be an increment of the storage's sector size. </remarks>
		public int Write(IntPtr ABuffer, int ASize, long AOffset)
		{
			IAsyncResult LResult = BeginWrite(ABuffer, ASize, AOffset, null, null);
			return EndWrite(LResult);
		}

		/// <summary> Queues the writing of a block and specifies a completion callback. </summary>
		/// <remarks> The write count must be an increment of the storage's sector size. 
		/// The AUserData will be available as the AsyncData member of the IAsyncResult passed to the callback. </remarks>
		public unsafe IAsyncResult BeginWrite(IntPtr ABuffer, int ASize, long AOffset, AsyncCallback ACallback, object AUserData)
		{
			if (ABuffer == IntPtr.Zero)
				throw new ArgumentNullException("ABuffer");

			AsyncResult LResult = PrepareAsyncResult(ASize, AOffset, ACallback, AUserData, true);
			try
			{
				if (LResult.FIsCompleted)
					return LResult;
				else
				{
					int LReturn;
					LReturn = WriteFile(FHandle, ABuffer, ASize, IntPtr.Zero, LResult.FOverlapped);
					if (LReturn == 0)
					{
						LReturn = Marshal.GetLastWin32Error();
						switch (LReturn)
						{
							case 6 /* ERROR_INVALID_HANDLE */ : FHandle.SetHandleAsInvalid(); break;
							case 997 /* ERROR_IO_PENDING */ : return LResult;
						}
						ThrowWinIOError(FFileName, LReturn);
					}
					return LResult;
				}
			}
			catch
			{
				RelinquishAsyncResult(LResult);
				throw;
			}
		}

		/// <summary> Writes ACount entries of the provided ABuffers array, each element being of ASize size starting at offset [AOffset]. </summary>
		/// <remarks> The write size must be an increment of the storage's sector size. </remarks>
		public int Write(IntPtr[] ABuffers, int ACount, int ASize, long AOffset)
		{
			IAsyncResult LResult = BeginWrite(ABuffers, ACount, ASize, AOffset, null, null);
			return EndWrite(LResult);
		}

		/// <summary> Queues the writing of an array of buffers. </summary>
		/// <remarks> The write size must be an increment of the system's page size. </remarks>
		public unsafe IAsyncResult BeginWrite(IntPtr[] ABuffers, int ACount, int ASize, long AOffset, AsyncCallback ACallback, object AUserData)
		{
			if (ABuffers == null)
				throw new ArgumentNullException("ABuffers");
			Error.DebugAssertFail((ASize % FPageSize) == 0, "BeginWrite(Gather): ASize is not a multiple of the system page size");

			AsyncResult LResult = PrepareAsyncResult(ACount * ASize, AOffset, ACallback, AUserData, true);
			try
			{
				if (LResult.FIsCompleted)
					return LResult;
				else
				{
					// Construct the array of page sized file segments
					int LPerBufferPageCount = ASize / FPageSize;
					FILE_SEGMENT_ELEMENT* LElements = stackalloc FILE_SEGMENT_ELEMENT[(ACount * LPerBufferPageCount) + 1];
					for (int LElementIndex = 0; LElementIndex < ACount; LElementIndex++)
					{
						for (int LPageIndex = 0; LPageIndex < LPerBufferPageCount; LPageIndex++)
							LElements[(LElementIndex * LPerBufferPageCount) + LPageIndex].Buffer = (IntPtr)((uint)ABuffers[LElementIndex] + (LPageIndex * FPageSize));
					}
					LElements[(ACount * LPerBufferPageCount)].Buffer = IntPtr.Zero;

					int LReturn;
					LReturn = WriteFileGather(FHandle, LElements, ACount * ASize, IntPtr.Zero, LResult.FOverlapped);
					if (LReturn == 0)
					{
						LReturn = Marshal.GetLastWin32Error();
						switch (LReturn)
						{
							case 6 /* ERROR_INVALID_HANDLE */ : FHandle.SetHandleAsInvalid(); break;
							case 997 /* ERROR_IO_PENDING */ : return LResult;
						}
						ThrowWinIOError(FFileName, LReturn);
					}
					return LResult;
				}
			}
			catch
			{
				RelinquishAsyncResult(LResult);
				throw;
			}
		}

		/// <summary> Called to end an asynchronous write request. </summary>
		/// <remarks> This is usually called within the callback method.  Will throw an exception if an error occurred attempting the I/O. </remarks>
		public unsafe int EndWrite(IAsyncResult AAsyncResult)
		{
			// Get and verify the async result
			if (AAsyncResult == null)
				throw new ArgumentNullException("AAsyncResult");
			FastFile.AsyncResult LResult = AAsyncResult as FastFile.AsyncResult;
			if ((LResult == null) || !LResult.FIsWrite)
				Error.Fail("FastFile.EndWrite was called with an incorrect AsyncResult");
			if (Interlocked.CompareExchange(ref LResult.FEndCalled, 1, 0) == 1)
				Error.Fail("FastFile.EndWrite has been incorrectly called multiple times with a given AsyncResult.");

			try
			{
				// Wait for the I/O to complete
				if (!LResult.IsCompleted)
				{
					LResult.FAsyncWaitHandle.WaitOne();
					LResult.FIsCompleted = true;
				}

				// Throw an error if one occurred
				if (LResult.FErrorCode != 0)
					ThrowWinIOError(LResult.FFileName, LResult.FErrorCode);

				return LResult.FSize;
			}
			finally
			{
				// Relinquish the async result to the pool
				RelinquishAsyncResult(LResult);
			}
		}

		#endregion

		#region Reading

		/// <remarks> The read count must be an increment of the storage's sector size. </remarks>
		public int Read(IntPtr ABuffer, int ACount, long AOffset)
		{
			IAsyncResult LResult = BeginRead(ABuffer, ACount, AOffset, null, null);
			return EndRead(LResult);
		}

		/// <remarks> The read count must be an increment of the storage's sector size.  
		/// The AUserData will be available as the AsyncData member of the IAsyncResult passed to the callback. </remarks>
		public unsafe IAsyncResult BeginRead(IntPtr ABuffer, int ACount, long AOffset, AsyncCallback ACallback, object AUserData)
		{
			if (ABuffer == IntPtr.Zero)
				throw new ArgumentNullException("ABuffer");

			AsyncResult LResult = PrepareAsyncResult(ACount, AOffset, ACallback, AUserData, false);
			try
			{
				if (LResult.FIsCompleted)
					return LResult;
				else
				{
					int LReturn;
					LReturn = ReadFile(FHandle, ABuffer, ACount, IntPtr.Zero, LResult.FOverlapped);
					if (LReturn == 0)
					{
						LReturn = Marshal.GetLastWin32Error();
						switch (LReturn)
						{
							case 6 /* ERROR_INVALID_HANDLE */ : FHandle.SetHandleAsInvalid(); break;
							case 997 /* ERROR_IO_PENDING */ : return LResult;
						}
						ThrowWinIOError(FFileName, LReturn);
					}
					return LResult;
				}
			}
			catch
			{
				RelinquishAsyncResult(LResult);
				throw;
			}
		}

		public int Read(IntPtr[] ABuffers, int ACount, int ASize, long AOffset)
		{
			IAsyncResult LResult = BeginRead(ABuffers, ACount, ASize, AOffset, null, null);
			return EndRead(LResult);
		}

		/// <remarks> ASize must be an multiple of the PageSize.
		/// The AUserData will be available as the AsyncData member of the IAsyncResult passed to the callback. </remarks>
		public unsafe IAsyncResult BeginRead(IntPtr[] ABuffers, int ACount, int ASize, long AOffset, AsyncCallback ACallback, object AUserData)
		{
			if (ABuffers == null)
				throw new ArgumentNullException("ABuffers");
			Error.DebugAssertFail((ASize % FPageSize) == 0, "BeginRead(Scatter): ASize is not a multiple of the system page size");

			AsyncResult LResult = PrepareAsyncResult(ACount * ASize, AOffset, ACallback, AUserData, false);
			try
			{
				if (LResult.FIsCompleted)
					return LResult;
				else
				{
					// Construct the array of page sized file segments
					int LPerBufferPageCount = ASize / FPageSize;
					FILE_SEGMENT_ELEMENT* LElements = stackalloc FILE_SEGMENT_ELEMENT[(ACount * LPerBufferPageCount) + 1];
					for (int LElementIndex = 0; LElementIndex < ACount; LElementIndex++)
					{
						for (int LPageIndex = 0; LPageIndex < LPerBufferPageCount; LPageIndex++)
							LElements[(LElementIndex * LPerBufferPageCount) + LPageIndex].Buffer = (IntPtr)((uint)ABuffers[LElementIndex] + (LPageIndex * FPageSize));
					}
					LElements[(ACount * LPerBufferPageCount)].Buffer = IntPtr.Zero;

					int LReturn;
					LReturn = ReadFileScatter(FHandle, LElements, ACount * ASize, IntPtr.Zero, LResult.FOverlapped);
					if (LReturn == 0)
					{
						LReturn = Marshal.GetLastWin32Error();
						switch (LReturn)
						{
							case 6 /* ERROR_INVALID_HANDLE */ : FHandle.SetHandleAsInvalid(); break;
							case 997 /* ERROR_IO_PENDING */ : return LResult;
						}
						ThrowWinIOError(FFileName, LReturn);
					}
					return LResult;
				}
			}
			catch
			{
				RelinquishAsyncResult(LResult);
				throw;
			}
		}

		/// <summary> Called to end an asynchronous write request. </summary>
		/// <remarks> This is usually called within the callback method.  Will throw an exception if an error occurred attempting the I/O. </remarks>
		public unsafe int EndRead(IAsyncResult AAsyncResult)
		{
			// Get and verify the async result
			if (AAsyncResult == null)
				throw new ArgumentNullException("AAsyncResult");
			FastFile.AsyncResult LResult = AAsyncResult as FastFile.AsyncResult;
			if ((LResult == null) || LResult.FIsWrite)
				Error.Fail("FastFile.EndRead was called with an incorrect AsyncResult");
			if (Interlocked.CompareExchange(ref LResult.FEndCalled, 1, 0) == 1)
				Error.Fail("FastFile.EndRead has been incorrectly called multiple times with a given AsyncResult.");

			try
			{
				// Wait for the I/O to complete
				if (!LResult.IsCompleted)
				{
					LResult.FAsyncWaitHandle.WaitOne();
					LResult.FIsCompleted = true;
				}

				// Throw an error if one occurred
				if (LResult.FErrorCode != 0)
					ThrowWinIOError(LResult.FFileName, LResult.FErrorCode);

				return LResult.FSize;
			}
			finally
			{
				// Relinquish the async result to the pool
				RelinquishAsyncResult(LResult);
			}
		}

		#endregion

		#region Length

		public unsafe long Length
		{
			get
			{
				int LLow = 0;
				int LHigh = 0;
				LLow = GetFileSize(FHandle, out LHigh);
				if (LLow == -1)
				{
					int LResult = Marshal.GetLastWin32Error();
					if (LResult != 0)
						ThrowWinIOError(FFileName, LResult);
				}
				return (LHigh << 0x20) | LLow;
			}
			set
			{
				int LLow = (int)value;
				int LHigh = (int)(value >> 0x20);
				SetFilePointer(FHandle, LLow, &LHigh, (int)SeekOrigin.Begin);
				if (LLow == -1)
				{
					ThrowWinIOError(FFileName, Marshal.GetLastWin32Error());
					Error.Fail("Unknown Error Condition With SetFilePointer");
				}
				SetEndOfFile(FHandle);
			}
		}

		#endregion

		#region Win32 Helpers

		private static void ThrowWinIOError(string AFileName)
		{
			ThrowWinIOError(AFileName, Marshal.GetLastWin32Error());
		}

		private static void ThrowWinIOError(string AFileName, int AErrorCode)
		{
			switch (AErrorCode)
			{
				case 2 : throw new StorageException(StorageException.Codes.FileNotFound, AFileName);
				case 3 : throw new StorageException(StorageException.Codes.PathNotFound, AFileName);
				case 5 : throw new StorageException(StorageException.Codes.UnauthorizedAccess, AFileName);
				case 0x20 : throw new StorageException(StorageException.Codes.SharingViolation, AFileName);
				case 80 : throw new StorageException(StorageException.Codes.FileAlreadyExists, AFileName);
				case 0xCE : throw new StorageException(StorageException.Codes.PathToLong, AFileName);
				case 0 : return;
				default : throw new StorageException(StorageException.Codes.GeneralIOError, new IOException(GetWin32Message(AErrorCode), MakeHRFromErrorCode(AErrorCode)), AFileName);
			}
		}

		private static int MakeHRFromErrorCode(int AErrorCode)
		{
			return -2147024896 | AErrorCode;
		}

		private static string GetWin32Message(int AErrorCode)
		{
			StringBuilder LResult = new StringBuilder(0x200);
			int LReturn = FormatMessage(12800, IntPtr.Zero, AErrorCode, 0, LResult, LResult.Capacity, IntPtr.Zero);
			if (LReturn != 0)
				return LResult.ToString();
			return Strings.Get("Storage.UnknownIOError", AErrorCode);
		}

		#endregion

		#region Win32

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, FileShare dwShareMode, IntPtr securityAttrs, FileMode dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(SafeFileHandle handle);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        internal static extern bool SetEndOfFile(SafeFileHandle hFile);
 
		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        private static extern unsafe int SetFilePointer(SafeFileHandle handle, int lo, int* hi, int origin);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
		private static extern unsafe int WriteFile(SafeFileHandle handle, IntPtr buffer, int numBytesToWrite, IntPtr numBytesWritten, NativeOverlapped* lpOverlapped);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        private static extern unsafe int WriteFileGather(SafeFileHandle hFile, FILE_SEGMENT_ELEMENT* aSegmentArray, int nNumberOfBytesToWrite, IntPtr lpReserved, NativeOverlapped* lpOverlapped);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        private static extern unsafe int ReadFile(SafeFileHandle handle, IntPtr bytes, int numBytesToRead, IntPtr numBytesRead, NativeOverlapped* overlapped);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        private static extern unsafe int ReadFileScatter(SafeFileHandle hFile, FILE_SEGMENT_ELEMENT* aSegmentArray, int nNumberOfBytesToRead, IntPtr lpReserved, NativeOverlapped* lpOverlapped);
 
		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto)]
		internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr va_list_arguments);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
		internal static extern void GetSystemInfo(out SYSTEM_INFO si);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", SetLastError=true)]
        internal static extern int GetFileSize(SafeFileHandle hFile, out int highSize);
 
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		internal struct FILE_SEGMENT_ELEMENT
		{
			[FieldOffset(0)]
			public IntPtr Buffer;
			[FieldOffset(0)]
			public UInt64 Alignment;
		}

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		public struct SYSTEM_INFO
		{
			public ushort wProcessorArchitecture;
			public ushort wReserved;
			public uint dwPageSize;
			public IntPtr lpMinimumApplicationAddress;
			public IntPtr lpMaximumApplicationAddress;
			public IntPtr dwActiveProcessorMask;
			public uint dwNumberOfProcessors;
			public uint dwProcessorType;
			public uint dwAllocationGranularity;
			public ushort wProcessorLevel;
			public ushort wProcessorRevision;
		}

		#endregion
	}
}
