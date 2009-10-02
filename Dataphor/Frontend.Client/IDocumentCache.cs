/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

namespace Alphora.Dataphor.Frontend.Client
{
	public interface IDocumentCache : IDisposable
	{
		string CachePath { get; }
		Stream Freshen(string AName, uint ACRC32);
		uint GetCRC32(string AName);
		Stream Reference(string AName);
	}
}
