/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.DAE.Storage
{
	/*
		File Page layout ->
			-Every 2^10 page is an allocation page
			-Every 2^10 allocation page is also a super page
			-The first super page is also a master page
			
			P is Page Header
			A is Allocation Header
			S is Super Header
			M is Master Header
			---------------------------------------------------------->
			0x00000 0x00001	... 0x00200 0x00201	... 0x80000 0x80001 ...
			[PASM-] [PDDDD] ... [PA---] [PDDDD] ... [PAS--] [PDDDD] ...
			---------------------------------------------------------->
		
		Allocation Header ->
			-Allocation header contains a UseMap array of 32x32bit uints providing 1024 bits 
			 of allocation information
			-Each bit in the UseMap represents the allocation status of a single page
			-Individual UseMap bit indexes correspond with a page number relative to the allocation page
			-The UseMap's 0th bit is always allocated because the allocation page itself is not available
			 for allocation
			-Allocation header contains a MetaUseMap 32bit uint
			-Each bit in the MetaUseMap represents the allocation status of a 32bit block in the UseMap
			-Individual MetaUseMap bit indexes correspond with 32bit offsets within the UseMap
			-MetaUseMap bits are set when all bits in the corresponding UseMap block are set (allocated)
			
			|--32bits--|
			[0][1]...[0]
			 |	|	  |
			 |	|	  +------------------------+
			 |	|							   |
			 |	+--------------+			   |
			 +----+			   |			   |
			      |			   |			   |
			[0010...0110][1111...1111]...[0000...0000]
			|---32bits--|
			|-----------------1024bits---------------|
			
		Super Header ->
			-Super header contains a UseMap array of 32x32bit uints providing 1024 bits
			 of allocation status information
			-Each UseMap bit corresponds with an allocation page
			-The index if each UseMap bit corresponds with an allocation page relative to
			 the super page's position.
			-A bit in the UseMap is set when all bits in the corresponding allocation page
			 are set
			-Super header contains a MetaUseMap uint which acts just like the MetaUseMap
			 of the alloc header.
			 
		Master Header ->
			-Master header contains a UseMap array of 128x32bit uints providing 4096 bits
			 of allocation status information
			-Each UseMap bit corresponds with a super page
			-The index of each UseMap bit corresponds with a super page's position
			-A bit in the UseMap is set when all bits in the corresponding super page are set
			-Master header contains a MetaUseMap array of 4x32 bit uints providing 128 bits
			 of allocation status information
			-The MetaUseMap of the master header is similar to the alloc and super headers
			 except that it is an array of 4 uints instead of 1 uint.
			-The master header also has a file size which represents the size of the file
			 in pages
			-Portions of the file which are beyond the currently allocated size of the file
			 are flagged as unallocated.
		
			
		Allocation Conversions ->
		
			SuperNumber =	|  PageNumber   |
							|  -----------  |
							|_ 2^20		   _|

			AllocNumber =	|  ((PageNumber & (2^20 - 1))   |
							|  ---------------------------  |
							|_ 2^10						   _|

			DataNumber = PageNumber & (2^10 - 1)

			PageNumber = (SuperNumber * 2^20) + (AllocNumber * 2^10) + DataNumber
			PageNumber = (SuperNumber * 2^20) + (AllocNumber * 2^10)
			PageNumber = SuperNumber * 2^20
	*/

	[StructLayout(LayoutKind.Explicit, Size = 132)]
	public struct AllocPageData
	{
		[FieldOffset(0)]	public uint MetaUseMap;
		[FieldOffset(4)]	public uint UseMap;	//uint[32]
	}

	[StructLayout(LayoutKind.Explicit, Size = 132)]
	public struct AllocPageHeader
	{
		[FieldOffset(0)]	public AllocPageData AllocPageData;
	}

	[StructLayout(LayoutKind.Explicit, Size = 264)]
	public struct SuperPageHeader
	{
		[FieldOffset(0)]	public AllocPageData AllocPageData;
		[FieldOffset(132)]	public AllocPageData SuperPageData;
	}

	[StructLayout(LayoutKind.Explicit, Size = 796)]
	public struct MasterPageHeader
	{
		[FieldOffset(0)]	public AllocPageData AllocPageData;
		[FieldOffset(132)]	public AllocPageData SuperPageData;
		[FieldOffset(264)]	public uint MetaUseMap;	//uint[4]
		[FieldOffset(276)]	public uint UseMap;	//uint[128]
		[FieldOffset(788)]	public long InitializedPages;
	}

	/// <summary> Manages page allocation. </summary>
	public class AllocationManager
	{
		public const uint CSuperPageMask = 0x000FFFFF;
		public const uint CAllocPageMask = 0x000003FF;
		public const uint CUseMapMask = 0x0000001F;
		public const uint CMostSignificantMask = 0x80000000;
		public const int CMasterPageNumber = 0;
		public const int CSuperPageShift = 20;
		public const int CAllocPageShift = 10;
		public const int CIntSizeShift = 5;
		public const int CIntSize = 32;

		public const uint CDefaultFileGrowthAmount = 256;

		public AllocationManager(BufferManager ABufferManager, LockManager ALockManager, FileManager AFileManager)
		{
			FBufferManager = ABufferManager;
			FLockManager = ALockManager;
			FFileManager = AFileManager;

			FDefaultFileGrowth.Amount = CDefaultFileGrowthAmount;

			// Make sure all files are initialized
			foreach (DataFile LDataFile in FBufferManager.DataFiles)
				if (LDataFile.Size == 0)
					InitializeFile(LDataFile);

			// Default the active file to the last file in the list
			if (FBufferManager.DataFiles.Count > 0)
				FActiveFileNumber = ((DataFile)FBufferManager.DataFiles[FBufferManager.DataFiles.Count - 1]).FileNumber;
			else
				FActiveFileNumber = 0;
		}

		private BufferManager FBufferManager;
		public BufferManager BufferManager
		{
			get { return FBufferManager; }
		}

		private LockManager FLockManager;
		public LockManager LockManager
		{
			get { return FLockManager; }
		}

		private FileManager FFileManager;
		public FileManager FileManager
		{
			get { return FFileManager; }
		}

		private uint FActiveFileNumber;
		/// <summary> The file number to use when the a file number is not specified. </summary>
		public uint ActiveFileNumber
		{
			get
			{
				lock (this)
				{
					if (FActiveFileNumber == 0)
						FActiveFileNumber = AllocateNewFile();
					return FActiveFileNumber;
				}
			}
			set { FActiveFileNumber = value; }
		}

		private Quantifier FDefaultFileGrowth;
		/// <summary> The default file growth characteristics. </summary>
		public Quantifier DefaultFileGrowth
		{
			get { return FDefaultFileGrowth; }
			set { FDefaultFileGrowth = value; }
		}

		/// <summary> Allocates a data page with preferencial proximity to a specified page. </summary>
		/// <param name="AProximity"> The page by which proximity is requested. </param>
		/// <returns> The newly allocated page's PageID. </returns>
		public PageID AllocatePage(PageID AProximity)
		{
			// TODO: Contiguous allocation
			return AllocatePage(AProximity.FileNumber, false);
		}

		/// <summary> Allocates a data page within a specified file (if possible). </summary>
		/// <param name="AFileNumber"> The file to allocate a page within. </param>
		/// <param name="ARequired">
		///		Indicates that the caller requires that the page be within the 
		///		specified file.  If true, and the page cannot be allocated, an
		///		exception is thrown.  If false, and the page cannot be allocated,
		///		another file will be used or created.
		/// </param>
		/// <returns> The allocated page's PageID. </returns>
		public unsafe PageID AllocatePage(uint AFileNumber, bool ARequired)
		{
			MasterPageHeader LHeaderCopy;
			PageID LMasterPageID = new PageID(AFileNumber, CMasterPageNumber);

			for (;;)
			{
				LockManager.Lock(LMasterPageID, false);
				try
				{
					BufferNode LMasterPage = FBufferManager.Fix(LMasterPageID, false);
					try
					{
						fixed (byte* LBufferPtr = &(LMasterPage.Data[0]))
							LHeaderCopy = *((MasterPageHeader*)LBufferPtr);
					}
					finally
					{
						FBufferManager.Unfix(LMasterPage);
					}
					for (uint i = 0; i < 4; i++)
					{
						int LMetaIndex = MostSignificantZBitIndex((&(LHeaderCopy.MetaUseMap))[i]);
						if (LMetaIndex < CIntSize)		// Spot available?
						{
							int LIndex = MostSignificantZBitIndex((&(LHeaderCopy.UseMap))[(i << CIntSizeShift) + LMetaIndex]);
							uint LPageNumber = (uint)(((((i << CIntSizeShift) + LMetaIndex) << CIntSizeShift) + LIndex) << CSuperPageShift);
							if (AllocateFromSuperPage(new PageID(AFileNumber, LPageNumber), LHeaderCopy.FileSize, out LPageNumber))
								return new PageID(AFileNumber, LPageNumber);
							else
								break;
						}
						else
						{
							if (i == 3)
							{
								if (ARequired)
									throw new StorageException(StorageException.CFileOutOfSpace, AFileNumber));
								return AllocatePage(GetNextFileNumber(AFileNumber), false);
							}
						}
					}
				}
				finally
				{
					LockManager.Release(LMasterPageID);
				}
			}
		}

		/// <summary> Allocates a data page within the currently active file. </summary>
		/// <returns> The allocated page's PageID. </returns>
		public PageID AllocatePage()
		{
			return AllocatePage(ActiveFileNumber, false);
		}

		/// <summary> Deallocates a given page. </summary>
		/// <param name="APageID"> The PageID of the page which is to be deallocated. </param>
		public unsafe void DeallocPage(PageID APageID)
		{
			//Ensure that the deallocated page is not a master/super/alloc page
			if ((APageID.PageNumber & CAllocPageMask) == 0)
				throw new StorageException(String.Format(CCannotDeallocateAllocationPage, APageID.ToString()));

			PageID LAllocPageID = new PageID(APageID.FileNumber, APageID.PageNumber >> CAllocPageShift);

			for (;;)
			{
				LockManager.Lock(LAllocPageID, false);
				try
				{
					AllocPageHeader LHeaderCopy;
					BufferNode LPage = FBufferManager.Fix(LAllocPageID, false);
					try
					{
						fixed (byte* LBufferPtr = &(LPage.Data[0]))
							LHeaderCopy = *((AllocPageHeader*)LBufferPtr);
					}
					finally
					{
						FBufferManager.Unfix(LPage);
					}

					// TODO: Handle automatic shrinkage

					int LUseMapIndex = (int)((APageID.PageNumber - LAllocPageID.PageNumber) >> CAllocPageShift);
					uint LUseMap = (&(LHeaderCopy.AllocPageData.UseMap))[LUseMapIndex];

					if (!LockManager.LockImmediate(LAllocPageID, true))
						continue;
					try
					{
						uint LMetaMask;
						// Handle case where UseMap becomes no longer full
						if (LUseMap == UInt32.MaxValue)
						{
							// Handle case where MetaUseMap becomes no longer full
							if (LHeaderCopy.AllocPageData.MetaUseMap == UInt32.MaxValue)
							{
								if (!UpdateSuperPageForAllocPage(LAllocPageID, false))
									continue;
							}
							LMetaMask = CMostSignificantMask >> LUseMapIndex;
						}
						else
							LMetaMask = UInt32.MinValue;

						uint LUseMapMask = CMostSignificantMask >> (int)((APageID.PageNumber - LAllocPageID.PageNumber) & CUseMapMask);
						
						UpdateAllocPage(APageID, LMetaMask, LUseMapIndex, LUseMapMask);
						break;
					}
					finally
					{
						LockManager.Release(LAllocPageID);
					}
				}
				finally
				{
					LockManager.Release(LAllocPageID);
				}
			}
		}

		/// <summary> Attempts to resize the data file. </summary>
		/// <param name="AFileNumber"> The file number of the file to resize. </param>
		/// <param name="ANewSize"> The new size (in pages). </param>
		/// <returns>
		///		True if the update took place.  False, if an immediate 
		///		lock fails.
		/// </returns>
		private unsafe bool InternalResize(uint AFileNumber, uint ANewSize)
		{
			DataFile LDataFile = BufferManager.DataFiles[AFileNumber];
			PageID LMasterPageID = new PageID(AFileNumber, CMasterPageNumber);

			LockManager.Lock(LMasterPageID, false);
			try
			{
				uint LOldSize;
				BufferNode LMasterPage = BufferManager.Fix(LMasterPageID, false);
				try
				{
					fixed (byte* LBufferPtr = &(LMasterPage.Data[0]))
						LOldSize = ((MasterPageHeader*)LBufferPtr)->FileSize;
				}
				finally
				{
					BufferManager.Unfix(LMasterPage);
				}

				if (LOldSize < ANewSize)	// Growing
				{
					LDataFile.Size = ANewSize;
					// Initialize each new page
					for (uint i = LOldSize; i < ANewSize; i++)
					{
						if ((i & CSuperPageMask) == 0)			// This is a super page
							InitializeSuperPage(new PageID(AFileNumber, i));
						else if ((i & CAllocPageMask) == 0)		// This is an alloc page
							InitializeAllocPage(new PageID(AFileNumber, i));
						else									// This is a data page
							InitializeDataPage(new PageID(AFileNumber, i));
					}
					return UpdateMasterPageFileSize(LMasterPageID, ANewSize);
				}
				else						// Shrinking
				{
					// Ensure that no alloced pages are discarded
					// TODO: Finish File Shrinking
					//throw new StorageException(String.Format(CUnableToShrinkFile, AFileNumber));

					// Update the master page size
					if (UpdateMasterPageFileSize(LMasterPageID, ANewSize))
					{
						// Shrink the DataFile
						LDataFile.Size = ANewSize;
						return true;
					}
					else
						return false;
				}
			}
			finally
			{
				LockManager.Release(LMasterPageID);
			}
		}
		
		/// <summary> Resizes the specified data file. </summary>
		/// <param name="AFileNumber"> The FileNumber of the file to resize. </param>
		/// <param name="ANewSize"> The new size (in pages) for the file. </param>
		public unsafe void ResizeFile(uint AFileNumber, uint ANewSize)
		{
			// Keep trying to resize until we are successful
			for (;;)
			{
				if (InternalResize(AFileNumber, ANewSize))
					return;
			}
		}

		/// <summary> Updates the usage map information on the master page. </summary>
		/// <remarks> It is assumed that an exclusive lock already be on the master page before this call. </remarks>
		/// <param name="APageID"> The page ID for the master page. </param>
		/// <param name="AMetaIndex"> The index of the MetaUseMap to modify. </param>
		/// <param name="AMetaMask"> The mask to XOR with the said MetaUseMap. </param>
		/// <param name="AUseMapIndex"> The index of the UseMap to modify. </param>
		/// <param name="AUseMask"> The mask to XOR with the said UseMap. </param>
		private unsafe void UpdateMasterPage(PageID APageID, uint AMetaIndex, uint AMetaMask, uint AUseMapIndex, uint AUseMask)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
				{
					MasterPageHeader* LHeader = (MasterPageHeader*)LBufferPtr;
					(&(LHeader->MetaUseMap))[AMetaIndex] ^= AMetaMask;
					(&(LHeader->UseMap))[AUseMapIndex] ^= AUseMask;
				}
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		/// <summary> Updates the master page's usage maps for changes to a specified super page. </summary>
		/// <param name="ASuperPageID"> The super page which has changed. </param>
		/// <param name="AFull"> True if the super page has become full.  False if the super page has become not full. </param>
		/// <returns> False if we are unable to place an exclusive lock on the master page. </returns>
		private unsafe bool UpdateMasterPageForSuperPage(PageID ASuperPageID, bool AFull)
		{
			PageID LMasterPageID = new PageID(ASuperPageID.FileNumber, CMasterPageNumber);

			LockManager.Lock(LMasterPageID, false);
			try
			{
				MasterPageHeader LHeaderCopy;
				BufferNode LPage = BufferManager.Fix(ASuperPageID, false);
				try
				{
					fixed (byte* LBufferPtr = &(LPage.Data[0]))
						LHeaderCopy = *((MasterPageHeader*)LBufferPtr);
				}
				finally
				{
					BufferManager.Unfix(LPage);
				}

				uint LUseMapOffset = ASuperPageID.PageNumber >> CSuperPageShift;
				uint LUseMapIndex = LUseMapOffset >> CIntSizeShift;
				uint LUseMask = CMostSignificantMask >> (int)(LUseMapOffset & CUseMapMask);
				uint LUseMap = (&(LHeaderCopy.UseMap))[LUseMapIndex];

				if (!LockManager.LockImmediate(LMasterPageID, true))
					return false;
				try
				{
					uint LMetaMask;
					uint LMetaIndex = LUseMapIndex >> (CIntSizeShift + CIntSizeShift);
					// Handle full UseMap
					if ((AFull && ((LUseMap ^ LUseMask) == UInt32.MaxValue)) || (!AFull && (LUseMap == UInt32.MaxValue)))
						LMetaMask = CMostSignificantMask >> (int)((LUseMapIndex >> CIntSizeShift) - ((LUseMapIndex >> CIntSizeShift) & CUseMapMask));
					else
						LMetaMask = UInt32.MinValue;

					UpdateMasterPage(LMasterPageID, LMetaIndex, LMetaMask, LUseMapIndex, LUseMask);
					return true;
				}
				finally
				{
					LockManager.Release(LMasterPageID);
				}
			}
			finally
			{
				LockManager.Release(LMasterPageID);
			}
		}

		/// <summary> Updates a SuperPage's use map. </summary>
		/// <param name="APageID">
		///		The SuperPage PageID to lock. An excusive lock is assumed to 
		///		already exist on the specified page.
		///	</param>
		/// <param name="AMetaMask"> The bitmask to XOR with the page's MetaUseMap. </param>
		/// <param name="AUseMapIndex"> The index of the usemap to change. </param>
		/// <param name="AUseMask"> The bitmask to XOR with the page's specified UseMap. </param>
		private unsafe void UpdateSuperPage(PageID APageID, uint AMetaMask, uint AUseMapIndex, uint AUseMask)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
				{
					SuperPageHeader* LHeader = (SuperPageHeader*)LBufferPtr;
					LHeader->SuperPageData.MetaUseMap ^= AMetaMask;
					(&(LHeader->SuperPageData.UseMap))[AUseMapIndex] ^= AUseMask;
				}
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		/// <summary> Updates the super page for changes to the specified allocation page. </summary>
		/// <param name="AAllocPageID"> The pageID for the allocation page which has changed. </param>
		/// <param name="AFull"> When true, the alloc page has become full.  Otherwise it has become not full. </param>
		/// <returns> Returns false if an exclusive lock could not be acquired on the super page. </returns>
		private unsafe bool UpdateSuperPageForAllocPage(PageID AAllocPageID, bool AFull)
		{
			PageID LSuperPageID = new PageID(AAllocPageID.FileNumber, AAllocPageID.PageNumber & ~CSuperPageMask);

			LockManager.Lock(LSuperPageID, false);
			try
			{
				SuperPageHeader LHeaderCopy;
				BufferNode LPage = BufferManager.Fix(LSuperPageID, false);
				try
				{
					fixed (byte* LBufferPtr = &(LPage.Data[0]))
						LHeaderCopy = *((SuperPageHeader*)LBufferPtr);
				}
				finally
				{
					BufferManager.Unfix(LPage);
				}

				uint LUseMapOffset = (AAllocPageID.PageNumber - LSuperPageID.PageNumber) >> CAllocPageShift;
				uint LUseMapIndex = LUseMapOffset >> CIntSizeShift;
				uint LUseMask = CMostSignificantMask >> (int)(LUseMapOffset - (LUseMapIndex << CIntSizeShift));
				uint LUseMap = (&(LHeaderCopy.SuperPageData.UseMap))[LUseMapIndex];

				if (!LockManager.LockImmediate(LSuperPageID, true))
					return false;
				try
				{
					// Handle full UseMap
					uint LMetaMask;
					if ((AFull && ((LUseMap ^ LUseMask) == UInt32.MaxValue)) || (!AFull && (LUseMap == UInt32.MaxValue)))
					{
						LMetaMask = CMostSignificantMask >> (int)(LUseMapIndex >> CIntSizeShift);
						// Handle full MetaUseMap
						if ((AFull & ((LMetaMask ^ LHeaderCopy.SuperPageData.MetaUseMap) == UInt32.MaxValue)) || (!AFull && (LHeaderCopy.SuperPageData.MetaUseMap == UInt32.MaxValue)))
						{
							if (!UpdateMasterPageForSuperPage(LSuperPageID, AFull))
								return false;
						}
					}
					else
						LMetaMask = UInt32.MinValue;

					UpdateSuperPage(LSuperPageID, LMetaMask, LUseMapIndex, LUseMask);
					return true;
				}
				finally
				{
					LockManager.Release(LSuperPageID);
				}
			}
			finally
			{
				LockManager.Release(LSuperPageID);
			}
		}

		/// <summary> Updates an AllocPage's use map. </summary>
		/// <param name="APageID">
		///		The AllocPage PageID to lock. An excusive lock is assumed to 
		///		already exist on the specified page.
		///	</param>
		/// <param name="AMetaMask"> The bitmask to XOR with the page's MetaUseMap. </param>
		/// <param name="AUseMapIndex"> The index of the usemap to change. </param>
		/// <param name="AUseMask"> The bitmask to XOR with the page's specified UseMap. </param>
		private unsafe void UpdateAllocPage(PageID APageID, uint AMetaMask, int AUseMapIndex, uint AUseMask)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
				{
					AllocPageHeader* LHeader = (AllocPageHeader*)LBufferPtr;
					LHeader->AllocPageData.MetaUseMap ^= AMetaMask;
					(&(LHeader->AllocPageData.UseMap))[AUseMapIndex] ^= AUseMask;
				}
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		/// <summary> Allocates a new page from a specified allocation page. </summary>
		/// <param name="APageID"> The PageID of the allocation page to allocate from. </param>
		/// <param name="AFileSize">
		///		The current size of the file so that the allocation can ensure that
		///		the file is grown properly.
		///	</param>
		/// <param name="APageNumber"> The resulting page number which has been allocated. </param>
		/// <returns> False if exclusive locks were not able to be acquired. </returns>
		private unsafe bool AllocateFromAllocPage(PageID APageID, uint AFileSize, out uint APageNumber)
		{
			LockManager.Lock(APageID, false);
			try
			{
				AllocPageHeader LHeaderCopy;
				BufferNode LPage = FBufferManager.Fix(APageID, false);
				try
				{
					fixed (byte* LBufferPtr = &(LPage.Data[0]))
						LHeaderCopy = *((AllocPageHeader*)LBufferPtr);
				}
				finally
				{
					FBufferManager.Unfix(LPage);
				}
				int LUseMapIndex = MostSignificantZBitIndex(LHeaderCopy.AllocPageData.MetaUseMap);
				uint LUseMap = (&(LHeaderCopy.AllocPageData.UseMap))[LUseMapIndex];
				int LPageIndex = MostSignificantZBitIndex(LUseMap);
				APageNumber = (uint)(APageID.PageNumber + ((LUseMapIndex << CIntSizeShift) + LPageIndex));

				if (!LockManager.LockImmediate(APageID, true))
					return false;
				try
				{
					// Make sure that the file extends to the alloced page
					if (APageNumber > AFileSize)
						if (!GrowFile(APageID.FileNumber, APageNumber))
							return false;

					// Handle UseMap filling
					uint LUseMask = CMostSignificantMask >> LPageIndex;
					uint LMetaMask;
					if ((LUseMask ^ LUseMap) == UInt32.MaxValue)	// Usemap is full.
					{
						LMetaMask = CMostSignificantMask >> LUseMapIndex;
						// Handle MetaUseMap filling
						if ((LMetaMask ^ LHeaderCopy.AllocPageData.MetaUseMap) == UInt32.MaxValue)
						{
							if (!UpdateSuperPageForAllocPage(APageID, true))
								return false;
						}
					}
					else
						LMetaMask = UInt32.MinValue;
				
					UpdateAllocPage(APageID, LMetaMask, LUseMapIndex, LUseMask);
					return true;
				}
				finally
				{
					LockManager.Release(APageID);
				}
			}
			finally
			{
				LockManager.Release(APageID);
			}
		}

		/// <summary> Allocates a page from the super page. </summary>
		/// <param name="APageID"> The page id of the super page from which to allocate from. </param>
		/// <param name="AFileSize"> The current files size of the file so allocation can ensure the file is grown properly. </param>
		/// <param name="APageNumber"> The resulting page number. </param>
		/// <returns> False if an exclusive lock could not be placed a page. </returns>
		private unsafe bool AllocateFromSuperPage(PageID APageID, uint AFileSize, out uint APageNumber)
		{
			SuperPageHeader LHeaderCopy;
			LockManager.Lock(APageID, false);
			try
			{
				BufferNode LPage = FBufferManager.Fix(APageID, false);
				try
				{
					fixed (byte* LBufferPtr = &(LPage.Data[0]))
						LHeaderCopy = *((SuperPageHeader*)LBufferPtr);
				}
				finally
				{
					FBufferManager.Unfix(LPage);
				}
				int LMetaIndex = MostSignificantZBitIndex(LHeaderCopy.SuperPageData.MetaUseMap);
				return AllocateFromAllocPage
					(
						new PageID
						(
							APageID.FileNumber,
							(uint)(APageID.PageNumber + (((LMetaIndex << CIntSizeShift) + MostSignificantZBitIndex((&(LHeaderCopy.SuperPageData.UseMap))[LMetaIndex])) << CAllocPageShift))
						), 
						AFileSize,
						out APageNumber
					);
			}
			finally
			{
				LockManager.Release(APageID);
			}
		}

		private unsafe void InitializePageHeader(PageHeader* AHeader, PageID APageID)
		{
			AHeader->PageID = APageID;
		}

		private unsafe void InitializeAllocHeader(AllocPageHeader* AAllocHeader, PageID APageID)
		{
			InitializePageHeader(&(AAllocHeader->PageHeader), APageID);
			AAllocHeader->AllocPageData.UseMap = CMostSignificantMask;	// Allocation page itself is allocated
		}

		private unsafe void InitializeSuperHeader(SuperPageHeader* ASuperHeader, PageID APageID)
		{
			InitializeAllocHeader(&(ASuperHeader->AllocPageHeader), APageID);
			// No allocation pages are fully alloced, so no map bits are set
		}

		private unsafe void InitializeMasterHeader(MasterPageHeader* AMasterHeader, PageID APageID)
		{
			InitializeSuperHeader(&(AMasterHeader->SuperPageHeader), APageID);
			// No super pages are fully alloced, so no map bits are set
		}

		private unsafe void InitializeMasterPage(uint AFileNumber)
		{
			BufferNode LPage = FBufferManager.Fix(new PageID(AFileNumber, CMasterPageNumber), true);
			try
			{
				Array.Clear(LPage.Data, 0, LPage.Data.Length);
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
					InitializeMasterHeader((MasterPageHeader*)LBufferPtr, new PageID(AFileNumber, CMasterPageNumber));
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		private unsafe void InitializeSuperPage(PageID APageID)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				Array.Clear(LPage.Data, 0, Int32.MaxValue);
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
					InitializeSuperHeader((SuperPageHeader*)LBufferPtr, APageID);
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		private unsafe void InitializeAllocPage(PageID APageID)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				Array.Clear(LPage.Data, 0, Int32.MaxValue);
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
					InitializeAllocHeader((AllocPageHeader*)LBufferPtr, APageID);
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		private unsafe void InitializeDataPage(PageID APageID)
		{
			BufferNode LPage = FBufferManager.Fix(APageID, true);
			try
			{
				Array.Clear(LPage.Data, 0, Int32.MaxValue);
				fixed (byte* LBufferPtr = &(LPage.Data[0]))
					InitializePageHeader((PageHeader*)LBufferPtr, APageID);
				LPage.Modify();
			}
			finally
			{
				FBufferManager.Unfix(LPage);
			}
		}

		/// <summary> Initializes a new file. </summary>
		/// <remarks> Initializes the new file with the master header page. </remarks>
		/// <param name="ADataFile"> The datafile to initialize. </param>
		private void InitializeFile(DataFile ADataFile)
		{
			InitializeMasterPage(ADataFile.FileNumber);
		}

		/// <summary> Increases the file size by the given growth increment. </summary>
		/// <param name="AFileNumber"> The file number to grow. </param>
		/// <param name="AMinimumSize"> The minimum size that the file must grow to. </param>
		/// <returns> False if an exclusive lock could not be obtained. </returns>
		public bool GrowFile(uint AFileNumber, uint AMinimumSize)
		{
			return InternalResize(AFileNumber, (uint)FDefaultFileGrowth.QuantifyGrowth(AMinimumSize));
		}

		/// <summary> Allocates a new file using the incrementing file number system of the file manager. </summary>
		/// <returns> The new file which was allocated. </returns>
		private uint AllocateNewFile()
		{
			return FileManager.NewDataFile().FileNumber;
		}

		private uint GetNextFileNumber(uint AFileNumber)
		{
			uint LLastFileNumber = 0;
			foreach (DataFile LDataFile in BufferManager.DataFiles)
			{
				if (LLastFileNumber == AFileNumber)
					return LDataFile.FileNumber;
				LLastFileNumber = LDataFile.FileNumber;
			}
			return AllocateNewFile();
		}

		/// <summary> Updates the file size stored with the master page. </summary>
		/// <param name="APageID"> The PageID of the master page. </param>
		/// <param name="ANewSize"> The new size of the file (in pages). </param>
		/// <returns> False if an exclusive lock could not be acquired. </returns>
		private unsafe bool UpdateMasterPageFileSize(PageID APageID, uint ANewSize)
		{
			if (!LockManager.LockImmediate(APageID, true))
				return false;
			try
			{
				BufferNode LMasterPage = BufferManager.Fix(APageID, true);
				try
				{
					fixed (byte* LBufferPtr = &(LMasterPage.Data[0]))
						((MasterPageHeader*)LBufferPtr)->FileSize = ANewSize;
					LMasterPage.Modify();
				}
				finally
				{
					BufferManager.Unfix(LMasterPage);
				}
			}
			finally
			{
				LockManager.Release(APageID);
			}
			return true;
		}
	}
}
