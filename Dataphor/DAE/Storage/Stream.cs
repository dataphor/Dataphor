/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> Stream manager interface </summary>
	public interface IStreamManager
	{
		UInt64 Allocate();
		void Deallocate(UInt64 AStreamID);
		Stream Open(UInt64 AStreamID, LockMode AMode);
		void Close(UInt64 AStreamID);
	}
}
