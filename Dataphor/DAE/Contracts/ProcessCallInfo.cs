/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.Contracts
{
    /// <nodoc/>
	[DataContract]
    public struct ProcessCallInfo
    {
		[DataMember]
		public IsolationLevel[] TransactionList;
		
		public static ProcessCallInfo Empty
		{
			get
			{
				ProcessCallInfo LInfo = new ProcessCallInfo();
				LInfo.TransactionList = new IsolationLevel[0];
				return LInfo;
			}
		}
    }
}
