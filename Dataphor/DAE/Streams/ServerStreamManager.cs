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
		public StreamHeader(StreamID streamID, IStreamProvider provider) : base()
		{
			_streamID = streamID;
			_provider = provider;
			#if LOCKSTREAMS
			FLockID = new LockID(this, AStreamID.ToString());
			#endif
		}
		
		private StreamID _streamID;
		public StreamID StreamID { get { return _streamID; } }
		
		private IStreamProvider _provider;
		public IStreamProvider Provider 
		{ 
			get { return _provider; }
			set { _provider = value; }
		}

		#if LOCKSTREAMS
		private LockID FLockID;
		public LockID LockID { get { return FLockID; } }
		#endif
		
		private StreamIDList _references;
		public StreamIDList References
		{
			get
			{
				if (_references == null)
					_references = new StreamIDList();
				return _references;
			}
		}
		
		private ServerStream _stream;
		public ServerStream Stream
		{
			get { return _stream; }
			set { _stream = value; }
		}
		
		private int _streamCount;
		public int StreamCount
		{
			get { return _streamCount; }
			set { _streamCount = value; }
		}
		
		public override int GetHashCode()
		{
			return _streamID.GetHashCode();
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is StreamHeader) && (((StreamHeader)objectValue).StreamID == _streamID);
		}

		public override string ToString()
		{
			return String.Format("StreamHeader (StreamID: {0}, Count: {1})", _streamID, _streamCount);
		}
	}
	
	public class StreamHeaders : Dictionary<StreamID, StreamHeader>
	{
		public StreamHeaders() : base(){}
		
		public void Add(StreamHeader streamHeader)
		{
			Add(streamHeader.StreamID, streamHeader);
		}
	}
	
	public class StreamEvent : System.Object
	{
		public StreamEvent(StreamID streamID) : base()
		{	
			StreamID = streamID;
		}
		
		public StreamID StreamID;

		public bool Deallocated;
		
		public int OpenCount;
	}
	
	public class RegisterStreamEvent : StreamEvent
	{
		public RegisterStreamEvent(StreamID streamID) : base(streamID) {}
		
		public override string ToString()
		{
			return String.Format("StreamID: {0}, Event: Register, Open Count: {1}, Deallocated: {2}.", StreamID.ToString(), OpenCount.ToString(), Deallocated.ToString());
		}
	}
	
	public class AllocateStreamEvent : StreamEvent
	{
		public AllocateStreamEvent(StreamID streamID) : base(streamID) {}
		
		public override string ToString()
		{
			return String.Format("StreamID: {0}, Event: Allocate, Open Count: {1}, Deallocated: {2}.", StreamID.ToString(), OpenCount.ToString(), Deallocated.ToString());
		}
	}
	
	public class ReferenceStreamEvent : StreamEvent
	{
		public ReferenceStreamEvent(StreamID streamID, StreamID referencedStreamID) : base(streamID)
		{
			ReferencedStreamID = referencedStreamID;
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
		public void Open(StreamID streamID)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].StreamID == streamID)
				{
					this[index].OpenCount++;
					break;
				}
		}
		
		public void Close(StreamID streamID)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].StreamID == streamID)
				{
					this[index].OpenCount--;
					break;
				}
		}
		
		public void Deallocate(StreamID streamID)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].StreamID == streamID)
				{
					this[index].Deallocated = true;
					break;
				}
		}
		
		public override string ToString()
		{
			StringBuilder results = new StringBuilder();
			for (int index = 0; index < Count; index++)
				results.AppendFormat("{0}\r\n", this[index].ToString());
			return results.ToString();
		}
	}
	
	public class ServerStreamManager : StreamManager
	{
		public ServerStreamManager(IServer server) : base()
		{
			#if UseFileStreamProvider
			FDefaultProvider = new FileStreamProvider();
			#else
			_defaultProvider = new MemoryStreamProvider();
			#endif
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_defaultProvider != null)
			{
				_defaultProvider.Dispose();
				_defaultProvider = null;
			}
			
			_headers = null;

			base.Dispose(disposing);
		}

		#if UseFileStreamProvider		
		private FileStreamProvider FDefaultProvider;
		#else
		private MemoryStreamProvider _defaultProvider;
		#endif
		private StreamHeaders _headers = new StreamHeaders();
		private Dictionary<StreamID, StreamID> _referencingHeaders = new Dictionary<StreamID, StreamID>(); // <stream id key> references <stream id value>
		private UInt64 _nextStreamID = 1; // must be 1 so that no stream could ever be 0 (the Null stream)
		
		private StreamEvents _streamEvents;
		public StreamEvents StreamEvents { get { return _streamEvents; } }
		
		private List<string> _streamOpens;
		public List<string> StreamOpens { get { return _streamOpens; } }
		
		public string StreamOpensAsString()
		{
			StringBuilder builder = new StringBuilder();
			foreach (String stringValue in _streamOpens)
				builder.AppendFormat("{0}\r\n", stringValue);
			return builder.ToString();
		}
		
		private bool _streamTracingEnabled;
		public bool StreamTracingEnabled
		{
			get { return _streamTracingEnabled; }
			set
			{
				if (_streamTracingEnabled != value)
				{
					_streamTracingEnabled = value;
					if (_streamTracingEnabled)
					{
						_streamEvents = new StreamEvents();
						_streamOpens = new List<string>();
					}
					else
					{
						_streamEvents = null;
						_streamOpens = null;
					}
				}
			}
		}
		
		private StreamID InternalGetNextStreamID()
		{
			StreamID streamID = new StreamID(_nextStreamID);
			_nextStreamID++;
			return streamID;
		}
		
		private StreamHeader GetStreamHeader(StreamID streamID)
		{
			StreamHeader header;
			if (!_headers.TryGetValue(streamID, out header))
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, streamID.ToString());
			return header;
		}
		
		public int Count()
		{
			lock (this)
			{
				return _headers.Count;
			}
		}
		
		private void InternalRegister(StreamID streamID, IStreamProvider provider)
		{
			_headers.Add(new StreamHeader(streamID, provider));
		}
		
		public StreamID Register(IStreamProvider provider)
		{
			lock (this)
			{
				StreamID streamID = InternalGetNextStreamID();
				InternalRegister(streamID, provider);
				if (StreamTracingEnabled)
					StreamEvents.Add(new RegisterStreamEvent(streamID));
				return streamID;
			}
		}
		
		private void InternalUnregister(StreamID streamID)
		{
			StreamHeader header = GetStreamHeader(streamID);
			_headers.Remove(streamID);
		}
		
		public void Unregister(StreamID streamID)
		{
			lock (this)
			{
				if (StreamTracingEnabled)
					StreamEvents.Deallocate(streamID);
				InternalUnregister(streamID);
			}
		}
		
		public override StreamID Allocate()
		{
			lock (this)
			{
				StreamID streamID = InternalGetNextStreamID();
				if (StreamTracingEnabled)
					StreamEvents.Add(new AllocateStreamEvent(streamID));
				_defaultProvider.Create(streamID);
				InternalRegister(streamID, _defaultProvider);
				return streamID;
			}
		}
		
		public override StreamID Reference(StreamID streamID)
		{
			lock (this)
			{
				StreamID localStreamID = InternalGetNextStreamID();
				StreamHeader header = GetStreamHeader(streamID);
				if (StreamTracingEnabled)
					StreamEvents.Add(new ReferenceStreamEvent(localStreamID, header.StreamID));
				header.References.Add(localStreamID);
				_referencingHeaders.Add(localStreamID, streamID);
				InternalRegister(localStreamID, _defaultProvider);
				return localStreamID;
			}
		}
		
		protected void InternalCopy(StreamHeader sourceStreamHeader, StreamHeader targetStreamHeader)
		{
			Stream sourceStream = InternalOpen(sourceStreamHeader, true);
			try
			{
				Stream targetStream = targetStreamHeader.Provider.Open(targetStreamHeader.StreamID);
				try
				{
					targetStream.Position = 0;
					StreamUtility.CopyStream(sourceStream, targetStream);
				}
				finally
				{
					targetStreamHeader.Provider.Close(targetStreamHeader.StreamID);
				}
			}
			finally
			{
				sourceStream.Close();
				InternalClose(sourceStreamHeader);
			}
		}
		
		public override void Deallocate(StreamID streamID)
		{
			lock (this)
			{
				StreamHeader header = GetStreamHeader(streamID);
				if (StreamTracingEnabled)
					StreamEvents.Deallocate(streamID);
					
				_headers.Remove(streamID);

				StreamID sourceStreamID;;
				if (!_referencingHeaders.TryGetValue(streamID, out sourceStreamID))
				{
					// if this stream is a referenced stream
					if (header.References.Count > 0)
					{
						// select a referencing stream to be the new source stream
						StreamID newStreamID = header.References[0];
						StreamHeader newHeader = GetStreamHeader(newStreamID);

						// reassign the stream id for the stream in the provider (from this stream to the new source stream id)
						header.Provider.Reassign(header.StreamID, newStreamID);

						// change the provider in the header for the new source stream id
						newHeader.Provider = header.Provider;
						if (newHeader.Stream != null)
							newHeader.Stream.SourceStream = newHeader.Provider.Open(newStreamID); // TODO: Shouldn't this close the old stream?
						
						// dereference the new header
						_referencingHeaders.Remove(newStreamID);
						header.References.RemoveAt(0);

						// move all references to this stream to the new source stream
						MoveReferences(header, newHeader);
					}
					else
						// destroy this stream
						header.Provider.Destroy(streamID);
				}
				else
				{
					// if this stream is a reference stream
					StreamHeader sourceHeader = GetStreamHeader(sourceStreamID);

					// move all references to this stream to the source stream
					MoveReferences(header, sourceHeader);

					// dereference the source stream
					sourceHeader.References.Remove(streamID);
					_referencingHeaders.Remove(streamID);
				}
			}
		}

		public bool IsLocked(StreamID streamID)
		{
			return false;
		}
		
		public void Change(ServerStream stream)
		{
			lock (this)
			{
				StreamHeader header = GetStreamHeader(stream.StreamID);
				
				// if this stream is a reference stream
				StreamID sourceStreamID;
				if (_referencingHeaders.TryGetValue(stream.StreamID, out sourceStreamID))
				{
					StreamHeader sourceHeader = GetStreamHeader(sourceStreamID);

					// dereference the source stream
					_referencingHeaders.Remove(stream.StreamID);
					sourceHeader.References.Remove(stream.StreamID);

					// copy the data from the source stream into this stream
					_defaultProvider.Create(stream.StreamID);
					InternalCopy(sourceHeader, header);
					header.Stream.SourceStream = _defaultProvider.Open(stream.StreamID);

					// move all references to this stream to the source stream
					MoveReferences(header, sourceHeader);
				}
			}
		}
		
		protected void MoveReferences(StreamHeader fromHeader, StreamHeader toHeader)
		{
			for (int index = fromHeader.References.Count - 1; index >= 0; index--)
			{
				StreamID streamID = fromHeader.References[index];
				
				// Remove the reference to the from header
				fromHeader.References.RemoveAt(index);
				_referencingHeaders.Remove(streamID);
				
				// Add the reference to the to header
				toHeader.References.Add(streamID);
				_referencingHeaders.Add(streamID, toHeader.StreamID);

				// Make sure the referencing header has the right stream
				StreamHeader referenceHeader = GetStreamHeader(streamID);
				if (referenceHeader.Stream != null)
					referenceHeader.Stream.SourceStream = toHeader.Provider.Open(toHeader.StreamID); // TODO: Shouldn't this close the old stream?
			}
		}
		
		protected Stream InternalOpen(StreamHeader header, bool cover)
		{
			if (header.Stream == null)
			{
				StreamID sourceStreamID;
				if (_referencingHeaders.TryGetValue(header.StreamID, out sourceStreamID))
					header.Stream = new ServerStream(this, header.StreamID, InternalOpen(GetStreamHeader(sourceStreamID), true));
				else
					header.Stream = new ServerStream(this, header.StreamID, header.Provider.Open(header.StreamID));
			}
			header.StreamCount++;
			if (cover)
				return new CoverStream(header.Stream);
			return header.Stream;
		}
		
		#if DEBUG
		private int _openCount;
		#endif
		public override Stream Open(int ownerID, StreamID streamID, LockMode mode)
		{
			lock (this)
			{
				#if DEBUG
				_openCount++;
				#endif
				if (_streamTracingEnabled)
				{
					StreamEvents.Open(streamID);
					StreamOpens.Add(String.Format("Open {0}", streamID.Value.ToString()));
				}
				StreamHeader header = GetStreamHeader(streamID);
				return new ManagedStream(this, ownerID, streamID, InternalOpen(header, false));
			}
		}
		
		protected void InternalClose(StreamHeader header)
		{
			header.StreamCount--;
			if (header.StreamCount == 0)
			{
				StreamID sourceStreamID;
				if (_referencingHeaders.TryGetValue(header.StreamID, out sourceStreamID))
				{
					Stream sourceStream = header.Stream.SourceStream;
					header.Stream.SourceStream = null;
					sourceStream.Close();
					InternalClose(GetStreamHeader(sourceStreamID));
				}
				else
					header.Provider.Close(header.StreamID);
				header.Stream.Close();
				header.Stream = null;
			}
		}
		
		#if DEBUG
		private int _closeCount;
		#endif
		public override void Close(int ownerID, StreamID streamID)
		{
			lock (this)
			{
				#if DEBUG
				_closeCount++;
				#endif
				if (_streamTracingEnabled)
				{
					StreamEvents.Close(streamID);
					StreamOpens.Add(String.Format("Close {0}", streamID.Value.ToString()));
				}
				StreamHeader header = GetStreamHeader(streamID);
				InternalClose(header);
			}
		}
	}
}

