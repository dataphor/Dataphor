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
		public LocalStream(LocalStreamManager streamManager, StreamID streamID, DAE.Runtime.LockMode mode) : base(null)
		{
			_streamManager = streamManager;
			_streamID = streamID;
			_mode = mode;
		}

		private LocalStreamManager _streamManager;		
		private DAE.Runtime.LockMode _mode;
		private StreamID _streamID;
		
		protected override Stream SourceStream
		{
			get
			{
				if (_sourceStream == null)
					_sourceStream = new RemoteStreamWrapper(_streamManager.SourceStreamManager.OpenRemote(_streamID, _mode));
				return _sourceStream;
			}
		}
		
		public override void Close()
		{
			if (_streamManager != null)
			{
				base.Close();
				_streamManager.Close(_streamID);
				_streamManager = null;
			}
		}
	}
}
