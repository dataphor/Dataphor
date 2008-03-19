/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Storage
{
	public sealed class AllocationUtility
	{
		/// <summary> Finds the index of the most significant non-zero bit. </summary>
		/// <remarks> Performs a binary search. </remarks>
		/// <returns>
		///		Zero based bit index of the most significant non-zero bit, or
		///		32 (out of range) for a value with all bits set.
		///	</returns>
		public static int MostSignificantZBitIndex(uint AValue)
		{
			uint LMasked;
			int LResult = 0;
			if (AValue == 0xFFFFFFFF)	//Account for out of range
				return 32;
			LMasked = AValue & 0xFFFF0000;
			if (LMasked == 0)
			{
				LResult |= 16;
				AValue = LMasked;
			}
			LMasked = AValue & 0xFF00FF00;
			if (LMasked == 0)
			{
				LResult |= 8;
				AValue = LMasked;
			}
			LMasked = AValue & 0xF0F0F0F0;
			if (LMasked == 0)
			{
				LResult |= 4;
				AValue = LMasked;
			}
			LMasked = AValue & 0xCCCCCCCC;
			if (LMasked == 0)
			{
				LResult |= 2;
				AValue = LMasked;
			}
			if ((AValue & 0xAAAAAAAA) == 0)
				LResult |= 1;
			return LResult;
		}
	}
}
