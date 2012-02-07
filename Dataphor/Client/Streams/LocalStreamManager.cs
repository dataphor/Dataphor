/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Streams
{
	using Alphora.Dataphor.DAE.Runtime;
	
	public class LocalStreamHeader : Disposable
	{
		public LocalStreamHeader(StreamID streamID) : base()
		{
			_streamID = streamID;
		}
		
		public LocalStreamHeader(StreamID streamID, LocalStream stream) : base()
		{
			_streamID = streamID;
			_stream = stream;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_stream != null)
			{
				_stream.Close();
				_stream = null;
			}

			base.Dispose(disposing);
		}
		
		private StreamID _streamID;
		public StreamID StreamID { get { return _streamID; } }

		private LocalStream _stream;
		public LocalStream Stream 
		{ 
			get { return _stream; } 
			set { _stream = value; }
		}
	}
	
	public class LocalStreamHeaders : Dictionary<StreamID, LocalStreamHeader>, IDisposable
	{
		public LocalStreamHeaders() : base(){}
		
		#if USEFINALIZER
		~LocalStreamHeaders()
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
			foreach (KeyValuePair<StreamID, LocalStreamHeader> entry in this)
				entry.Value.Dispose();
			Clear();
		}

		public void Add(LocalStreamHeader stream)
		{
			Add(stream.StreamID, stream);
		}
	}
	
	public class LocalStreamManager : Disposable, IStreamManager
	{
		public LocalStreamManager(IStreamManager sourceStreamManager)
		{
			_sourceStreamManager = sourceStreamManager;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_headers != null)
			{
				foreach (LocalStreamHeader header in _headers.Values)
				{
					if (header.Stream != null)
					{
						try
						{
							header.Stream.Reset();
							#if UNMANAGEDSTREAM
							FSourceStreamManager.Close(header.StreamID);
							#else
							header.Stream.Close();
							#endif
						}
						catch
						{
							// ignore exceptions here, the stream is disposed and we don't care
						}

						header.Stream = null;
					}
				}
				_headers.Dispose();
				_headers = null;
			}
			base.Dispose(disposing);
		}
		
		private IStreamManager _sourceStreamManager;
		public IStreamManager SourceStreamManager { get { return _sourceStreamManager; } }

		private LocalStreamHeaders _headers = new LocalStreamHeaders();

		// IStreamManager
		public StreamID Allocate()
		{
			// Allocates a new stream in the source stream manager, and saves the header and local cache for it locally
			StreamID streamID = _sourceStreamManager.Allocate();
			LocalStreamHeader header = new LocalStreamHeader(streamID);
			_headers.Add(header);
			return streamID;
		}
		
		public StreamID Reference(StreamID streamID)
		{
			LocalStreamHeader targetHeader;
			if (_headers.TryGetValue(streamID, out targetHeader) && (targetHeader.Stream != null) && targetHeader.Stream.Modified)
			{
				StreamID localStreamID = Allocate();
				Stream stream = Open(localStreamID, LockMode.Exclusive);
				try
				{
					Stream targetStream = new CoverStream(targetHeader.Stream);
					try
					{
						StreamUtility.CopyStream(targetStream, stream);
						return localStreamID;
					}
					finally
					{
						targetStream.Close();
					}
				}
				finally
				{
					stream.Close();
					Close(localStreamID);
				}
			}
			else
			{
				StreamID localStreamID = _sourceStreamManager.Reference(streamID);
				LocalStreamHeader header = new LocalStreamHeader(localStreamID);
				_headers.Add(header);
				return localStreamID;
			}
		}
		
		public void Deallocate(StreamID streamID)
		{
			// Deallocates the given stream in the source stream manager, and removes the header and local cache for it locally, without flushing
			LocalStreamHeader header;
			if (_headers.TryGetValue(streamID, out header))
			{
				if (header.Stream != null)
				{
					header.Stream.Reset();
					#if UNMANAGEDSTREAM
					FSourceStreamManager.Close(header.StreamID);
					#else
					header.Stream.Close();
					#endif
					header.Stream = null;
				}
				_sourceStreamManager.Deallocate(header.StreamID);
				_headers.Remove(header.StreamID);
				header.Dispose();
			}
			else
				_sourceStreamManager.Deallocate(streamID);
		}
		
		public Stream Open(StreamID streamID, LockMode mode)
		{
			// Ensures that the given stream is supported by a local cache and returns a stream accessing it
			LocalStreamHeader header;
			if (!_headers.TryGetValue(streamID, out header))
			{
				header = new LocalStreamHeader(streamID, new LocalStream(this, streamID, mode)); //FSourceStreamManager.Open(AStreamID, AMode)));
				_headers.Add(header);
			}
			else if (header.Stream == null)
				header.Stream = new LocalStream(this, streamID, mode); //, FSourceStreamManager.Open(AStreamID, AMode));
			return new CoverStream(header.Stream);
		}

		public IRemoteStream OpenRemote(StreamID streamID, LockMode mode)
		{
			Stream stream = Open(streamID, mode);
			IRemoteStream result = stream as IRemoteStream;
			if (result != null)
				return result;
			return new CoverStream(stream);
		}
		
		public void Close(StreamID streamID)
		{
			// Close takes no action, the local cache is still maintained, so the remote stream is kept open
			// a call to flush is required to force the data back to the remote stream manager
		}

		// FlushStreams -- called by the local cursors to ensure that the overflow for a row is consistent
		public void FlushStreams(StreamID[] streamIDs)
		{
			foreach (StreamID streamID in streamIDs)
				Flush(streamID);
		}
		
		public void Flush(StreamID streamID)
		{
			// Ensures that the given local cache is flushed to the source stream manager
			LocalStreamHeader header;
			if (_headers.TryGetValue(streamID, out header) && (header.Stream != null))
				header.Stream.Flush();
		}

		public void Release(StreamID streamID)
		{
			// Ensures that the given local cache is flushed and closes the stream obtained from the source stream manager
			LocalStreamHeader header;
			if (_headers.TryGetValue(streamID, out header))
			{
				if (header.Stream != null)
				{
					header.Stream.Flush();
					#if UNMANAGEDSTREAM
					FSourceStreamManager.Close(AStreamID);
					#else
					header.Stream.Close();
					#endif
					header.Stream = null;
				}
				_headers.Remove(header.StreamID);
				header.Dispose();
			}
		}

		public void ReleaseStreams(StreamID[] streamIDs)
		{
			foreach (StreamID streamID in streamIDs)
				Release(streamID);
		}
	}
}

