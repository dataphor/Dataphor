/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.Collections;
	using System.Diagnostics;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Memory;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/*
		Contains table and presentation implementations for the cursor engine.
	*/

    #if OnExpression
    public class OnTable : Table
    {
		public OnTable(OnNode ANode, ServerProcess AProcess) : base(ANode, AProcess){}
		
		public new OnNode Node
		{
			get
			{
				return (OnNode)FNode;
			}
		}
		
		protected Row FSourceRow;
		protected IServerCursor FSourceCursor;
		
		protected override void InternalOpen()
		{
			FSourceCursor = Process.ServerSession.RemoteConnect(Node.ServerLink).GetPlan(Node).RemoteCursor;
			FSourceRow = Process.RowManager.RequestRow(Process, Node.DataType.RowType);
		}
		
		protected override void InternalClose()
		{
			if (FSourceRow != null)
			{
				Process.RowManager.ReleaseRow(FSourceRow);
				FSourceRow = null;
			}
			
			if (FSourceCursor != null)
			{
				Process.ServerSession.RemoteConnect(Node.ServerLink).GetPlan(Node).Dispose();
				FSourceCursor = null;
			}
		}
		
		protected override void InternalReset()
		{
			FSourceCursor.Reset();
		}
		
		protected override void InternalSelect(Row ARow)
		{
			FSourceCursor.Select(ARow);
		}
		
		protected override void InternalNext()
		{
			FSourceCursor.Next();
		}
		
		protected override void InternalLast()
		{
			FSourceCursor.Last();
		}
		
		protected override void InternalFirst()
		{
			FSourceCursor.First();
		}
		
		protected override void InternalPrior()
		{
			FSourceCursor.Prior();
		}
		
		protected override bool InternalBOF()
		{
			return FSourceCursor.BOF();
		}
		
		protected override bool InternalEOF()
		{
			return FSourceCursor.EOF();
		}

		private Schema.RowType FBookmarkRowType;
		private Schema.RowType GetBookmarkRowType()
		{
			if (FBookmarkRowType == null)
			{
				FBookmarkRowType = new Schema.RowType();
				FBookmarkRowType.Columns.Add(new Schema.Column(String.Empty, Schema.DataType.SystemGuid));
			}
			return FBookmarkRowType;
		}

		protected override Row InternalGetBookmark()
		{
			Row LBookmarkRow = Process.RowManager.RequestRow(Process, GetBookmarkRowType());
			try
			{
				Scalar LScalar = LBookmarkRow.GetScalarForWrite(0);
				LScalar.GuidConveyor.SetAsGuid(LScalar, FSourceCursor.GetBookmark());
				return LBookmarkRow;
			}
			catch
			{
				Process.RowManager.ReleaseRow(LBookmarkRow);
				throw;
			}
		}

		private Guid BookmarkRowToGuid(Row ABookmarkRow)
		{
			Scalar LScalar = ABookmarkRow[0];
			return LScalar.GuidConveyor.GetAsGuid(LScalar);
		}

		protected override bool InternalGotoBookmark(Row ABookmark)
		{
			return FSourceCursor.GotoBookmark(BookmarkRowToGuid(ABookmark));
		}
		
		protected override int InternalCompareBookmarks(Row ABookmark1, Row ABookmark2)
		{
			return FSourceCursor.CompareBookmarks(BookmarkRowToGuid(ABookmark1), BookmarkRowToGuid(ABookmark2));
		}
		
		protected override Row InternalGetKey()
		{
			return FSourceCursor.GetKey();
		}
		
		protected override bool InternalFindKey(Row AKey, bool AForward)
		{
			return FSourceCursor.FindKey(AKey, AForward);
		}
		
		protected override void InternalFindNearest(Row AKey)
		{
			FSourceCursor.FindNearest(AKey);
		}
		
		protected override bool InternalRefresh(Row ARow)
		{
			return FSourceCursor.Refresh(ARow);
		}
		
		protected override int InternalRowCount()
		{
			return FSourceCursor.RowCount();
		}
		
		protected override void InternalInsert(Row ARow)
		{
			FSourceCursor.Insert(ARow);
		}
		
		protected override void InternalUpdate(Row ARow)
		{
			FSourceCursor.Update(ARow);
		}
		
		protected override void InternalDelete()
		{
			FSourceCursor.Delete();
		}
    }
#endif

}

