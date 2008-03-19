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
	
	using Alphora.Dataphor.DAE.Runtime;
	
	public class LocalStreamHeader : Disposable
	{
		public LocalStreamHeader(StreamID AStreamID) : base()
		{
			FStreamID = AStreamID;
		}
		
		public LocalStreamHeader(StreamID AStreamID, LocalStream AStream) : base()
		{
			FStreamID = AStreamID;
			FStream = AStream;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FStream != null)
			{
				FStream.Close();
				FStream = null;
			}

			base.Dispose(ADisposing);
		}
		
		private StreamID FStreamID;
		public StreamID StreamID { get { return FStreamID; } }

		private LocalStream FStream;
		public LocalStream Stream 
		{ 
			get { return FStream; } 
			set { FStream = value; }
		}
	}
	
	public class LocalStreamHeaders : Hashtable, IDisposable
	{
		public LocalStreamHeaders() : base(){}
		
		public LocalStreamHeader this[StreamID AStreamID] { get { return (LocalStreamHeader)base[AStreamID]; } }
		
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
		
		protected void Dispose(bool ADisposing)
		{
			foreach (DictionaryEntry LEntry in this)
				((LocalStreamHeader)LEntry.Value).Dispose();
			Clear();
		}

		public void Add(LocalStreamHeader AStream)
		{
			Add(AStream.StreamID, AStream);
		}
	}
	
	public class LocalStreamManager : Disposable, IStreamManager
	{
		public LocalStreamManager(IStreamManager ASourceStreamManager)
		{
			FSourceStreamManager = ASourceStreamManager;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FHeaders != null)
			{
				foreach (LocalStreamHeader LHeader in FHeaders.Values)
				{
					if (LHeader.Stream != null)
					{
						try
						{
							LHeader.Stream.Reset();
							#if UNMANAGEDSTREAM
							FSourceStreamManager.Close(LHeader.StreamID);
							#else
							LHeader.Stream.Close();
							#endif
						}
						catch
						{
							// ignore exceptions here, the stream is disposed and we don't care
						}

						LHeader.Stream = null;
					}
				}
				FHeaders.Dispose();
				FHeaders = null;
			}
			base.Dispose(ADisposing);
		}
		
		private IStreamManager FSourceStreamManager;
		public IStreamManager SourceStreamManager { get { return FSourceStreamManager; } }

		private LocalStreamHeaders FHeaders = new LocalStreamHeaders();

		// IStreamManager
		public StreamID Allocate()
		{
			// Allocates a new stream in the source stream manager, and saves the header and local cache for it locally
			StreamID LStreamID = FSourceStreamManager.Allocate();
			LocalStreamHeader LHeader = new LocalStreamHeader(LStreamID);
			FHeaders.Add(LHeader);
			return LStreamID;
		}
		
		public StreamID Reference(StreamID AStreamID)
		{
			LocalStreamHeader LTargetHeader = FHeaders[AStreamID];
			if ((LTargetHeader != null) && (LTargetHeader.Stream != null) && LTargetHeader.Stream.Modified)
			{
				StreamID LStreamID = Allocate();
				Stream LStream = Open(LStreamID, LockMode.Exclusive);
				try
				{
					Stream LTargetStream = new CoverStream(LTargetHeader.Stream);
					try
					{
						StreamUtility.CopyStream(LTargetStream, LStream);
						return LStreamID;
					}
					finally
					{
						LTargetStream.Close();
					}
				}
				finally
				{
					LStream.Close();
					Close(LStreamID);
				}
			}
			else
			{
				StreamID LStreamID = FSourceStreamManager.Reference(AStreamID);
				LocalStreamHeader LHeader = new LocalStreamHeader(LStreamID);
				FHeaders.Add(LHeader);
				return LStreamID;
			}
		}
		
		public void Deallocate(StreamID AStreamID)
		{
			// Deallocates the given stream in the source stream manager, and removes the header and local cache for it locally, without flushing
			LocalStreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader != null)
			{
				if (LHeader.Stream != null)
				{
					LHeader.Stream.Reset();
					#if UNMANAGEDSTREAM
					FSourceStreamManager.Close(LHeader.StreamID);
					#else
					LHeader.Stream.Close();
					#endif
					LHeader.Stream = null;
				}
				FSourceStreamManager.Deallocate(LHeader.StreamID);
				FHeaders.Remove(LHeader.StreamID);
				LHeader.Dispose();
			}
			else
				FSourceStreamManager.Deallocate(AStreamID);
		}
		
		public Stream Open(StreamID AStreamID, LockMode AMode)
		{
			// Ensures that the given stream is supported by a local cache and returns a stream accessing it
			LocalStreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader == null)
			{
				LHeader = new LocalStreamHeader(AStreamID, new LocalStream(this, AStreamID, AMode)); //FSourceStreamManager.Open(AStreamID, AMode)));
				FHeaders.Add(LHeader);
			}
			else if (LHeader.Stream == null)
				LHeader.Stream = new LocalStream(this, AStreamID, AMode); //, FSourceStreamManager.Open(AStreamID, AMode));
			return new CoverStream(LHeader.Stream);
		}

		public IRemoteStream OpenRemote(StreamID AStreamID, LockMode AMode)
		{
			Stream LStream = Open(AStreamID, AMode);
			IRemoteStream LResult = LStream as IRemoteStream;
			if (LResult != null)
				return LResult;
			return new CoverStream(LStream);
		}
		
		public void Close(StreamID AStreamID)
		{
			// Close takes no action, the local cache is still maintained, so the remote stream is kept open
			// a call to flush is required to force the data back to the remote stream manager
		}

		// FlushStreams -- called by the local cursors to ensure that the overflow for a row is consistent
		public void FlushStreams(StreamID[] AStreamIDs)
		{
			foreach (StreamID LStreamID in AStreamIDs)
				Flush(LStreamID);
		}
		
		public void Flush(StreamID AStreamID)
		{
			// Ensures that the given local cache is flushed to the source stream manager
			LocalStreamHeader LHeader = FHeaders[AStreamID];
			if ((LHeader != null) && (LHeader.Stream != null))
				LHeader.Stream.Flush();
		}

		public void Release(StreamID AStreamID)
		{
			// Ensures that the given local cache is flushed and closes the stream obtained from the source stream manager
			LocalStreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader != null)
			{
				if (LHeader.Stream != null)
				{
					LHeader.Stream.Flush();
					#if UNMANAGEDSTREAM
					FSourceStreamManager.Close(AStreamID);
					#else
					LHeader.Stream.Close();
					#endif
					LHeader.Stream = null;
				}
				FHeaders.Remove(LHeader.StreamID);
				LHeader.Dispose();
			}
		}

		public void ReleaseStreams(StreamID[] AStreamIDs)
		{
			foreach (StreamID LStreamID in AStreamIDs)
				Release(LStreamID);
		}
	}
}

