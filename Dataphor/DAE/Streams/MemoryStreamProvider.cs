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
	using System.Runtime.Remoting.Lifetime;
	
	public class MemoryStreamHeader : Disposable, ISponsor
	{
		public MemoryStreamHeader(StreamID AStreamID)
		{
			StreamID = AStreamID;
			Stream = new MemoryStream();
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (Stream != null)
			{
				Stream.Close();
				Stream = null;
			}

			base.Dispose(ADisposing);
		}
		
		// ISponsor
		TimeSpan ISponsor.Renewal(ILease ALease)
		{
			return TimeSpan.FromMinutes(5);
		}
		
		public StreamID StreamID;
		public Stream Stream;
	}
	
	public class MemoryStreamHeaders : Hashtable, IDisposable
	{
		public MemoryStreamHeaders() : base(){}
		
		public MemoryStreamHeader this[StreamID AStreamID] { get { return (MemoryStreamHeader)base[AStreamID]; } }
		
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
		
		protected void Dispose(bool ADisposing)
		{
			foreach (DictionaryEntry LEntry in this)
				((MemoryStreamHeader)LEntry.Value).Dispose();
			Clear();
		}

		public void Add(MemoryStreamHeader AStream)
		{
			Add(AStream.StreamID, AStream);
		}
	}
	
	public class MemoryStreamProvider : StreamProvider
	{
		protected override void Dispose(bool ADisposing)
		{
			if (FHeaders != null)
			{
				FHeaders.Dispose();
				FHeaders = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		private MemoryStreamHeaders FHeaders = new MemoryStreamHeaders();
		
		public void Create(StreamID AID)
		{
			lock (this)
			{
				FHeaders.Add(new MemoryStreamHeader(AID));
			}
		}
		
		private MemoryStreamHeader GetStreamHeader(StreamID AStreamID)
		{
			MemoryStreamHeader LHeader = FHeaders[AStreamID];
			if (LHeader == null)
				throw new StreamsException(StreamsException.Codes.StreamIDNotFound, AStreamID.ToString());
			return LHeader;
		}
		
		public override Stream Open(StreamID AStreamID)
		{
			lock (this)
			{
				MemoryStreamHeader LHeader = GetStreamHeader(AStreamID);
				return LHeader.Stream;
			}
		}
		
		public override void Close(StreamID AStreamID)
		{
			// no cleanup to perform here
		}
		
		public override void Destroy(StreamID AStreamID)
		{
			lock (this)
			{
				MemoryStreamHeader LHeader = GetStreamHeader(AStreamID);
				FHeaders.Remove(AStreamID);
				LHeader.Dispose();
			}
		}

		public override void Reassign(StreamID AOldStreamID, StreamID ANewStreamID)
		{
			lock (this)
			{
				MemoryStreamHeader LHeader = GetStreamHeader(AOldStreamID);
				FHeaders.Remove(AOldStreamID);
				LHeader.StreamID = ANewStreamID;
				FHeaders.Add(LHeader);
			}
		}
	}
}