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
		private int FHandle;
		public int Handle { get { return FHandle; } set { FHandle = value; } }
		
		[DataMember]
		private Guid FID;
		public Guid ID { get { return FID; } set { FID = value; } }
		
		[DataMember]
		private PlanStatistics FStatistics;
		public PlanStatistics Statistics { get { return FStatistics; } set { FStatistics = value; } }

		[DataMember]
		private Exception[] FMessages;
		public Exception[] Messages { get { return FMessages; } set { FMessages = value; } }
		
		[DataMember]
		private CursorCapability FCapabilities;
		public CursorCapability Capabilities { get { return FCapabilities; } set { FCapabilities = value; } }
		
		[DataMember]
		private CursorType FCursorType;
		public CursorType CursorType { get { return FCursorType; } set { FCursorType = value; } }

		[DataMember]
		private CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation { get { return FCursorIsolation; } set { FCursorIsolation = value; } }
		
		[DataMember]
		private long FCacheTimeStamp;
		public long CacheTimeStamp { get { return FCacheTimeStamp; } set { FCacheTimeStamp = value; } }
		
		[DataMember]
		private long FClientCacheTimeStamp;
		public long ClientCacheTimeStamp { get { return FClientCacheTimeStamp; } set { FClientCacheTimeStamp = value; } }
		
		[DataMember]
		private bool FCacheChanged;
		public bool CacheChanged { get { return FCacheChanged; } set { FCacheChanged = value; } }
		
		[DataMember]
		private string FObjectName;
		public string ObjectName { get { return FObjectName; } set { FObjectName = value; } }
		
		[DataMember]
		private string FCatalog;
		public string Catalog { get { return FCatalog; } set { FCatalog = value; } }
		
		[DataMember]
		private string FOrder;
		public string Order { get { return FOrder; } set { FOrder = value; } }
	}
}
