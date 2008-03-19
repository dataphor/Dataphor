/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

/*
	Page Buffer Implementation ->
		-The page buffer is composed of an array of extents.
		-Each extent may be of varying size
		-Extents are never resized, only removed (starting from the last)
		-To shrink the total buffer size, all allocations within the last extent must first be moved to other extents; then the extent is deallocated
		
*/

namespace Alphora.Dataphor.DAE.Storage
{
	public class PageBuffer : Disposable
	{
		private const int CMaxCapacityGrowth = 256;
		private const int CInitialCapacity = 16;

		public PageBuffer(int APageSize)
		{
			FPageSize = APageSize;
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				while (FCount > 0)
					Deallocate();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		private int FPageSize;
		public int PageSize { get { return FPageSize; } }

		private Extent[] FExtents = new Extent[CInitialCapacity];
		
		public Extent this[int AIndex]
		{
			get
			{
				if (AIndex >= FCount)
					throw new ArgumentOutOfRangeException("AIndex");
				return FExtents[AIndex];
			}
		}

		private int FCount;
		public int Count { get { return FCount; } }

		public IntPtr Allocate(int APageCount)
		{
			// Enlarge the capacity if necessary
			if (FCount == FExtents.Length)
			{
				Extent[] LExtents = new Extent[Math.Min(CMaxCapacityGrowth, FExtents.Length * 2)];
				Buffer.BlockCopy(FExtents, 0, LExtents, 0, FExtents.Length);
				FExtents = LExtents;
			}
			// Allocate the extent
			Extent LExtent = new Extent(FPageSize * APageCount);
			FExtents[FCount] = LExtent;
			FCount++;
			return LExtent.Data;
		}

		public void Deallocate()
		{
			if (FCount > 0)
			{
				FCount--;
				FExtents[FCount].Dispose();
				FExtents[FCount] = null;
			}
			else
				Error.Warn("PageBuffer.Dallocate() called with zero extents.");
		}

		public class Extent : Disposable
		{
			/// <summary> Allocates a page aligned extent of the specified size. </summary>
			public Extent(int ASize)
			{
				FSize = ASize;
				FData = MemoryUtility.Allocate(FSize);
			}

			protected override void Dispose(bool ADisposing)
			{
				MemoryUtility.Deallocate(FData);
				FData = IntPtr.Zero;
				FSize = 0;
				base.Dispose(ADisposing);
			}

			private int FSize;
			/// <summary> The size (in bytes) of this extent. </summary>
			public int Size { get { return FSize; } }

			private IntPtr FData;
			/// <summary> The virtual memory address of the beginning of this extent. </summary>
			public IntPtr Data { get { return FData; } }
		}
	}

	public sealed class MemoryUtility
	{
		public static IntPtr Allocate(int ASize)
		{
			// TODO: Investigate using Large Page support.  Would this give us anything?
			IntPtr LResult = VirtualAlloc(IntPtr.Zero, ASize, 0x1000 /* MEM_COMMIT */, 4 /* PAGE_READWRITE */);
			if (LResult == IntPtr.Zero)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			return LResult;
		}

		public static IntPtr Reallocate(IntPtr ASource, int AOldSize, int ANewSize)
		{
			IntPtr LResult = Allocate(ANewSize);
			try
			{
				CopyMemory(LResult, ASource, Math.Min(AOldSize, ANewSize));
				if (!VirtualFree(ASource, AOldSize, 0x8000 /* MEM_RELEASE */))
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
			catch
			{
				VirtualFree(LResult, ANewSize, 0x8000 /* MEM_RELEASE */);
				throw;
			}
			return LResult;
		}

		public static void Deallocate(IntPtr ATarget)
		{
			if (!VirtualFree(ATarget, 0, 0x8000 /* MEM_RELEASE */))
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
		}

		public static void Clear(IntPtr ATarget, int ACount)
		{
			ZeroMemory(ATarget, ACount);
		}

		public static void Move(IntPtr ASource, IntPtr ATarget, int ACount)
		{
			MoveMemory(ATarget, ASource, ACount);
		}

		#region Win32

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, uint fLAllocationIndexType, uint flProtect);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern bool VirtualFree(IntPtr lpAddress, int dwSize, uint dwFreeType);

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);	// Memory must not overlap

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern void MoveMemory(IntPtr Destination, IntPtr Source, int Length);	// Memory can overlap

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		private static extern void ZeroMemory(IntPtr Destination, int Length);

		#endregion
	}
}

/*
		-There are allocation and meta-allocation bitmaps tracking which extents have free pages
		-Each extent maintains a page allocation bitmap

				Error.AssertFail(APageCount < 0x8000, "Extent size must be 2^15 (32768) or fewer pages");

				FAllocation = new uint[(APageCount >> 5) + ((FPageCount & 0x1F) != 0 ? 1 : 0)];
				FMetaAllocation = new uint[(FAllocation.Length >> 5) + ((FAllocation.Length & 0x1F) != 0 ? 1 : 0)];

			private uint FMasterAllocation;
			private uint[] FMetaAllocation;
			private uint[] FAllocation;

			public IntPtr Allocate()
			{
				int LMetaIndex = AllocationUtility.MostSignificantZBitIndex(FMasterAllocation);
				if (LLMetaIndex < FMetaAllocation.Length)
				{
					int LAllocationOffset = AllocationUtility.MostSignificantZBitIndex(FMetaAllocation[LMetaIndex]);
					int LAllocationIndex = (LMetaIndex << 5) | LAllocationOffset;
					if (LAllocationIndex < FAllocation.Length)
					{
						int LPageOffset = AllocationUtility.MostSignificantZBitIndex(FAllocation[LAllocationIndex]);
						int LPageIndex = (LAllocationIndex << 5) | LPageOffset;
						if (LPageIndex < FPageCount)
						{
							// Update the allocation
							FAllocation[LAllocationIndex] |= (0x80000000 >> LPageOffset);
							if (FAllocation[LAllocationIndex] == 0xFFFFFFFF)
							{
								FMetaAllocation[LMetaIndex] |= (0x80000000 >> LAllocationOffset);
								if (FMetaAllocation[LMetaIndex] == 0xFFFFFFFF)
									FMasterAllocation |= (0x80000000 >> LMetaIndex);
							}
							return (IntPtr)((int)FData + LPageIndex);
						}
						else
							return IntPtr.Zero;
					}
					else
						return IntPtr.Zero;
				}
				else
					return IntPtr.Zero;
			}

			public void Deallocate(IntPtr ABuffer)
			{
				int LPageIndex = ((int)ABuffer - (int)FData) / FPageSize;
				if (FAllocation[LPageIndex]
				int LAllocationIndex = LPageIndex >> 5;
				int LMasterIndex = LAllocationIndex >> 5;

			}
*/