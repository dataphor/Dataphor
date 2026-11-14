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
		public static ListenerFault ExceptionToFault(Exception exception)
		{
			ListenerFault fault = new ListenerFault();
			
			fault.Message = exception.Message;
			if (exception.InnerException != null)
				fault.InnerFault = ExceptionToFault(exception.InnerException);
				
			return fault;
		}
		
		public static Exception FaultToException(ListenerFault fault)
		{
			return new Exception(fault.Message, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
		}
	}
}
