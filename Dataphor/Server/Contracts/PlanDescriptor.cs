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
		public PlanDescriptor
		(
			int AHandle,
			Guid AID,
			PlanStatistics AStatistics,
			Exception[] AMessages,
			CursorCapability ACapabilities,
			CursorType ACursorType,
			CursorIsolation ACursorIsolation,
			long ACacheTimeStamp,
			long AClientCacheTimeStamp,
			bool ACacheChanged,
			string AObjectName,
			string ACatalog
		) : base()
		{
			FHandle = AHandle;
			FID = AID;
			FStatistics = AStatistics;
			FMessages = AMessages;
			FCapabilities = ACapabilities;
			FCursorType = ACursorType;
			FCursorIsolation = ACursorIsolation;
			FCacheTimeStamp = ACacheTimeStamp;
			FClientCacheTimeStamp = AClientCacheTimeStamp;
			ACacheChanged = FCacheChanged;
			FObjectName = AObjectName;
			FCatalog = ACatalog;
		}
		
		[DataMember]
		private int FHandle;
		public int Handle { get { return FHandle; } }
		
		[DataMember]
		private Guid FID;
		public Guid ID { get { return FID; } }
		
		[DataMember]
		private PlanStatistics FStatistics;
		public PlanStatistics Statistics { get { return FStatistics; } }

		[DataMember]
		private Exception[] FMessages;
		public Exception[] Messages { get { return FMessages; } }
		
		[DataMember]
		private CursorCapability FCapabilities;
		public CursorCapability Capabilities { get { return FCapabilities; } }
		
		[DataMember]
		private CursorType FCursorType;
		public CursorType CursorType { get { return FCursorType; } }

		[DataMember]
		private CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation { get { return FCursorIsolation; } }
		
		[DataMember]
		private long FCacheTimeStamp;
		public long CacheTimeStamp { get { return FCacheTimeStamp; } }
		
		[DataMember]
		private long FClientCacheTimeStamp;
		public long ClientCacheTimeStamp { get { return FClientCacheTimeStamp; } }
		
		[DataMember]
		private bool FCacheChanged;
		public bool CacheChanged { get { return FCacheChanged; } }
		
		[DataMember]
		private string FObjectName;
		public string ObjectName { get { return FObjectName; } }
		
		[DataMember]
		private string FCatalog;
		public string Catalog { get { return FCatalog; } }
		
		[DataMember]
		private string FOrder;
		public string Order { get { return FOrder; } }
	}
}
