/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Contracts
{
	public static class ListenerFaultUtility
	{
		public static ListenerFault ExceptionToFault(Exception AException)
		{
			ListenerFault LFault = new ListenerFault();
			
			LFault.Message = AException.Message;
			if (AException.InnerException != null)
				LFault.InnerFault = ExceptionToFault(AException.InnerException);
				
			return LFault;
		}
		
		public static Exception FaultToException(ListenerFault AFault)
		{
			return new Exception(AFault.Message, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
		}
	}
}
