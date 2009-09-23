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
		public ScriptDescriptor(int AHandle) : base()
		{
			FHandle = AHandle;
			FBatches = new List<BatchDescriptor>();
			FMessages = new ParserMessages();
		}
		
		[DataMember]
		private int FHandle;
		public int Handle { get { return FHandle; } }
		
		[DataMember]
		private List<BatchDescriptor> FBatches;
		public List<BatchDescriptor> Batches { get { return FBatches; } }
		
		[DataMember]
		private ParserMessages FMessages;
		public ParserMessages Messages { get { return FMessages; } }
	}
}
