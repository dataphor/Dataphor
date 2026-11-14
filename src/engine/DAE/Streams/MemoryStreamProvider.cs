/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	
	public class MemoryStreamHeader : Disposable
	{
		public MemoryStreamHeader(StreamID streamID)
		{
			StreamID = streamID;
			Stream = new MemoryStream();
		}
		
		protected override void Dispose(bool disposing)
		{
			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}

			base.Dispose(disposing);
		}
		
		public StreamID StreamID;
		public Stream Stream;
	}
	
	public class MemoryStreamHeaders : Dictionary<StreamID, MemoryStreamHeader>, IDisposable
	{
		public MemoryStreamHeaders() : base(){}
		
		public new MemoryStreamHeader this[StreamID streamID] 
		{ 
			get { return (MemoryStreamHeader)base[streamID]; } 
		}
		
		#if USEFINALIZER
		~MemoryStreamHeaders()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
		
		public void Dispose()
		{
			#if USEFINALIZER
			GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}
		
		protected void Dispose(bool disposing)
		{
			foreach (KeyValuePair<StreamID, MemoryStreamHeader> entry in this)
				entry.Value.Dispose();
			Clear();
		}

		public void Add(MemoryStreamHeader stream)
		{
			Add(stream.StreamID, stream);
		}
	}
	
	public class MemoryStreamProvider : StreamProvider
	{
		protected override void Dispose(bool disposing)
		{
			if (_headers != null)
			{
				_headers.Dispose();
				_headers = null;
			}
			
			base.Dispose(disposing);
		}
		
		private MemoryStreamHeaders _headers = new MemoryStreamHeaders();
		
		public void Create(StreamID iD)
		{
			lock (this)
			{
				_headers.Add(new MemoryStreamHeader(iD));
			}
		}
		
		private MemoryStreamHeader GetStreamHeader(StreamID streamID)
		{
			MemoryStreamHeader header;
			if (!_headers.TryGetValue(streamID, out header))
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, streamID.ToString());
			return header;
		}
		
		public override Stream Open(StreamID streamID)
		{
			lock (this)
			{
				MemoryStreamHeader header = GetStreamHeader(streamID);
				return header.Stream;
			}
		}
		
		public override void Close(StreamID streamID)
		{
			// no cleanup to perform here
		}
		
		public override void Destroy(StreamID streamID)
		{
			lock (this)
			{
				MemoryStreamHeader header = GetStreamHeader(streamID);
				_headers.Remove(streamID);
				header.Dispose();
			}
		}

		public override void Reassign(StreamID oldStreamID, StreamID newStreamID)
		{
			lock (this)
			{
				MemoryStreamHeader header = GetStreamHeader(oldStreamID);
				_headers.Remove(oldStreamID);
				header.StreamID = newStreamID;
				_headers.Add(header);
			}
		}
	}
}