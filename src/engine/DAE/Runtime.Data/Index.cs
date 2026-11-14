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

	/// <remarks>
	/// Provides a callback to notify users of the index when a set of rows has moved.
	/// </remarks>
	public delegate void IndexRowsMovedHandler(Index AIndex, StreamID AOldStreamID, int AOldEntryNumberMin, int AOldEntryNumberMax, StreamID ANewStreamID, int AEntryNumberDelta);

	/// <remarks>
	/// Provides a callback to notify users of the index when a row is deleted.
	/// </remarks>
	public delegate void IndexRowDeletedHandler(Index AIndex, StreamID AStreamID, int AEntryNumber);
	
	/// <remarks>
	/// Provides a storage structure for the search path followed by the find key in terms of index nodes.
	/// See the description of the FindKey method for the Index class for more information.
	/// </remarks>
	public class SearchPath : DisposableTypedList
	{
		public SearchPath() : base(typeof(IndexNode), true, false){}
		
		public new IndexNode this[int AIndex]
		{
			get { return (IndexNode)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public IndexNode DataNode { get { return this[Count - 1]; } }
		
		public new IndexNode RemoveAt(int AIndex)
		{
			return (IndexNode)base.RemoveItemAt(AIndex);
		}
		
		public new IndexNode DisownAt(int AIndex)
		{
			return (IndexNode)base.DisownAt(AIndex);
		}
	}
	
	public class IndexUtility : System.Object
	{
		public static unsafe StreamID GetStreamID(Stream AStream, long AOffset)
		{
			AStream.Position = AOffset;
			byte[] LBuffer = new byte[sizeof(StreamID)];
			AStream.Read(LBuffer, 0, LBuffer.Length);
			fixed (byte* LBufferPtr = &(LBuffer[0]))
			{
				return *((StreamID*)LBufferPtr);
			}
		}
		
		public static unsafe void SetStreamID(Stream AStream, long AOffset, StreamID AStreamID)
		{
			byte[] LBuffer = new byte[sizeof(StreamID)];
			fixed (byte* LBufferPtr = &(LBuffer[0]))
			{
				*((StreamID*)LBufferPtr) = AStreamID;
			}
			
			AStream.Position = AOffset;
			AStream.Write(LBuffer, 0, LBuffer.Length);
		}
		
		public static unsafe int GetInt32(Stream AStream, long AOffset)
		{
			AStream.Position = AOffset;
			byte[] LBuffer = new byte[sizeof(int)];
			AStream.Read(LBuffer, 0, LBuffer.Length);
			fixed (byte* LBufferPtr = &(LBuffer[0]))
			{
				return *((int*)LBufferPtr);
			}
		}
		
		public static unsafe void SetInt32(Stream AStream, long AOffset, int AValue)
		{
			byte[] LBuffer = new byte[sizeof(int)];
			fixed (byte* LBufferPtr = &(LBuffer[0]))
			{
				*((int*)LBufferPtr) = AValue;
			}
			AStream.Position = AOffset;
			AStream.Write(LBuffer, 0, LBuffer.Length);
		}
	}
	
	/// <remarks>
	///	Provides a generic implementation of a B+Tree structure.
	/// The main characteristics of this structure are Fanout, Capacity, KeyLength and DataLength.
	/// The size of the nodes of the index is determined by these characteristics.  A callback is
	/// used for index comparison, so the index itself has no knowledge of what is actually stored.
	/// Makes use of the StreamManager class to provide read/write access to streams for the storage
	/// of the actual data.  The Index class is the entry point to the structure, but stores no data,
	/// it simply points to the nodes that do.  
	/// </remarks>	
	public abstract class Index : Disposable
	{
		public const int MinimumFanout = 2;
		
		public Index
		(
			int AFanout, 
			int ACapacity, 
			int AKeyLength, 
			int ADataLength
		) : base()
		{
			FFanout = AFanout;
			FCapacity = ACapacity;
			FKeyLength = AKeyLength;
			FDataLength = ADataLength;
		}
		
		/// <summary>The StreamID of the root node of the tree.</summary>
		private StreamID FRootID;
		public StreamID RootID { get { return FRootID; } }

		/// <summary>The StreamID of the first leaf node in the tree.</summary>		
		private StreamID FHeadID;
		public StreamID HeadID { get { return FHeadID; } }

		/// <summary>The StreamID of the last leaf node in the tree.</summary>
		private StreamID FTailID;
		public StreamID TailID { get { return FTailID; } }

		/// <summary>The height of the tree.</summary>
		private int FHeight;
		public int Height { get { return FHeight; } }

		/// <summary>Create is only called once to create a new stored instance of the index.</summary>
		public void Create(ServerProcess AProcess)
		{
			using (IndexNode LIndexNode = AllocateNode(AProcess, IndexNodeType.Data))
			{
				FRootID = LIndexNode.StreamID;
				FHeadID = FRootID;
				FTailID = FRootID;
				FHeight = 1;
			}
		}

		/// <summary>Only called to bind to an existing stored instance of the index.</summary>
		public void Bind(StreamID ARootID, StreamID AHeadID, StreamID ATailID, int AHeight)
		{
			FRootID = ARootID;
			FHeadID = AHeadID;
			FTailID = ATailID;
			FHeight = AHeight;
		}
		
		/// <summary>Drops the index and deallocates the streams associated with it.</summary>
		public void Drop(ServerProcess AProcess)
		{
			DeallocateNode(AProcess, FRootID);
			FRootID = StreamID.Null;
			FHeight = 0;
		}
		
		private void DeallocateNode(ServerProcess AProcess, StreamID AStreamID)
		{
			using (IndexNode LIndexNode = new IndexNode(AProcess, this, AStreamID))
			{
				for (int LEntryIndex = LIndexNode.EntryCount - 1; LEntryIndex >= 0; LEntryIndex--)
				{
					if (LIndexNode.NodeType == IndexNodeType.Routing)
					{
						if (LEntryIndex > 0)
							DisposeKey(AProcess, LIndexNode.Key(LEntryIndex));
						DeallocateNode(AProcess, IndexUtility.GetStreamID(LIndexNode.Data(LEntryIndex), 0));
					}
					else
					{
						DisposeKey(AProcess, LIndexNode.Key(LEntryIndex));
						DisposeData(AProcess, LIndexNode.Data(LEntryIndex));

						if (LIndexNode.NextNode == StreamID.Null)
							FTailID = LIndexNode.PriorNode;
						else
						{
							using (IndexNode LNextNode = new IndexNode(AProcess, this, LIndexNode.NextNode))
							{
								LNextNode.PriorNode = LIndexNode.PriorNode;
							}
						}
						
						if (LIndexNode.PriorNode == StreamID.Null)
							FHeadID = LIndexNode.NextNode;
						else
						{
							using (IndexNode LPriorNode = new IndexNode(AProcess, this, LIndexNode.PriorNode))
							{
								LPriorNode.NextNode = LIndexNode.NextNode;
							}
						}
					}
				}
			}

			AProcess.StreamManager.Deallocate(AStreamID);
		}

		/// <summary> 
		/// Will return an IndexNode wrapper on a newly allocated IndexNode stream opened in exclusive mode
		/// it is the callers responsibility Dispose the returned IndexNode, closing and unlocking the stream
		/// </summary>
		private unsafe IndexNode AllocateNode(ServerProcess AProcess, IndexNodeType ANodeType)
		{
			// Allocate a new index node
			StreamID LStreamID = AProcess.StreamManager.Allocate();
			Stream LStream = AProcess.StreamManager.Open(LStreamID, LockMode.Shared);

			// Size it (HeaderSize + ((KeyLength + DataLength | sizeof(StreamID)) * Capacity | Fanout)
			if (ANodeType == IndexNodeType.Routing)
				LStream.SetLength(checked(IndexNode.HeaderSize + ((KeyLength + sizeof(StreamID)) * Fanout)));
			else
				LStream.SetLength(checked(IndexNode.HeaderSize + ((KeyLength + DataLength) * Capacity)));
			return new IndexNode(AProcess, this, LStreamID, LStream, ANodeType);
		}
		
		/// <summary>The number of entries per routing node in the tree.</summary>		
		private int FFanout;		
		public int Fanout { get { return FFanout; } }

		/// <summary>The number of entries per data node in the tree.</summary>		
		private int FCapacity;
		public int Capacity { get { return FCapacity; } }

		/// <summary>The length in bytes of a key entry in the tree.</summary>		
		private int FKeyLength;
		public int KeyLength { get { return FKeyLength; } }

		/// <summary>The length in bytes of a data entry in the tree.</summary>		
		private int FDataLength;
		public int DataLength { get { return FDataLength; } }
		
		/// <summary>
		/// The given streams are copied into the index, so references within the streams 
		/// are considered owned by the index after the insert.
		/// </summary>
		public void Insert(ServerProcess AProcess, Stream AKey, Stream AData)
		{
			int LEntryNumber;
			using (SearchPath LSearchPath = new SearchPath())
			{
				bool LResult = FindKey(AProcess, AKey, null, LSearchPath, out LEntryNumber);
				if (LResult)
					throw new IndexException(IndexException.Codes.DuplicateKey);
					
				InternalInsert(AProcess, LSearchPath, LEntryNumber, AKey, AData);
			}
		}
		
		private unsafe void InternalInsert(ServerProcess AProcess, SearchPath ASearchPath, int AEntryNumber, Stream AKey, Stream AData)
		{
			// Walk back up the search path, inserting data and splitting pages as necessary
			IndexNode LNewIndexNode;
			for (int LIndex = ASearchPath.Count - 1; LIndex >= 0; LIndex--)
			{
				if (ASearchPath[LIndex].EntryCount >= ASearchPath[LIndex].MaxEntries)
				{
					// Allocate a new node
					using (LNewIndexNode = AllocateNode(AProcess, ASearchPath[LIndex].NodeType))
					{
						// Thread it into the list of leaves, if necessary
						if (LNewIndexNode.NodeType == IndexNodeType.Data)
						{
							LNewIndexNode.PriorNode = ASearchPath[LIndex].StreamID;
							LNewIndexNode.NextNode = ASearchPath[LIndex].NextNode;
							ASearchPath[LIndex].NextNode = LNewIndexNode.StreamID;
							if (LNewIndexNode.NextNode == StreamID.Null)
								FTailID = LNewIndexNode.StreamID;
							else
							{
								using (IndexNode LNextIndexNode = new IndexNode(AProcess, this, LNewIndexNode.NextNode))
								{
									LNextIndexNode.PriorNode = LNewIndexNode.StreamID;
								}
							}
						}
						
						// Insert the upper half of the entries from ASearchPath[LIndex] into the new index node
						int LEntryCount = ASearchPath[LIndex].EntryCount;
						int LEntryPivot = LEntryCount / 2;
						for (int LEntryIndex = LEntryPivot; LEntryIndex < LEntryCount; LEntryIndex++)
							LNewIndexNode.InternalInsert(ASearchPath[LIndex].Key(LEntryIndex), ASearchPath[LIndex].Data(LEntryIndex), LEntryIndex - LEntryPivot); // The internal call prevents the RowsMoved event from being fired
						
						// Remove the upper half of the entries from ASearchPath[LIndex]
						for (int LEntryIndex = LEntryCount - 1; LEntryIndex >= LEntryPivot; LEntryIndex--)
							ASearchPath[LIndex].InternalDelete(LEntryIndex); // The internal call prevents the data inside from being passed to the DisposeXXX methods, and prevents the RowDeleted event from being fired
							
						// Notify index clients of the data change
						RowsMoved(ASearchPath[LIndex].StreamID, LEntryPivot, LEntryCount - 1, LNewIndexNode.StreamID, -LEntryPivot);
						
						// Insert the new entry into the appropriate node
						if (AEntryNumber >= LEntryPivot)
							LNewIndexNode.Insert(AKey, AData, AEntryNumber - LEntryPivot);
						else
							ASearchPath[LIndex].Insert(AKey, AData, AEntryNumber);
							
						// Reset the AKey, AData and AEntryNumber variables for the next round
						// The key for the entry one level up is the first key for the newly allocated node
						AKey = new MemoryStream(KeyLength);
						AKey.SetLength(KeyLength);
						CopyKey(AProcess, LNewIndexNode.Key(0), AKey);
						
						// The data is the StreamID of the newly allocated node
						AData = new MemoryStream(sizeof(StreamID));
						IndexUtility.SetStreamID(AData, 0, LNewIndexNode.StreamID);
					}

					if (LIndex == 0)
					{
						// Allocate a new root node and grow the height of the tree by 1
						using (LNewIndexNode = AllocateNode(AProcess, IndexNodeType.Routing))
						{
							LNewIndexNode.Insert(AKey, AData, 0);
							AKey = new MemoryStream(KeyLength);
							AKey.SetLength(KeyLength);
							AData = new MemoryStream(DataLength);
							AData.SetLength(KeyLength);
							IndexUtility.SetStreamID(AData, 0, ASearchPath[LIndex].StreamID);
							LNewIndexNode.Insert(AKey, AData, 0);
							FRootID = LNewIndexNode.StreamID;
							FHeight++;
						}
					}
					else
					{
						bool LResult = ASearchPath[LIndex - 1].NodeSearch(AKey, null, out AEntryNumber);
						// At this point we should be guaranteed to have a routing key which does not exist in the parent node
						if (LResult)
							throw new IndexException(IndexException.Codes.DuplicateRoutingKey);
					}
				}
				else
				{
					ASearchPath[LIndex].Insert(AKey, AData, AEntryNumber);
					break;
				}
			}
		}
		
		/// <summary>Updates the entry given by AOldKey to the stream given by ANewKey.  The data for the entry is moved to the new location.</summary>
		public void Update(ServerProcess AProcess, Stream AOldKey, Stream ANewKey)
		{
			Update(AProcess, AOldKey, ANewKey, null);
		}
		
		/// <summary>Updates the entry given by AOldKey to the entry given by ANewKey and ANewData.  If AOldKey == ANewKey, the data for the entry is updated in place, otherwise it is moved to the location given by ANewKey.</summary>
		public void Update(ServerProcess AProcess, Stream AOldKey, Stream ANewKey, Stream ANewData)
		{
			int LEntryNumber;
			using (SearchPath LSearchPath = new SearchPath())
			{
				bool LResult = FindKey(AProcess, AOldKey, null, LSearchPath, out LEntryNumber);
				if (!LResult)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				if (Compare(AProcess, AOldKey, null, ANewKey, null) == 0)
				{
					if (ANewData != null)
						LSearchPath.DataNode.Update(ANewData, LEntryNumber);
				}
				else
				{
					if (ANewData == null)
					{
						ANewData = new MemoryStream(DataLength);
						ANewData.SetLength(DataLength);
						CopyData(AProcess, LSearchPath.DataNode.Data(LEntryNumber), ANewData);
					}
					InternalDelete(AProcess, LSearchPath, LEntryNumber);
					LSearchPath.Dispose();
					LResult = FindKey(AProcess, ANewKey, null, LSearchPath, out LEntryNumber);
					if (LResult)
						throw new IndexException(IndexException.Codes.DuplicateKey);
						
					InternalInsert(AProcess, LSearchPath, LEntryNumber, ANewKey, ANewData);
				}
			}
		}
		
		private void InternalDelete(ServerProcess AProcess, SearchPath ASearchPath, int AEntryNumber)
		{
			ASearchPath.DataNode.Delete(AEntryNumber);
		}
		
		// TODO: Asynchronous collapsed node recovery
		/// <summary>Deletes the entry given by AKey.  The streams are disposed through the DisposeKey event, so it is the responsibility of the index user to dispose references within the streams.</summary>
		public void Delete(ServerProcess AProcess, Stream AKey)
		{
			int LEntryNumber;
			using (SearchPath LSearchPath = new SearchPath())
			{
				bool LResult = FindKey(AProcess, AKey, null, LSearchPath, out LEntryNumber);
				if (!LResult)
					throw new IndexException(IndexException.Codes.KeyNotFound);
					
				InternalDelete(AProcess, LSearchPath, LEntryNumber);
			}
		}

		/// <summary>
		/// Searches for the given key within the index.  ASearchPath and AEntryNumber together give the 
		/// location of the key in the index.  If the search is successful, the entry exists, otherwise 
		/// the EntryNumber indicates where the entry should be placed for an insert.
		/// </summary>
		/// <param name="AKey">The key to be found.</param>
		/// <param name="ACompareContext">Context information which will passed to the compare handler.  May be null.</param>
		/// <param name="ASearchPath">A <see cref="SearchPath"/> which will contain the set of nodes along the search path to the key.</param>
		/// <param name="AEntryNumber">The EntryNumber where the key either is, or should be, depending on the result of the find.</param>
		/// <returns>A boolean value indicating the success or failure of the find.</returns>
		public bool FindKey(ServerProcess AProcess, Stream AKey, object ACompareContext, SearchPath ASearchPath, out int AEntryNumber)
		{
			return new IndexNode(AProcess, this, FRootID).FindKey(AKey, ACompareContext, ASearchPath, out AEntryNumber);
		}
		
		public abstract int Compare(ServerProcess AProcess, Stream AIndexKey, object AIndexContext, Stream ACompareKey, object ACompareContext);
		public abstract void CopyKey(ServerProcess AProcess, Stream ASourceKey, Stream ATargetKey);
		public abstract void CopyData(ServerProcess AProcess, Stream ASourceData, Stream ATargetData);
		public abstract void DisposeKey(ServerProcess AProcess, Stream AKey);
		public abstract void DisposeData(ServerProcess AProcess, Stream AData);

		public event IndexRowsMovedHandler OnRowsMoved;
		public void RowsMoved(StreamID AOldStreamID, int AOldEntryNumberMin, int AOldEntryNumberMax, StreamID ANewStreamID, int AEntryNumberDelta)
		{
			if (OnRowsMoved != null)
				OnRowsMoved(this, AOldStreamID, AOldEntryNumberMin, AOldEntryNumberMax, ANewStreamID, AEntryNumberDelta);
		}
		
		public event IndexRowDeletedHandler OnRowDeleted;
		public void RowDeleted(StreamID AStreamID, int AEntryNumber)
		{
			if (OnRowDeleted != null)
				OnRowDeleted(this, AStreamID, AEntryNumber);
		}
	}

	public enum IndexNodeType {Routing, Data}
	
	/// <remarks>
	/// <para>
	/// IndexNode wraps a stream containing data for a node of the tree.
	/// Creation of an IndexNode requests a shared lock on the stream.
	/// Disposing the node closes the stream, releasing the lock.
	/// </para>
	/// <para>
	///	IndexNode Header ->
	///		byte NodeType // 0x00 = Routing, 0x01 = Data
	///		StreamID PriorNode
	///		StreamID NextNode
	///		int EntryCount
	/// </para>
	/// </remarks>
	public unsafe class IndexNode : Disposable
	{
		public static readonly int NodeTypeOffset = 0;
		public static readonly int PriorNodeOffset = sizeof(byte);
		public static readonly int NextNodeOffset = sizeof(byte) + sizeof(StreamID);
		public static readonly int EntryCountOffset = sizeof(byte) + sizeof(StreamID) + sizeof(StreamID);
		public static readonly int HeaderSize = sizeof(byte) + sizeof(StreamID) + sizeof(StreamID) + sizeof(int);
		public static readonly int DataOffset = HeaderSize;
		
		// Existing stream IndexNode constructor
		public IndexNode(ServerProcess AProcess, Index AIndex, StreamID AStreamID)
		{					   
			FProcess = AProcess;
			FIndex = AIndex;
			FStreamID = AStreamID;
			FStream = FProcess.StreamManager.Open(AStreamID, LockMode.Shared);
			InternalInitialize();
		}
		
		// Newly allocated stream IndexNode constructor
		public IndexNode(ServerProcess AProcess, Index AIndex, StreamID AStreamID, Stream AStream, IndexNodeType ANodeType)
		{
			FProcess = AProcess;
			FIndex = AIndex;
			FStreamID = AStreamID;
			FStream = AStream;
			NodeType = ANodeType;
			InternalInitialize();
		}
		
		private ServerProcess FProcess;
		
		private void InternalInitialize()
		{
			if (NodeType == IndexNodeType.Data)
			{
				FDataLength = FIndex.DataLength;
				FMaxEntries = FIndex.Capacity;
			}
			else
			{
				FDataLength = sizeof(StreamID);
				FMaxEntries = FIndex.Fanout;
			}
			FKeyLength = FIndex.KeyLength;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FProcess != null)
			{
				if (FProcess.StreamManager != null)
                	FProcess.StreamManager.Close(FStreamID);
				FProcess = null;
			}
			
			if (FStream != null)
			{
				FStream.Close();
				FStream = null;
			}

			base.Dispose(ADisposing);
		}
		
		// The given stream is expected to be of the appropriate size
		private Stream FStream;
		private int FKeyLength;
		private int FDataLength;

		private int FMaxEntries;
		public int MaxEntries { get { return FMaxEntries; } }
		
		private Index FIndex;
		public Index Index { get { return FIndex; } }
		
		private StreamID FStreamID;
		public StreamID StreamID { get { return FStreamID; } }

		public IndexNodeType NodeType
		{
			get
			{
				FStream.Position = NodeTypeOffset;
				byte LByte = (byte)FStream.ReadByte();
				if ((LByte & 1) == 0)
					return IndexNodeType.Routing;
				else
					return IndexNodeType.Data;
				
			}
			set
			{
				FStream.Position = NodeTypeOffset;
				byte LByte = (byte)FStream.ReadByte();
				FStream.Position = NodeTypeOffset;
				if (value == IndexNodeType.Routing)
					FStream.WriteByte((byte)(LByte & ~1));
				else
					FStream.WriteByte((byte)(LByte | 1));
			}
		}

		/// <summary>The prior node in the leaf node list, will be StreamID.Null if this is the head of the list.</summary>		
		public StreamID PriorNode
		{
			get { return IndexUtility.GetStreamID(FStream, PriorNodeOffset); }
			set { IndexUtility.SetStreamID(FStream, PriorNodeOffset, value); }
		}

		/// <summary>The next node in the leaf node list, will be StreamID.Null if this is the tail of the list.</summary>		
		public StreamID NextNode
		{
			get { return IndexUtility.GetStreamID(FStream, NextNodeOffset); }
			set { IndexUtility.SetStreamID(FStream, NextNodeOffset, value); }
		}

		/// <summary>The number of active entries in this node.</summary>		
		public int EntryCount
		{
			get { return IndexUtility.GetInt32(FStream, EntryCountOffset); }
			set { IndexUtility.SetInt32(FStream, EntryCountOffset, value); }
		}

		/// <summary>The key for the given entry.</summary>
		public Stream Key(int AIndex)
		{
			return new CoverStream(FStream, DataOffset + (AIndex * (FKeyLength + FDataLength)), FKeyLength);
		}

		/// <summary>The data for the given entry.</summary>
		public Stream Data(int AIndex)
		{
			return new CoverStream(FStream, DataOffset + (AIndex * (FKeyLength + FDataLength)) + FKeyLength, FDataLength);
		}
		
		/// <summary>The entire entry.</summary>
		public Stream Entry(int AIndex)
		{
			return new CoverStream(FStream, DataOffset + (AIndex * (FKeyLength + FDataLength)), FKeyLength + FDataLength);
		}

		/// <summary>
		/// Performs a binary search among the entries in this node for the given key.  Will always return an
		/// entry index in AEntryNumber, which is the index of the entry that was found if the method returns true,
		/// otherwise it is the index where the key should be inserted if the method returns false.
		/// </summary>
		public bool NodeSearch(Stream AKey, object ACompareContext, out int AEntryNumber)
		{
			int LLo = (NodeType == IndexNodeType.Routing ? 1 : 0);
			int LHi = EntryCount - 1;
			int LIndex = 0;
			int LResult = -1;
			
			while (LLo <= LHi)
			{
				LIndex = (LLo + LHi) / 2;
				LResult = FIndex.Compare(FProcess, Key(LIndex), null, AKey, ACompareContext);
				if (LResult == 0)
					break;
				else if (LResult > 0)
					LHi = LIndex - 1;
				else // if (LResult < 0) unnecessary
					LLo = LIndex + 1;
			}
			
			if (LResult == 0)
				AEntryNumber = LIndex;
			else
				AEntryNumber = LLo;
				
			return LResult == 0;
		}

		/// <summary>
		/// The recursive portion of the find key algorithm invoked by the FindKey method of the parent Index.
		/// </summary>
		public bool FindKey(Stream AKey, object ACompareContext, SearchPath ASearchPath, out int AEntryNumber)
		{
			ASearchPath.Add(this);
			if (NodeType == IndexNodeType.Routing)
			{
				// Perform a binary search among the keys in this node to determine which streamid to follow for the next node
				bool LResult = NodeSearch(AKey, ACompareContext, out AEntryNumber);

				// If the key was found, use the given entry number, otherwise, use the one before the given entry
				AEntryNumber = LResult ? AEntryNumber : (AEntryNumber - 1);
				return 
					new IndexNode
					(
						FProcess,
						FIndex, 
						IndexUtility.GetStreamID(Data(AEntryNumber), 0)
					).FindKey
					(
						AKey, 
						ACompareContext,
						ASearchPath, 
						out AEntryNumber
					);
			}
			else
			{
				// Perform a binary search among the keys in this node to determine which entry, if any, is equal to the given key
				return NodeSearch(AKey, ACompareContext, out AEntryNumber);
			}
		}
		
		internal void InternalInsert(Stream AKey, Stream AData, int AEntryNumber)
		{
			// foreach Entry >= AEntryNumber, desc, copy the existing entry to the next entry
			int LEntryCount = EntryCount;
			for (int LIndex = LEntryCount - 1; LIndex >= AEntryNumber; LIndex--)
				StreamUtility.CopyStream(Entry(LIndex), Entry(LIndex + 1));
			
			// Write the given Key and Data streams into the node
			AKey.Position = 0;
			StreamUtility.CopyStream(AKey, Key(AEntryNumber));
			AData.Position = 0;
			StreamUtility.CopyStream(AData, Data(AEntryNumber));
			
			// Increment EntryCount
			EntryCount = LEntryCount + 1;
		}

		/// <summary>Inserts the given Key and Data streams into this node at the given index.</summary>
		public void Insert(Stream AKey, Stream AData, int AEntryNumber)
		{
			InternalInsert(AKey, AData, AEntryNumber);
			
			FIndex.RowsMoved(FStreamID, AEntryNumber, EntryCount - 2, FStreamID, 1);
		}
		
		public void Update(Stream AData, int AEntryNumber)
		{
			if (NodeType == IndexNodeType.Data)
				FIndex.DisposeData(FProcess, Data(AEntryNumber));

			AData.Position = 0;
			StreamUtility.CopyStream(AData, Data(AEntryNumber));
		}
		
		internal void InternalDelete(int AEntryNumber)
		{
			// foreach Entry > AEntryNumber, asc, copy the existing entry to the entry below
			int LEntryCount = EntryCount;
			for (int LIndex = AEntryNumber + 1; LIndex < LEntryCount; LIndex++)
				StreamUtility.CopyStream(Entry(LIndex), Entry(LIndex - 1));
			
			// Decrement EntryCount
			EntryCount = LEntryCount - 1;
		}

		/// <summary>Deletes the Entry given by the index from this node.</summary>		
		public void Delete(int AEntryNumber)
		{
			// Copy the data for the streams to be deleted
			if (NodeType == IndexNodeType.Data)
				FIndex.DisposeData(FProcess, Data(AEntryNumber));
			FIndex.DisposeKey(FProcess, Key(AEntryNumber));

			InternalDelete(AEntryNumber);
			
			FIndex.RowDeleted(FStreamID, AEntryNumber);
			FIndex.RowsMoved(FStreamID, AEntryNumber + 1, EntryCount, FStreamID, -1);
		}
	}
}
