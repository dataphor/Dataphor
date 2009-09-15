/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
//#define UseFileStreamProvider
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Streams
{
	public class StreamHeader
	{
		public StreamHeader(int AResourceManagerID, StreamID AStreamID, IStreamProvider AProvider) : base()
		{
			FStreamID = AStreamID;
			FProvider = AProvider;
			#if LOCKSTREAMS
			FLockID = new LockID(AResourceManagerID, AStreamID.ToString());
			#endif
		}
		
		private StreamID FStreamID;
		public StreamID StreamID { get { return FStreamID; } }
		
		private IStreamProvider FProvider;
		public IStreamProvider Provider 
		{ 
			get { return FProvider; }
			set { FProvider = value; }
		}

		#if LOCKSTREAMS
		private LockID FLockID;
		public LockID LockID { get { return FLockID; } }
		#endif
		
		private StreamIDList FReferences;
		public StreamIDList References
		{
			get
			{
				if (FReferences == null)
					FReferences = new StreamIDList();
				return FReferences;
			}
		}
		
		private ServerStream FStream;
		public ServerStream Stream
		{
			get { return FStream; }
			set { FStream = value; }
		}
		
		private int FStreamCount;
		public int StreamCount
		{
			get { return FStreamCount; }
			set { FStreamCount = value; }
		}
		
		public override int GetHashCode()
		{
			return FStreamID.GetHashCode();
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is StreamHeader) && (((StreamHeader)AObject).StreamID == FStreamID);
		}
	}
	
	public class StreamHeaders : Hashtable
	{
		public StreamHeaders() : base(){}
		
		public StreamHeader this[StreamID AStreamID] { get { return (StreamHeader)base[AStreamID]; } }
		
		public void Add(StreamHeader AStreamHeader)
		{
			Add(AStreamHeader.StreamID, AStreamHeader);
		}
	}
	
	public class StreamEvent : System.Object
	{
		public StreamEvent(StreamID AStreamID) : base()
		{	
			StreamID = AStreamID;
		}
		
		public StreamID StreamID;

		public bool Deallocated;
		
		public int OpenCount;
	}
	
	public class RegisterStreamEvent : StreamEvent
	{
		public RegisterStreamEvent(StreamID AStreamID) : base(AStreamID) {}
		
		public override string ToString()
		{
			return String.Format("StreamID: {0}, Event: Register, Open Count: {1}, Deallocated: {2}.", StreamID.ToString(), OpenCount.ToString(), Deallocated.ToString());
		}
	}
	
	public class AllocateStreamEvent : StreamEvent
	{
		public AllocateStreamEvent(StreamID AStreamID) : base(AStreamID) {}
		
		public override string ToString()
		{
			return String.Format("StreamID: {0}, Event: Allocate, Open Count: {1}, Deallocated: {2}.", StreamID.ToString(), OpenCount.ToString(), Deallocated.ToString());
		}
	}
	
	public class ReferenceStreamEvent : StreamEvent
	{
		public ReferenceStreamEvent(StreamID AStreamID, StreamID AReferencedStreamID) : base(AStreamID)
		{
			ReferencedStreamID = AReferencedStreamID;
		}
		
		public StreamID ReferencedStreamID;

		public override string ToString()
		{
			return String.Format("StreamID: {0}, Event: Reference, Referenced StreamID: {1}, Open Count: {2}, Deallocated: {3}.", StreamID.ToString(), ReferencedStreamID.ToString(), OpenCount.ToString(), Deallocated.ToString());
		}
	}
	
	#if USETYPEDLIST
	public class StreamEvents : TypedList
	{
		public StreamEvents() : base(typeof(StreamEvent)) {}
		
		public new StreamEvent this[int AIndex]
		{
			get { return (StreamEvent)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class StreamEvents : BaseList<StreamEvent>
	{
	#endif
		public void Open(StreamID AStreamID)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].StreamID == AStreamID)
				{
					this[LIndex].OpenCount++;
					break;
				}
		}
		
		public void Close(StreamID AStreamID)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].StreamID == AStreamID)
				{
					this[LIndex].OpenCount--;
					break;
				}
		}
		
		public void Deallocate(StreamID AStreamID)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].StreamID == AStreamID)
				{
					this[LIndex].Deallocated = true;
					break;
				}
		}
		
		public override string ToString()
		{
			StringBuilder LResults = new StringBuilder();
			for (int LIndex = 0; LIndex < Count; LIndex++)
				LResults.AppendFormat("{0}\r\n", this[LIndex].ToString());
			return LResults.ToString();
		}
	}
	
	public class ServerStreamManager : StreamManager
	{
		public ServerStreamManager(int AResourceManagerID, LockManager ALockManager, IServer AServer) : base()
		{
			FResourceManagerID = AResourceManagerID;
			FLockManager = ALockManager;
			#if UseFileStreamProvider
			FDefaultProvider = new FileStreamProvider();
			#else
			FDefaultProvider = new MemoryStreamProvider();
			#endif
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FDefaultProvider != null)
			{
				FDefaultProvider.Dispose();
				FDefaultProvider = null;
			}
			
			FHeaders = null;
			FLockManager = null;
			FResourceManagerID = -1;

			base.Dispose(ADisposing);
		}

		#if UseFileStreamProvider		
		private FileStreamProvider FDefaultProvider;
		#else
		private MemoryStreamProvider FDefaultProvider;
		#endif
		private StreamHeaders FHeaders = new StreamHeaders();
		private Dictionary<StreamID, StreamID> FReferencingHeaders = new Dictionary<StreamID, StreamID>(); // <stream id key> references <stream id value>
		private UInt64 FNextStreamID = 1; // must be 1 so that no stream could ever be 0 (the Null stream)
		private LockManager FLockManager;
		private int FResourceManagerID;
		
		private StreamEvents FStreamEvents;
		public StreamEvents StreamEvents { get { return FStreamEvents; } }
		
		private List<string> FStreamOpens;
		public List<string> StreamOpens { get { return FStreamOpens; } }
		
		public string StreamOpensAsString()
		{
			StringBuilder LBuilder = new StringBuilder();
			foreach (String LString in FStreamOpens)
				LBuilder.AppendFormat("{0}\r\n", LString);
			return LBuilder.ToString();
		}
		
		private bool FStreamTracingEnabled;
		public bool StreamTracingEnabled
		{
			get { return FStreamTracingEnabled; }
			set
			{
				if (FStreamTracingEnabled != value)
				{
					FStreamTracingEnabled = value;
					if (FStreamTracingEnabled)
					{
						FStreamEvents = new StreamEvents();
						FStreamOpens = new List<string>();
					}
					else
					{
						FStreamEvents = null;
						FStreamOpens = null;
					}
				}
			}
		}
		
		private StreamID InternalGetNextStreamID()
		{
			StreamID LStreamID = new StreamID(FNextStreamID);
			FNextStreamID++;
			return LStreamID;
		}
		
		private StreamHeader GetStreamHeader(StreamID AStreamID)
		{
			StreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, AStreamID.ToString());
			return LHeader;
		}
		
		public int Count()
		{
			lock (this)
			{
				return FHeaders.Count;
			}
		}
		
		private void InternalRegister(StreamID AStreamID, IStreamProvider AProvider)
		{
			FHeaders.Add(new StreamHeader(FResourceManagerID, AStreamID, AProvider));
		}
		
		public StreamID Register(IStreamProvider AProvider)
		{
			lock (this)
			{
				StreamID LStreamID = InternalGetNextStreamID();
				InternalRegister(LStreamID, AProvider);
				if (StreamTracingEnabled)
					StreamEvents.Add(new RegisterStreamEvent(LStreamID));
				return LStreamID;
			}
		}
		
		private void InternalUnregister(StreamID AStreamID)
		{
			StreamHeader LHeader = GetStreamHeader(AStreamID);
			FHeaders.Remove(AStreamID);
		}
		
		public void Unregister(StreamID AStreamID)
		{
			lock (this)
			{
				if (StreamTracingEnabled)
					StreamEvents.Deallocate(AStreamID);
				InternalUnregister(AStreamID);
			}
		}
		
		public override StreamID Allocate()
		{
			lock (this)
			{
				StreamID LStreamID = InternalGetNextStreamID();
				if (StreamTracingEnabled)
					StreamEvents.Add(new AllocateStreamEvent(LStreamID));
				FDefaultProvider.Create(LStreamID);
				InternalRegister(LStreamID, FDefaultProvider);
				return LStreamID;
			}
		}
		
		public override StreamID Reference(StreamID AStreamID)
		{
			lock (this)
			{
				StreamID LStreamID = InternalGetNextStreamID();
				StreamHeader LHeader = GetStreamHeader(AStreamID);
				if (StreamTracingEnabled)
					StreamEvents.Add(new ReferenceStreamEvent(LStreamID, LHeader.StreamID));
				LHeader.References.Add(LStreamID);
				FReferencingHeaders.Add(LStreamID, AStreamID);
				InternalRegister(LStreamID, FDefaultProvider);
				return LStreamID;
			}
		}
		
		protected void InternalCopy(StreamHeader ASourceStreamHeader, StreamHeader ATargetStreamHeader)
		{
			Stream LSourceStream = InternalOpen(ASourceStreamHeader, true);
			try
			{
				Stream LTargetStream = ATargetStreamHeader.Provider.Open(ATargetStreamHeader.StreamID);
				try
				{
					LTargetStream.Position = 0;
					StreamUtility.CopyStream(LSourceStream, LTargetStream);
				}
				finally
				{
					ATargetStreamHeader.Provider.Close(ATargetStreamHeader.StreamID);
				}
			}
			finally
			{
				LSourceStream.Close();
				InternalClose(ASourceStreamHeader);
			}
		}
		
		public override void Deallocate(StreamID AStreamID)
		{
			lock (this)
			{
				StreamHeader LHeader = GetStreamHeader(AStreamID);
				#if LOCKSTREAMS
				if (FLockManager.IsLocked(LHeader.LockID))
				{
					if (StreamTracingEnabled)
						FStreamOpens.Add(String.Format("Deallocation Exception {0}", AStreamID.ToString()));
					#if FINDLEAKS
					throw new StreamsException(StreamsException.Codes.StreamInUse, AStreamID.ToString());
					#endif
				}
				#endif
				if (StreamTracingEnabled)
					StreamEvents.Deallocate(AStreamID);
					
				FHeaders.Remove(AStreamID);

				StreamID LSourceStreamID;;
				if (!FReferencingHeaders.TryGetValue(AStreamID, out LSourceStreamID))
				{
					// if this stream is a referenced stream
					if (LHeader.References.Count > 0)
					{
						// select a referencing stream to be the new source stream
						StreamID LNewStreamID = LHeader.References[0];
						StreamHeader LNewHeader = GetStreamHeader(LNewStreamID);

						// reassign the stream id for the stream in the provider (from this stream to the new source stream id)
						LHeader.Provider.Reassign(LHeader.StreamID, LNewStreamID);

						// change the provider in the header for the new source stream id
						LNewHeader.Provider = LHeader.Provider;
						if (LNewHeader.Stream != null)
							LNewHeader.Stream.SourceStream = LNewHeader.Provider.Open(LNewStreamID); // TODO: Shouldn't this close the old stream?
						
						// dereference the new header
						FReferencingHeaders.Remove(LNewStreamID);
						LHeader.References.RemoveAt(0);

						// move all references to this stream to the new source stream
						MoveReferences(LHeader, LNewHeader);
					}
					else
						// destroy this stream
						LHeader.Provider.Destroy(AStreamID);
				}
				else
				{
					// if this stream is a reference stream
					StreamHeader LSourceHeader = GetStreamHeader(LSourceStreamID);

					// move all references to this stream to the source stream
					MoveReferences(LHeader, LSourceHeader);

					// dereference the source stream
					LSourceHeader.References.Remove(AStreamID);
					FReferencingHeaders.Remove(AStreamID);
				}
			}
		}

		// here for diagnostics
		public bool IsLocked(StreamID AStreamID)
		{
			#if LOCKSTREAMS
			return FLockManager.IsLocked(GetStreamHeader(AStreamID).LockID);
			#else
			return false;
			#endif
		}
		
		public void Change(ServerStream AStream)
		{
			lock (this)
			{
				StreamHeader LHeader = GetStreamHeader(AStream.StreamID);
				
				// if this stream is a reference stream
				StreamID LSourceStreamID;
				if (FReferencingHeaders.TryGetValue(AStream.StreamID, out LSourceStreamID))
				{
					StreamHeader LSourceHeader = GetStreamHeader(LSourceStreamID);

					// dereference the source stream
					FReferencingHeaders.Remove(AStream.StreamID);
					LSourceHeader.References.Remove(AStream.StreamID);

					// copy the data from the source stream into this stream
					FDefaultProvider.Create(AStream.StreamID);
					InternalCopy(LSourceHeader, LHeader);
					LHeader.Stream.SourceStream = FDefaultProvider.Open(AStream.StreamID);

					// move all references to this stream to the source stream
					MoveReferences(LHeader, LSourceHeader);
				}
			}
		}
		
		protected void MoveReferences(StreamHeader AFromHeader, StreamHeader AToHeader)
		{
			for (int LIndex = AFromHeader.References.Count - 1; LIndex >= 0; LIndex--)
			{
				StreamID LStreamID = AFromHeader.References[LIndex];
				
				// Remove the reference to the from header
				AFromHeader.References.RemoveAt(LIndex);
				FReferencingHeaders.Remove(LStreamID);
				
				// Add the reference to the to header
				AToHeader.References.Add(LStreamID);
				FReferencingHeaders.Add(LStreamID, AToHeader.StreamID);

				// Make sure the referencing header has the right stream
				StreamHeader LReferenceHeader = GetStreamHeader(LStreamID);
				if (LReferenceHeader.Stream != null)
					LReferenceHeader.Stream.SourceStream = AToHeader.Provider.Open(AToHeader.StreamID); // TODO: Shouldn't this close the old stream?
			}
		}
		
		protected Stream InternalOpen(StreamHeader AHeader, bool ACover)
		{
			if (AHeader.Stream == null)
			{
				StreamID LSourceStreamID;
				if (FReferencingHeaders.TryGetValue(AHeader.StreamID, out LSourceStreamID))
					AHeader.Stream = new ServerStream(this, AHeader.StreamID, InternalOpen(GetStreamHeader(LSourceStreamID), true));
				else
					AHeader.Stream = new ServerStream(this, AHeader.StreamID, AHeader.Provider.Open(AHeader.StreamID));
			}
			AHeader.StreamCount++;
			if (ACover)
				return new CoverStream(AHeader.Stream);
			return AHeader.Stream;
		}
		
		#if DEBUG
		private int FOpenCount;
		#endif
		public override Stream Open(int AOwnerID, StreamID AStreamID, LockMode AMode)
		{
			lock (this)
			{
				#if DEBUG
				FOpenCount++;
				#endif
				if (FStreamTracingEnabled)
				{
					StreamEvents.Open(AStreamID);
					StreamOpens.Add(String.Format("Open {0}", AStreamID.Value.ToString()));
				}
				StreamHeader LHeader = GetStreamHeader(AStreamID);
				#if LOCKSTREAMS
				FLockManager.Lock(AOwnerID, LHeader.LockID, AMode);
				#endif
				return new ManagedStream(this, AOwnerID, AStreamID, InternalOpen(LHeader, false));
			}
		}
		
		protected void InternalClose(StreamHeader AHeader)
		{
			AHeader.StreamCount--;
			if (AHeader.StreamCount == 0)
			{
				StreamID LSourceStreamID;
				if (FReferencingHeaders.TryGetValue(AHeader.StreamID, out LSourceStreamID))
				{
					Stream LSourceStream = AHeader.Stream.SourceStream;
					AHeader.Stream.SourceStream = null;
					LSourceStream.Close();
					InternalClose(GetStreamHeader(LSourceStreamID));
				}
				else
					AHeader.Provider.Close(AHeader.StreamID);
				AHeader.Stream.Close();
				AHeader.Stream = null;
			}
		}
		
		#if DEBUG
		private int FCloseCount;
		#endif
		public override void Close(int AOwnerID, StreamID AStreamID)
		{
			lock (this)
			{
				#if DEBUG
				FCloseCount++;
				#endif
				if (FStreamTracingEnabled)
				{
					StreamEvents.Close(AStreamID);
					StreamOpens.Add(String.Format("Close {0}", AStreamID.Value.ToString()));
				}
				StreamHeader LHeader = GetStreamHeader(AStreamID);
				InternalClose(LHeader);
				#if LOCKSTREAMS
				FLockManager.Unlock(AOwnerID, LHeader.LockID);
				#endif
			}
		}
	}
}

