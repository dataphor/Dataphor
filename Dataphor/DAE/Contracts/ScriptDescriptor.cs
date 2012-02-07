/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Contracts
{
	[DataContract]
	public class ScriptDescriptor
	{
		public ScriptDescriptor(int handle) : base()
		{
			_handle = handle;
			_batches = new List<BatchDescriptor>();
			_messages = new List<DataphorFault>();
		}
		
		[DataMember]
		internal int _handle;
		public int Handle { get { return _handle; } }
		
		[DataMember]
		internal List<BatchDescriptor> _batches;
		public List<BatchDescriptor> Batches { get { return _batches; } }
		
		[DataMember]
		internal List<DataphorFault> _messages;
		public List<DataphorFault> Messages { get { return _messages; } }
	}
}
