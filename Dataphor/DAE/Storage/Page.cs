/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Runtime.InteropServices;

/*
	Page IDs ->
		-Page IDs identify a unique page within the entire data file set
		-Page IDs are not necessarily contiguous because files in general will be smaller than 2^54 pages
		-File Numbers are not necessarily contiguous either
		-File Numbers range from 1 to 1023 (0 is reserved)
		-Page ID 0 is a special value representing an unknown or unapplicable page ID
		-File 1 is the Master File, this file constains the directory of files

		  File #       Page #
		|-10bits-|-----54bits---|
		1111111111000000...000000

*/

namespace Alphora.Dataphor.DAE.Storage
{
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct PageID : IComparable
	{
		[FieldOffset(0)]	public ulong FID;

		public PageID(ulong AID) { FID = AID; }
		public PageID(int AFileNumber, long APageNumber)
		{
			FID = ((ulong)(AFileNumber & 0x3FF) << 54) | ((ulong)APageNumber & 0x3FFFFFFFFFFFFF);
		}

		public int FileNumber { get { return (int)(FID >> 54); } }
		public long PageNumber { get { return (long)(FID & 0x3FFFFFFFFFFFFF); } }

		public static implicit operator PageID(ulong APageID) { return new PageID(APageID); }
		public static explicit operator ulong(PageID AID) { return AID.FID; }

		public static readonly PageID None = new PageID(0);

		public override int GetHashCode()
		{
			return FID.GetHashCode();
		}

		public override bool Equals(object AOther)
		{
			return (AOther is PageID) && (((PageID)AOther).FID == FID);
		}

		public int CompareTo(object ASource)
		{
			ulong LSource = ((PageID)ASource).FID;
			return (FID < LSource ? -1 : (FID > LSource ? 1 : 0));
		}

		public static bool operator >(PageID APage1, PageID APage2)
		{
			return APage1.FID > APage2.FID;
		}

		public static bool operator <(PageID APage1, PageID APage2)
		{
			return APage1.FID < APage2.FID;
		}

		public static bool operator >=(PageID APage1, PageID APage2)
		{
			return APage1.FID >= APage2.FID;
		}

		public static bool operator <=(PageID APage1, PageID APage2)
		{
			return APage1.FID <= APage2.FID;
		}

		public static bool operator ==(PageID APage1, PageID APage2)
		{
			return APage1.FID == APage2.FID;
		}

		public static bool operator !=(PageID APage1, PageID APage2)
		{
			return APage1.FID != APage2.FID;
		}

		public static PageID operator +(PageID APage1, int AOffset)
		{
			unchecked
			{
				return new PageID(APage1.FID + (ulong)AOffset);
			}
		}

		public static PageID operator ++(PageID APage)
		{
			unchecked
			{
				return new PageID(APage.FID + 1);
			}
		}

		public static PageID operator --(PageID APage)
		{
			unchecked
			{
				return new PageID(APage.FID - 1);
			}
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 12)]
	public struct PageHeader
	{
		[FieldOffset(0)]	public PageID PageID;
		[FieldOffset(8)]	public uint CRC32;			// Computed for the page, skipping these 4 bytes
	}

	public interface IPageAddressing
	{
		PageID PageID { get; }
	}
}
