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
		public static NativeCLIFault ExceptionToFault(NativeCLIException AException)
		{
			NativeCLIFault LFault = new NativeCLIFault();
			
			LFault.Code = AException.Code;
			LFault.Severity = AException.Severity;
			LFault.Message = AException.Message;
			LFault.Details = AException.Details;
			LFault.ServerContext = AException.ServerContext;
			if (AException.InnerException != null)
				LFault.InnerFault = ExceptionToFault((NativeCLIException)AException.InnerException);
				
			return LFault;
		}
		
		public static NativeCLIException FaultToException(NativeCLIFault AFault)
		{
			return new NativeCLIException(AFault.Message, AFault.Code, AFault.Severity, AFault.Details, AFault.ServerContext, AFault.InnerFault == null ? null : FaultToException(AFault.InnerFault));
		}
	}
}
