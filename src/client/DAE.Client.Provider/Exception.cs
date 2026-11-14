/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Client.Provider
{
	public class DAEProviderException : Exception
	{
		public DAEProviderException() : base() {}
		public DAEProviderException(string AMessage) : base(AMessage) {}
		public DAEProviderException(string AMessage, Exception AInner) : base(AMessage, AInner) {}
	}
}
