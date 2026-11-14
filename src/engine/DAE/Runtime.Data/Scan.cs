/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.IO;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	public enum ScanDirection {Forward, Backward}
	    
	///	<remarks>
	/// Provides a "scan" on the given TableBuffer using the given access path (TableBufferIndex).
	/// A scan implements the Navigable, BackwardsNavigable, and Searchable CursorCapabilities in
	/// the same way as a Table value is expected to. A scan is an active window onto the given
	/// table buffer, changes made to the underlying data are immediately reflected in the scan.
	/// </remarks>
	public class Scan : Disposable, IActive
	{
		/// <remarks> Scan range keys are inclusive. </remarks>		
		public Scan(ServerProcess AProcess, TableBuffer ATable, TableBufferIndex AAccessPath, ScanDirection ADirection, Row AFirstKey, Row ALastKey)
		{
			FProcess = AProcess;
			FTable = ATable;
			FAccessPath = AAccessPath;
			FDirection = ADirection;
			FFirstKey = AFirstKey;
			FLastKey = ALastKey;
		}

		private ServerProcess FProcess;
		private TableBuffer FTable;
		private TableBufferIndex FAccessPath;
		private ScanDirection FDirection;
		private Row FFirstKey;
		private Row FLastKey;
		private bool FBOF;
		private bool FEOF;
		private IndexNode FIndexNode;
		private int FEntryNumber;

		private void SetIndexNode(IndexNode AIndexNode)
		{
			// This implements crabbing, because the new node is locked before the old node lock is released
			if (FIndexNode != null)
				FIndexNode.Dispose();
			FIndexNode = AIndexNode;
		}
		
		public void Open()
		{
			Active = true;
		}
		
		public void Close()
		{
			Active = false;
		}
		
		private bool FActive;
		public bool Active
		{
			get
			{
				return FActive;
			}
			set
			{
				if (value && !FActive)
				{
					FActive = true;
					InternalOpen();
				}
				else if (!value && FActive)
				{
					InternalClose();
					FActive = false;
				}
			}
		}
		
		private void CheckActive()
		{
			if (!Active)
				throw new ScanException(ScanException.Codes.ScanInactive);
		}
		
		private void CheckNotCrack()
		{
			if (FBOF || FEOF)
				throw new ScanException(ScanException.Codes.NoActiveRow);
		}
		
		private void InternalOpen()
		{
			FAccessPath.OnRowsMoved += new IndexRowsMovedHandler(IndexRowsMoved);
			FAccessPath.OnRowDeleted += new IndexRowDeletedHandler(IndexRowDeleted);
			First();
		}
		
		private void InternalClose()
		{
			FAccessPath.OnRowDeleted -= new IndexRowDeletedHandler(IndexRowDeleted);
			FAccessPath.OnRowsMoved -= new IndexRowsMovedHandler(IndexRowsMoved);
			SetIndexNode(null);
		}
		
		public void Reset()
		{
			CheckActive();
			SetIndexNode(null);
			First();
		}
		
		protected override void Dispose(bool ADisposing)
		{
			Close();
			base.Dispose(ADisposing);
		}
		
		private void UpdateScanPointer()
		{
			FEntryNumber += FDirection == ScanDirection.Forward ? -1 : 1;
			Next();
		}
		
		private void IndexRowsMoved(Index AIndex, StreamID AOldStreamID, int AOldEntryNumberMin, int AOldEntryNumberMax, StreamID ANewStreamID, int AEntryNumberDelta)
		{
			if ((FIndexNode.StreamID == AOldStreamID) && (FEntryNumber >= AOldEntryNumberMin) && (FEntryNumber <= AOldEntryNumberMax))
			{
				if (AOldStreamID != ANewStreamID)
					SetIndexNode(new IndexNode(FProcess, FIndexNode.Index, ANewStreamID));
					
				FEntryNumber += AEntryNumberDelta;
				UpdateScanPointer();
			}
		}
		
		private void IndexRowDeleted(Index AIndex, StreamID AStreamID, int AEntryNumber)
		{
			if ((FIndexNode.StreamID == AStreamID) && (FEntryNumber == AEntryNumber))
				UpdateScanPointer();
		}
		
		private bool FindIndexKey(Stream AKey, object ACompareContext, out IndexNode AIndexNode, out int AEntryNumber)
		{
			SearchPath LSearchPath = new SearchPath();
			try
			{
				bool LResult = FAccessPath.FindKey(FProcess, AKey, ACompareContext, LSearchPath, out AEntryNumber);
				AIndexNode = LSearchPath.DisownAt(LSearchPath.Count - 1);
				return LResult;
			}
			finally
			{
				LSearchPath.Dispose();
			}
		}

		private bool NextNode()
		{
			while (true)
			{
				if (FIndexNode.NextNode == StreamID.Null)
					return false;
				else
				{
					SetIndexNode(new IndexNode(FProcess, FIndexNode.Index, FIndexNode.NextNode));
					if (FIndexNode.EntryCount > 0)
					{
						FEntryNumber = 0;
						return true;
					}
				}
			}
		}
		
		private bool PriorNode()
		{
			while (true)
			{
				if (FIndexNode.PriorNode == StreamID.Null)
					return false;
				else
				{
					SetIndexNode(new IndexNode(FProcess, FIndexNode.Index, FIndexNode.PriorNode));
					if (FIndexNode.EntryCount > 0)
					{
						FEntryNumber = FIndexNode.EntryCount - 1;
						return true;
					}
				}
			}
		}
		
		public void First()
		{
			CheckActive();

			FBOF = true;
			FEOF = false;
			bool LResult = false;
			if (FFirstKey != null)
			{
				IndexNode LIndexNode;
				LResult = FindIndexKey(FFirstKey.Stream, FFirstKey.DataType, out LIndexNode, out FEntryNumber);
				SetIndexNode(LIndexNode);
			}
			else
			{
				if (FDirection == ScanDirection.Forward)
				{
					SetIndexNode(new IndexNode(FProcess, FAccessPath, FAccessPath.HeadID));
					FEntryNumber = 0;
				}
				else
				{
					SetIndexNode(new IndexNode(FProcess, FAccessPath, FAccessPath.TailID));
					FEntryNumber = FIndexNode.EntryCount - 1;
				}
			}
			
			if (!LResult)
			{
				// Determine FEOF
				IndexNode LSaveIndexNode = new IndexNode(FProcess, FIndexNode.Index, FIndexNode.StreamID);
				int LSaveEntryNumber = FEntryNumber;
				Next();
				FBOF = true;
				SetIndexNode(LSaveIndexNode);
				FEntryNumber = LSaveEntryNumber;
			}
		}		
		
		public void Prior()
		{
			CheckActive();

			bool LEOF = FEOF;
			
			if (FDirection == ScanDirection.Forward)
			{
				if (FEOF)
					FEOF = false;
				else
					FEntryNumber--;
					
				if (((FEntryNumber < 0) || (FIndexNode.EntryCount == 0)) && !PriorNode())
				{
					FEntryNumber = 0;
					FBOF = true;
				}

				// Make sure that the entry is >= FFirstKey
				if (!FBOF && (FFirstKey != null) && (FAccessPath.Compare(FProcess, FIndexNode.Key(FEntryNumber), null, FFirstKey.Stream, FFirstKey.DataType) < 0))
					First();
			}
			else
			{
				if (FEOF)
					FEOF = false;
				else
					FEntryNumber++;

				if ((FEntryNumber >= FIndexNode.EntryCount) && !NextNode())
				{
					FEntryNumber = FIndexNode.EntryCount - 1;
					FEntryNumber = FEntryNumber < 0 ? 0 : FEntryNumber;
					FBOF = true;
				}
					
				// Make sure that the entry is <= FFirstKey
				if (!FBOF && (FFirstKey != null) && (FAccessPath.Compare(FProcess, FIndexNode.Key(FEntryNumber), null, FFirstKey.Stream, FFirstKey.DataType) > 0))
					First();
			}
			
			// Make sure that if the scan is empty, the EOF flag is still true
			if (LEOF && FBOF)
				FEOF = true;
		}
		
		public void Next()
		{
			CheckActive();
			
			bool LBOF = FBOF;

			if (FDirection == ScanDirection.Forward)
			{
				if (FBOF)
					FBOF = false;
				else
					FEntryNumber++;

				if ((FEntryNumber >= FIndexNode.EntryCount) && !NextNode())
				{
					FEntryNumber = FIndexNode.EntryCount - 1;
					FEntryNumber = FEntryNumber < 0 ? 0 : FEntryNumber;
					FEOF = true;
				}
					
				// Make sure that the entry is <= LLastKey
				if (!FEOF && (FLastKey != null) && (FAccessPath.Compare(FProcess, FIndexNode.Key(FEntryNumber), null, FLastKey.Stream, FLastKey.DataType) > 0))
					Last();
			}
			else
			{
				if (FBOF)
					FBOF = false;
				else
					FEntryNumber--;
					
				if (((FEntryNumber < 0) || (FIndexNode.EntryCount == 0)) && !PriorNode())
				{
					FEntryNumber = 0;
					FEOF = true;
				}
				
				// Make sure that the entry is >= LLastKey
				if (!FEOF && (FLastKey != null) && (FAccessPath.Compare(FProcess, FIndexNode.Key(FEntryNumber), null, FLastKey.Stream, FLastKey.DataType) < 0))
					Last();
			}
				
			// Make sure that if the scan is empty, the BOF flag is still true
			if (LBOF && FEOF)
				FBOF = true;
		}
		
		public void Last()
		{
			CheckActive();
			
			FEOF = true;
			FBOF = false;
			bool LResult = false;
			if (FLastKey != null)
			{
				IndexNode LIndexNode;
				LResult = FindIndexKey(FLastKey.Stream, FLastKey.DataType, out LIndexNode, out FEntryNumber);
				SetIndexNode(LIndexNode);
			}
			else
			{
				if (FDirection == ScanDirection.Forward)
				{
					SetIndexNode(new IndexNode(FProcess, FAccessPath, FAccessPath.TailID));
					FEntryNumber = FIndexNode.EntryCount - 1;
				}
				else
				{
					SetIndexNode(new IndexNode(FProcess, FAccessPath, FAccessPath.HeadID));
					FEntryNumber = 0;
				}
			}
			
			if (!LResult)
			{
				// Determine FBOF
				IndexNode LSaveIndexNode = new IndexNode(FProcess, FIndexNode.Index, FIndexNode.StreamID);
				int LSaveEntryNumber = FEntryNumber;
				Prior();
				FEOF = true;
				if (FIndexNode != null)
					FIndexNode.Dispose();
				FIndexNode = LSaveIndexNode;
				FEntryNumber = LSaveEntryNumber;
			}
		}
		
		public bool BOF()
		{
			CheckActive();
			return FBOF;
		}
		
		public bool EOF()
		{
			CheckActive();
			return FEOF;
		}
		
		public Row GetRow()
		{
			Row LRow = FProcess.RowManager.RequestRow(FProcess, FTable.TableVar.DataType.CreateRowType());
			GetRow(LRow);
			return LRow;
		}
		
		private bool IsSubset(Row ARow, TableBufferIndex AIndex)
		{
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				if (!AIndex.KeyRowType.Columns.Contains(LColumn.Name) && !AIndex.DataRowType.Columns.Contains(LColumn.Name))
					return false;
			return true;
		}
		
		public void GetRow(Row ARow)
		{
			CheckActive();
			CheckNotCrack();
			
			if ((FAccessPath.IsClustered) || IsSubset(ARow, FAccessPath))
			{
				Row LRow = FProcess.RowManager.RequestRow(FProcess, FAccessPath.KeyRowType, FIndexNode.Key(FEntryNumber));
				try
				{
					LRow.CopyTo(ARow);
				}
				finally
				{
					FProcess.RowManager.ReleaseRow(LRow);
				}
				
				LRow = FProcess.RowManager.RequestRow(FProcess, FAccessPath.DataRowType, FIndexNode.Data(FEntryNumber));
				try
				{ 
					LRow.CopyTo(ARow); 
				}
				finally
				{
					FProcess.RowManager.ReleaseRow(LRow);
				}
			}
			else
			{
				using (SearchPath LSearchPath = new SearchPath())
				{
					int LEntryNumber;
					bool LResult = FTable.ClusteredIndex.FindKey(FProcess, FIndexNode.Data(FEntryNumber), null, LSearchPath, out LEntryNumber);
					if (LResult)
					{
						Row LRow = FProcess.RowManager.RequestRow(FProcess, FTable.ClusteredIndex.KeyRowType, LSearchPath.DataNode.Key(LEntryNumber));
						try
						{
							LRow.CopyTo(ARow);
						}
						finally
						{
							FProcess.RowManager.ReleaseRow(LRow);
						}

						LRow = FProcess.RowManager.RequestRow(FProcess, FTable.ClusteredIndex.DataRowType, LSearchPath.DataNode.Data(LEntryNumber));
						try
						{ 
							LRow.CopyTo(ARow); 
						}
						finally
						{
							FProcess.RowManager.ReleaseRow(LRow);
						}
					}
					else
						throw new ScanException(ScanException.Codes.ClusteredRowNotFound);
				}
			}
		}
		
		public Row GetKey()
		{
			CheckActive();
			CheckNotCrack();
			return FProcess.RowManager.RequestRow(FProcess, FAccessPath.KeyRowType, FIndexNode.Key(FEntryNumber));
		}
		
		public Row GetData()
		{
			CheckActive();
			CheckNotCrack();
			return FProcess.RowManager.RequestRow(FProcess, FAccessPath.DataRowType, FIndexNode.Key(FEntryNumber));
		}
		
		private Row EnsureKeyRow(Row AKey)
		{
			Row LKey = AKey;
			bool LIsKeyRow = AKey.DataType.Columns.Count <= FAccessPath.KeyRowType.Columns.Count;
			for (int LIndex = 0; LIndex < FAccessPath.KeyRowType.Columns.Count; LIndex++)
			{
				if (LIndex >= AKey.DataType.Columns.Count)
					break;
				else
				{
					if (!Schema.Object.NamesEqual(AKey.DataType.Columns[LIndex].Name, FAccessPath.KeyRowType.Columns[LIndex].Name))
					{
						LIsKeyRow = false;
						break;
					}
				}
			}
			
			if (!LIsKeyRow)
			{
				LKey = FProcess.RowManager.RequestRow(FProcess, FAccessPath.KeyRowType);
				AKey.CopyTo(LKey);
			}

			return LKey;
		}
		
		public bool FindKey(Row AKey)
		{
			CheckActive();
			int LEntryNumber;
			IndexNode LIndexNode;
			Row LKey = EnsureKeyRow(AKey);
			try
			{
				if (FFirstKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FProcess, FFirstKey.Stream, FFirstKey.DataType, LKey.Stream, LKey.DataType);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult > 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
						return false;
				}
				
				if (FLastKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FProcess, FLastKey.Stream, FLastKey.DataType, LKey.Stream, LKey.DataType);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult < 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
						return false;
				}

				bool LResult = FindIndexKey(LKey.Stream, LKey.DataType, out LIndexNode, out LEntryNumber);
				if (LResult)
				{
					SetIndexNode(LIndexNode);
					FEntryNumber = LEntryNumber;
					FBOF = false;
					FEOF = false;
				}
				else
					LIndexNode.Dispose();
				return LResult;
			}
			finally
			{
				if (!ReferenceEquals(AKey, LKey))
					FProcess.RowManager.ReleaseRow(LKey);
			}
		}
		
		public bool FindNearest(Row AKey)
		{
			CheckActive();
			int LEntryNumber;
			IndexNode LIndexNode;
			Row LKey = EnsureKeyRow(AKey);
			try
			{
				if (FFirstKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FProcess, FFirstKey.Stream, FFirstKey.DataType, LKey.Stream, LKey.DataType);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult > 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
					{
						First();
						Next();
						return false;
					}
				}
				
				if (FLastKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FProcess, FLastKey.Stream, FLastKey.DataType, LKey.Stream, LKey.DataType);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult < 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
					{
						Last();
						Prior();
						return false;
					}
				}
					
				bool LResult = FindIndexKey(LKey.Stream, LKey.DataType, out LIndexNode, out LEntryNumber);
				SetIndexNode(LIndexNode);
				FEntryNumber = LEntryNumber;
				
				if (FEntryNumber >= FIndexNode.EntryCount)
				{
					Next();
					if (FEOF)
					{
						Last();
						Prior();
					}
				}
				else
				{
					FBOF = false;
					FEOF = false;
				}

				return LResult;
			}
			finally
			{
				if (!ReferenceEquals(AKey, LKey))
					FProcess.RowManager.ReleaseRow(LKey);
			}
		}

		/// <remarks>The keys passed to this function must be of the same row type as the key for the accesspath for the scan</remarks>		
		public int CompareKeys(Row AKey1, Row AKey2)
		{
			CheckActive();
			return FAccessPath.Compare(FProcess, AKey1.Stream, null, AKey2.Stream, null);
		}
	}
}
