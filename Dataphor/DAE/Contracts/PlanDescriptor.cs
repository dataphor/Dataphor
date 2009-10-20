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
	public class PlanDescriptor
	{
		[DataMember]
		public int Handle;
		
		[DataMember]
		public Guid ID;
		
		[DataMember]
		public PlanStatistics Statistics;

		[DataMember]
		public List<DataphorFault> Messages;
		
		[DataMember]
		public CursorCapability Capabilities;
		
		[DataMember]
		public CursorType CursorType;

		[DataMember]
		public CursorIsolation CursorIsolation;
		
		[DataMember]
		public long CacheTimeStamp;
		
		[DataMember]
		public long ClientCacheTimeStamp;
		
		[DataMember]
		public bool CacheChanged;
		
		[DataMember]
		public string ObjectName;
		
		[DataMember]
		public string Catalog;
		
		[DataMember]
		public string Order;
	}
}
