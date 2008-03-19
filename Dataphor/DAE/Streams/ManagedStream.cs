/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	using System.Runtime.Remoting.Lifetime;

	using Alphora.Dataphor;

	public class ManagedStream : CoverStream
	{
		public ManagedStream(ServerStreamManager AManager, int AOwnerID, StreamID AStreamID, Stream ASourceStream) : base(ASourceStream)
		{
			FStreamManager = AManager;
			FOwnerID = AOwnerID;
			FStreamID = AStreamID;
		}
		
		public override void Close()
		{
			base.Close();
			if (FStreamManager != null)
			{
				FStreamManager.Close(FOwnerID, FStreamID);
				FStreamManager = null;
			}
		}
		
		private ServerStreamManager FStreamManager;
		private int FOwnerID;
		private StreamID FStreamID;
	}
}