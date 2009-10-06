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
	public class CursorDescriptor
	{
		public CursorDescriptor
		(
			int AHandle,
			CursorCapability ACapabilities,
			CursorType ACursorType,
			CursorIsolation ACursorIsolation
		) : base()
		{
			FHandle = AHandle;
			FCapabilities = ACapabilities;
			FCursorType = ACursorType;
			FCursorIsolation = ACursorIsolation;
		}
		
		[DataMember]
		internal int FHandle;
		public int Handle { get { return FHandle; } }

		[DataMember]
		internal CursorCapability FCapabilities;
		public CursorCapability Capabilities { get { return FCapabilities; } }
		
		[DataMember]
		internal CursorType FCursorType;
		public CursorType CursorType { get { return FCursorType; } }

		[DataMember]
		internal CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation { get { return FCursorIsolation; } }
	}
}
