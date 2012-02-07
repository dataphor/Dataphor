/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;

	using Alphora.Dataphor;

	public class ManagedStream : CoverStream
	{
		public ManagedStream(ServerStreamManager manager, int ownerID, StreamID streamID, Stream sourceStream) : base(sourceStream)
		{
			_streamManager = manager;
			_ownerID = ownerID;
			_streamID = streamID;
		}
		
		public override void Close()
		{
			base.Close();
			if (_streamManager != null)
			{
				_streamManager.Close(_ownerID, _streamID);
				_streamManager = null;
			}
		}
		
		private ServerStreamManager _streamManager;
		private int _ownerID;
		private StreamID _streamID;
	}
}