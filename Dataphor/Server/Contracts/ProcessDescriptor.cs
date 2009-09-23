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
	public class ProcessDescriptor
	{
		public ProcessDescriptor(int AHandle, int AID)
		{
			FHandle = AHandle;
			FID = AID;
		}
		
		[DataMember]
		private int FHandle;
		public int Handle { get { return FHandle; } }

		[DataMember]
		private int FID;
		public int ID { get { return FID; } }
	}
}
