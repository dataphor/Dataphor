/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	[DataContract]
	public class NativeCLIFault
	{
		[DataMember]
		public int Code;
		
		[DataMember]
		public ErrorSeverity Severity;
		
		[DataMember]
		public string Message;
		
		[DataMember]
		public string Details;
		
		[DataMember]
		public string ServerContext;
		
		[DataMember]
		public NativeCLIFault InnerFault;
	}
}
