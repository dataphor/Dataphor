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
		public ProcessDescriptor(int handle, int iD)
		{
			_handle = handle;
			_iD = iD;
		}
		
		[DataMember]
		internal int _handle;
		public int Handle { get { return _handle; } }

		[DataMember]
		internal int _iD;
		public int ID { get { return _iD; } }
	}
}
