/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> Provides Stream Allocation, Deallocation and Location. </summary>
	public class StreamDirectory
	{
		public StreamDirectory(AllocationManager AAllocationManager, uint ADirectoryFileNumber)
		{
			FAllocationManager = AAllocationManager;
			FDirectoryFileNumber = ADirectoryFileNumber;
		}

		private AllocationManager FAllocationManager;
		private uint FDirectoryFileNumber;

		public ulong Allocate(uint ADataFileNumber)
		{
		}

		public void Deallocate()
		{
		}


	}
}
