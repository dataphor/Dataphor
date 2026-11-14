/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;

#if USEROWMANAGER
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class RowLink : System.Object
	{
		public RowLink() : base() {}

		public Row Row;		
		public RowLink NextLink;
	}
	
	public class RowLinkChain : System.Object
	{
		public RowLinkChain() : base() {}
		
		private RowLink FHead;

		public RowLink Add(RowLink ALink)
		{
			ALink.NextLink = FHead;
			FHead = ALink;
			return ALink;
		}
		
		public RowLink Remove()
		{
			RowLink LLink = FHead;
			FHead = FHead.NextLink;
			return LLink;
		}
		
		public bool IsEmpty()
		{
			return FHead == null;
		}
	}
	
	public class RowManager : System.Object
	{
		private RowLinkChain FAvailable = new RowLinkChain();
		private RowLinkChain FLinkBuffer = new RowLinkChain();		

		private int FMaxRows = 1000;
		public int MaxRows 
		{ 
			get { return FMaxRows; } 
			set { FMaxRows = value; }
		}
		
		private int FRowCount = 0;
		public int RowCount { get { return FRowCount; } }

		private int FUsedRowCount = 0;		
		public int UsedRowCount { get { return FUsedRowCount; } }
		
		private Row AllocateRow()
		{
			#if DEBUG
			// Ensure RowCount not exceeded
			//if (FRowCount >= FMaxRows)
			//	throw new RuntimeException(RuntimeException.Codes.RowManagerOverflow);
			#endif

			FRowCount++;
			return new Row();
		}
		
		private Row GetRow()
		{
			lock (this)
			{
				FUsedRowCount++;
				if (FAvailable.IsEmpty())
					return AllocateRow();
				else
					return FLinkBuffer.Add(FAvailable.Remove()).Row;
			}
		}
		
		public Row RequestRow(IServerProcess AProcess, Schema.IRowType ARowType)
		{
			Row LRow = GetRow();
			LRow.Open(AProcess, ARowType);
			return LRow;
		}

		#if NATIVEROW
		public Row RequestRow(IServerProcess AProcess, Schema.IRowType ARowType, NativeRow ARow)
		{
			Row LRow = GetRow();
			LRow.Open(AProcess, ARowType, ARow);
			return LRow;
		}
		#else		
		public Row RequestRow(IServerProcess AProcess, Schema.IRowType ARowType, Stream AStream)
		{
			Row LRow = GetRow();
			LRow.Open(AProcess, ARowType, AStream);
			return LRow;
		}
		#endif
		
		private RowLink GetRowLink()
		{
			if (FLinkBuffer.IsEmpty())
				return new RowLink();
			else
				return FLinkBuffer.Remove();
		}
		
		public void ReleaseRow(Row ARow)
		{
			ARow.Close();
			lock (this)
			{
				RowLink LLink = GetRowLink();
				LLink.Row = ARow;
				FAvailable.Add(LLink);
				FUsedRowCount--;
			}
		}
	}
}
#endif
