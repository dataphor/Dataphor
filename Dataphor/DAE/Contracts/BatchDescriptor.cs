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
	[DataContract]
	public class BatchDescriptor
	{
		public BatchDescriptor(int AHandle, bool AIsExpression, int ALine)
		{
			FHandle = AHandle;
			FIsExpression = AIsExpression;
			FLine = ALine;
		}
		
		[DataMember]
		internal int FHandle;
		public int Handle { get { return FHandle; } }
		
		[DataMember]
		internal bool FIsExpression;
		public bool IsExpression { get { return FIsExpression; } }
		
		[DataMember]
		internal int FLine;
		public int Line { get { return FLine; } }
	}
}
