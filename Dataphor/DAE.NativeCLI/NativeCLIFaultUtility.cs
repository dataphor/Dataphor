/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	public static class NativeCLIFaultUtility
	{
		public static NativeCLIFault ExceptionToFault(NativeCLIException exception)
		{
			NativeCLIFault fault = new NativeCLIFault();
			
			fault.Code = exception.Code;
			fault.Severity = exception.Severity;
			fault.Message = exception.Message;
			fault.Details = exception.Details;
			fault.ServerContext = exception.ServerContext;
			if (exception.InnerException != null)
				fault.InnerFault = ExceptionToFault((NativeCLIException)exception.InnerException);
				
			return fault;
		}
		
		public static NativeCLIException FaultToException(NativeCLIFault fault)
		{
			return new NativeCLIException(fault.Message, fault.Code, fault.Severity, fault.Details, fault.ServerContext, fault.InnerFault == null ? null : FaultToException(fault.InnerFault));
		}
	}
}
