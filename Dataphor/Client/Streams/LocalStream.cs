/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	
	/// <summary>
	/// Primary focus of the LocalStream class is to keep data in this stream once it has been read
	/// for as long as possible.  Data will be read from the source stream as necessary, and, if
	/// modified, only a call to Flush will push the data back into the source stream.
	/// The given source stream is assumed to be at position 0.
	/// </summary>
	public class LocalStream : DeferredWriteStream
	{
		public LocalStream(LocalStreamManager AStreamManager, StreamID AStreamID, DAE.Runtime.LockMode AMode) : base(null)
		{
			FStreamManager = AStreamManager;
			FStreamID = AStreamID;
			FMode = AMode;
		}

		private LocalStreamManager FStreamManager;		
		private DAE.Runtime.LockMode FMode;
		private StreamID FStreamID;
		
		protected override Stream SourceStream
		{
			get
			{
				if (FSourceStream == null)
					FSourceStream = new RemoteStreamWrapper(FStreamManager.SourceStreamManager.OpenRemote(FStreamID, FMode));
				return FSourceStream;
			}
		}
		
		public override void Close()
		{
			if (FStreamManager != null)
			{
				base.Close();
				FStreamManager.Close(FStreamID);
				FStreamManager = null;
			}
		}
	}
}
