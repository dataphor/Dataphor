/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public enum ScanDirection {Forward, Backward}
	    
	///	<remarks>
	/// Provides a "scan" on the given NativeTable using the given access path (NativeRowTree).
	/// A scan implements the Navigable, BackwardsNavigable, and Searchable CursorCapabilities in
	/// the same way as a Table value is expected to. A scan is an active window onto the given
	/// table value, changes made to the underlying data are immediately reflected in the scan.
	/// </remarks>
	public class Scan : Disposable
	{
		/// <remarks> Scan range keys are inclusive. </remarks>		
		public Scan(IValueManager AManager, NativeTable ATable, NativeRowTree AAccessPath, ScanDirection ADirection, Row AFirstKey, Row ALastKey)
		{
			FManager = AManager;
			FTable = ATable;
			FAccessPath = AAccessPath;
			FDirection = ADirection;
			FFirstKey = AFirstKey;
			FLastKey = ALastKey;
		}

		private IValueManager FManager;
		private NativeTable FTable;
		private NativeRowTree FAccessPath;
		private ScanDirection FDirection;
		private Row FFirstKey;
		private Row FLastKey;
		private bool FBOF;
		private bool FEOF;
		private RowTreeNode FIndexNode;
		private int FEntryNumber;

		private void SetIndexNode(RowTreeNode AIndexNode)
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
			get { return FActive; }
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
			FAccessPath.OnRowsMoved += new NativeRowTreeRowsMovedHandler(RowTreeRowsMoved);
			FAccessPath.OnRowDeleted += new NativeRowTreeRowDeletedHandler(RowTreeRowDeleted);
			First();
		}
		
		private void InternalClose()
		{
			FAccessPath.OnRowDeleted -= new NativeRowTreeRowDeletedHandler(RowTreeRowDeleted);
			FAccessPath.OnRowsMoved -= new NativeRowTreeRowsMovedHandler(RowTreeRowsMoved);
			SetIndexNode(null);
		}
		
		public void Reset()
		{
			#if SAFETABLES
			CheckActive();
			#endif
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
		
		private void RowTreeRowsMoved(NativeRowTree ARowTree, NativeRowTreeNode AOldNode, int AOldEntryNumberMin, int AOldEntryNumberMax, NativeRowTreeNode ANewNode, int AEntryNumberDelta)
		{
			if ((FIndexNode.Node == AOldNode) && (FEntryNumber >= AOldEntryNumberMin) && (FEntryNumber <= AOldEntryNumberMax))
			{
				if (AOldNode != ANewNode)
					SetIndexNode(new RowTreeNode(FManager, FIndexNode.Tree, ANewNode, LockMode.Shared));
					
				FEntryNumber += AEntryNumberDelta;
				UpdateScanPointer();
			}
		}
		
		private void RowTreeRowDeleted(NativeRowTree ARowTree, NativeRowTreeNode ANode, int AEntryNumber)
		{
			if ((FIndexNode.Node == ANode) && (FEntryNumber == AEntryNumber))
				UpdateScanPointer();
		}
		
		private bool FindIndexKey(Schema.IRowType AKeyRowType, NativeRow AKey, out RowTreeNode AIndexNode, out int AEntryNumber)
		{
			RowTreeSearchPath LSearchPath = new RowTreeSearchPath();
			try
			{
				bool LResult = FAccessPath.FindKey(FManager, AKeyRowType, AKey, LSearchPath, out AEntryNumber);
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
				if (FIndexNode.Node.NextNode == null)
					return false;
				else
				{
					SetIndexNode(new RowTreeNode(FManager, FIndexNode.Tree, FIndexNode.Node.NextNode, LockMode.Shared));
					if (FIndexNode.Node.EntryCount > 0)
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
				if (FIndexNode.Node.PriorNode == null)
					return false;
				else
				{
					SetIndexNode(new RowTreeNode(FManager, FIndexNode.Tree, FIndexNode.Node.PriorNode, LockMode.Shared));
					if (FIndexNode.Node.EntryCount > 0)
					{
						FEntryNumber = FIndexNode.Node.EntryCount - 1;
						return true;
					}
				}
			}
		}
		
		public void First()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			FBOF = true;
			FEOF = false;
			bool LResult = false;
			if (FFirstKey != null)
			{
				RowTreeNode LIndexNode;
				LResult = FindIndexKey(FFirstKey.DataType, (NativeRow)FFirstKey.AsNative, out LIndexNode, out FEntryNumber);
				SetIndexNode(LIndexNode);
			}
			else
			{
				if (FDirection == ScanDirection.Forward)
				{
					SetIndexNode(new RowTreeNode(FManager, FAccessPath, FAccessPath.Head, LockMode.Shared));
					FEntryNumber = 0;
				}
				else
				{
					SetIndexNode(new RowTreeNode(FManager, FAccessPath, FAccessPath.Tail, LockMode.Shared));
					FEntryNumber = FIndexNode.Node.EntryCount - 1;
				}
			}
			
			if (!LResult)
			{
				// Determine FEOF
				RowTreeNode LSaveIndexNode = new RowTreeNode(FManager, FIndexNode.Tree, FIndexNode.Node, LockMode.Shared);
				int LSaveEntryNumber = FEntryNumber;
				Next();
				FBOF = true;
				SetIndexNode(LSaveIndexNode);
				FEntryNumber = LSaveEntryNumber;
			}
		}		
		
		public bool Prior()
		{
			#if SAFETABLES
			CheckActive();
			#endif

			bool LEOF = FEOF;
			
			if (FDirection == ScanDirection.Forward)
			{
				if (FEOF)
					FEOF = false;
				else
					FEntryNumber--;
					
				if (((FEntryNumber < 0) || (FIndexNode.Node.EntryCount == 0)) && !PriorNode())
				{
					FEntryNumber = 0;
					FBOF = true;
				}

				// Make sure that the entry is >= FFirstKey
				if (!FBOF && (FFirstKey != null) && (FAccessPath.Compare(FManager, FIndexNode.Tree.KeyRowType, FIndexNode.Node.Keys[FEntryNumber], FFirstKey.DataType, (NativeRow)FFirstKey.AsNative) < 0))
					First();
			}
			else
			{
				if (FEOF)
					FEOF = false;
				else
					FEntryNumber++;

				if ((FEntryNumber >= FIndexNode.Node.EntryCount) && !NextNode())
				{
					FEntryNumber = FIndexNode.Node.EntryCount - 1;
					FEntryNumber = FEntryNumber < 0 ? 0 : FEntryNumber;
					FBOF = true;
				}
					
				// Make sure that the entry is <= FFirstKey
				if (!FBOF && (FFirstKey != null) && (FAccessPath.Compare(FManager, FIndexNode.Tree.KeyRowType, FIndexNode.Node.Keys[FEntryNumber], FFirstKey.DataType, (NativeRow)FFirstKey.AsNative) > 0))
					First();
			}
			
			// Make sure that if the scan is empty, the EOF flag is still true
			if (LEOF && FBOF)
				FEOF = true;
				
			return !FBOF;
		}
		
		public bool Next()
		{
			#if SAFETABLES
			CheckActive();
			#endif
			
			bool LBOF = FBOF;

			if (FDirection == ScanDirection.Forward)
			{
				if (FBOF)
					FBOF = false;
				else
					FEntryNumber++;

				if ((FEntryNumber >= FIndexNode.Node.EntryCount) && !NextNode())
				{
					FEntryNumber = FIndexNode.Node.EntryCount - 1;
					FEntryNumber = FEntryNumber < 0 ? 0 : FEntryNumber;
					FEOF = true;
				}
					
				// Make sure that the entry is <= LLastKey
				if (!FEOF && (FLastKey != null) && (FAccessPath.Compare(FManager, FIndexNode.Tree.KeyRowType, FIndexNode.Node.Keys[FEntryNumber], FLastKey.DataType, (NativeRow)FLastKey.AsNative) > 0))
					Last();
			}
			else
			{
				if (FBOF)
					FBOF = false;
				else
					FEntryNumber--;
					
				if (((FEntryNumber < 0) || (FIndexNode.Node.EntryCount == 0)) && !PriorNode())
				{
					FEntryNumber = 0;
					FEOF = true;
				}
				
				// Make sure that the entry is >= LLastKey
				if (!FEOF && (FLastKey != null) && (FAccessPath.Compare(FManager, FIndexNode.Tree.KeyRowType, FIndexNode.Node.Keys[FEntryNumber], FLastKey.DataType, (NativeRow)FLastKey.AsNative) < 0))
					Last();
			}
				
			// Make sure that if the scan is empty, the BOF flag is still true
			if (LBOF && FEOF)
				FBOF = true;
				
			return !FEOF;
		}
		
		public void Last()
		{
			#if SAFETABLES
			CheckActive();
			#endif
			
			FEOF = true;
			FBOF = false;
			bool LResult = false;
			if (FLastKey != null)
			{
				RowTreeNode LIndexNode;
				LResult = FindIndexKey(FLastKey.DataType, (NativeRow)FLastKey.AsNative, out LIndexNode, out FEntryNumber);
				SetIndexNode(LIndexNode);
			}
			else
			{
				if (FDirection == ScanDirection.Forward)
				{
					SetIndexNode(new RowTreeNode(FManager, FAccessPath, FAccessPath.Tail, LockMode.Shared));
					FEntryNumber = FIndexNode.Node.EntryCount - 1;
				}
				else
				{
					SetIndexNode(new RowTreeNode(FManager, FAccessPath, FAccessPath.Head, LockMode.Shared));
					FEntryNumber = 0;
				}
			}
			
			if (!LResult)
			{
				// Determine FBOF
				RowTreeNode LSaveIndexNode = new RowTreeNode(FManager, FIndexNode.Tree, FIndexNode.Node, LockMode.Shared);
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
			#if SAFETABLES
			CheckActive();
			#endif
			return FBOF;
		}
		
		public bool EOF()
		{
			#if SAFETABLES
			CheckActive();
			#endif
			return FEOF;
		}
		
		public Row GetRow()
		{
			Row LRow = new Row(FManager, FTable.RowType);
			GetRow(LRow);
			return LRow;
		}
		
		private bool IsSubset(Row ARow, NativeRowTree AIndex)
		{
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				if (!AIndex.KeyRowType.Columns.ContainsName(LColumn.Name) && !AIndex.DataRowType.Columns.Contains(LColumn.Name))
					return false;
			return true;
		}
		
		public void GetRow(Row ARow)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			CheckNotCrack();
			
			if ((FAccessPath.IsClustered) || IsSubset(ARow, FAccessPath))
			{
				Row LRow = new Row(FManager, FAccessPath.KeyRowType, FIndexNode.Node.Keys[FEntryNumber]);
				try
				{
					LRow.CopyTo(ARow);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LRow = new Row(FManager, FAccessPath.DataRowType, FIndexNode.DataNode.Rows[FEntryNumber]);
				try
				{ 
					LRow.CopyTo(ARow); 
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
			{
				using (RowTreeSearchPath LSearchPath = new RowTreeSearchPath())
				{
					int LEntryNumber;
					bool LResult = FTable.ClusteredIndex.FindKey(FManager, FTable.ClusteredIndex.KeyRowType, FIndexNode.DataNode.Rows[FEntryNumber], LSearchPath, out LEntryNumber);
					if (LResult)
					{
						Row LRow = new Row(FManager, FTable.ClusteredIndex.KeyRowType, LSearchPath.DataNode.Node.Keys[LEntryNumber]);
						try
						{
							LRow.CopyTo(ARow);
						}
						finally
						{
							LRow.Dispose();
						}

						LRow = new Row(FManager, FTable.ClusteredIndex.DataRowType, LSearchPath.DataNode.DataNode.Rows[LEntryNumber]);
						try
						{ 
							LRow.CopyTo(ARow); 
						}
						finally
						{
							LRow.Dispose();
						}
					}
					else
						throw new ScanException(ScanException.Codes.ClusteredRowNotFound);
				}
			}
		}
		
		public Row GetKey()
		{
			#if SAFETABLES
			CheckActive();
			#endif
			CheckNotCrack();
			return new Row(FManager, FAccessPath.KeyRowType, FIndexNode.Node.Keys[FEntryNumber]);
		}
		
		public Row GetData()
		{
			#if SAFETABLES
			CheckActive();
			#endif
			CheckNotCrack();
			return new Row(FManager, FAccessPath.DataRowType, FIndexNode.Node.Keys[FEntryNumber]);
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
				LKey = new Row(FManager, FAccessPath.KeyRowType);
				AKey.CopyTo(LKey);
			}

			return LKey;
		}
		
		public bool FindKey(Row AKey)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			int LEntryNumber;
			RowTreeNode LIndexNode;
			Row LKey = EnsureKeyRow(AKey);
			try
			{
				if (FFirstKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FManager, FFirstKey.DataType, (NativeRow)FFirstKey.AsNative, LKey.DataType, (NativeRow)LKey.AsNative);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult > 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
						return false;
				}
				
				if (FLastKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FManager, FLastKey.DataType, (NativeRow)FLastKey.AsNative, LKey.DataType, (NativeRow)LKey.AsNative);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult < 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
						return false;
				}

				bool LResult = FindIndexKey(LKey.DataType, (NativeRow)LKey.AsNative, out LIndexNode, out LEntryNumber);
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
					LKey.Dispose();
			}
		}
		
		public bool FindNearest(Row AKey)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			int LEntryNumber;
			RowTreeNode LIndexNode;
			Row LKey = EnsureKeyRow(AKey);
			try
			{
				if (FFirstKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FManager, FFirstKey.DataType, (NativeRow)FFirstKey.AsNative, LKey.DataType, (NativeRow)LKey.AsNative);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult > 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
					{
						First();
						Next();
						return false;
					}
				}
				
				if (FLastKey != null)
				{
					int LCompareResult = FAccessPath.Compare(FManager, FLastKey.DataType, (NativeRow)FLastKey.AsNative, LKey.DataType, (NativeRow)LKey.AsNative);
					if (((FDirection == ScanDirection.Forward) && (LCompareResult < 0)) || ((FDirection == ScanDirection.Backward) && (LCompareResult < 0)))
					{
						Last();
						Prior();
						return false;
					}
				}
					
				bool LResult = FindIndexKey(LKey.DataType, (NativeRow)LKey.AsNative, out LIndexNode, out LEntryNumber);
				SetIndexNode(LIndexNode);
				FEntryNumber = LEntryNumber;
				
				if (FEntryNumber >= FIndexNode.Node.EntryCount)
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
					LKey.Dispose();
			}
		}

		/// <remarks>The keys passed to this function must be of the same row type as the key for the accesspath for the scan</remarks>		
		public int CompareKeys(Row AKey1, Row AKey2)
		{
			#if SAFETABLES
			CheckActive();
			#endif
			return FAccessPath.Compare(FManager, AKey1.DataType, (NativeRow)AKey1.AsNative, AKey2.DataType, (NativeRow)AKey2.AsNative);
		}
	}
}
