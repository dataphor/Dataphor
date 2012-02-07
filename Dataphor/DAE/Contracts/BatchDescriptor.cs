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
		public BatchDescriptor(int handle, bool isExpression, int line)
		{
			_handle = handle;
			_isExpression = isExpression;
			_line = line;
		}
		
		[DataMember]
		internal int _handle;
		public int Handle { get { return _handle; } }
		
		[DataMember]
		internal bool _isExpression;
		public bool IsExpression { get { return _isExpression; } }
		
		[DataMember]
		internal int _line;
		public int Line { get { return _line; } }
	}
}
