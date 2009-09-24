/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Language;

namespace Alphora.Dataphor.DAE.Contracts
{
	/// <nodoc/>
	[DataContract]
	public struct RemoteRowHeader
    {
		[DataMember]
		public string[] Columns;
    }
    
	/// <nodoc/>
	[DataContract]
	public struct RemoteRowBody
    {
		[DataMember]
		public byte[] Data;
    }
    
	/// <nodoc/>
	[DataContract]
	public struct RemoteRow
    {
		[DataMember]
		public RemoteRowHeader Header;

		[DataMember]
		public RemoteRowBody Body;
    }

	/// <nodoc/>
	[DataContract]
	public struct RemoteFetchData
    {
		[DataMember]
		public RemoteRowBody[] Body;

		[DataMember]
		public CursorGetFlags Flags;
	}

	/// <nodoc/>
	[DataContract]
	public struct RemoteProposeData
    {
		public bool Success;
		public RemoteRowBody Body;
    }

	/// <nodoc/>
	[DataContract]
	public struct RemoteParam
    {
		[DataMember]
		public string Name;

		[DataMember]
		public string TypeName;

		[DataMember]
		public Modifier Modifier; // hack: changed from 'Modifier' to 'byte' to fix fixup error
    }
    
	/// <nodoc/>
	[DataContract]
	public struct RemoteParamData
    {
		[DataMember]
		public RemoteParam[] Params;

		[DataMember]
		public RemoteRowBody Data;
    }

	/// <nodoc/>
	[DataContract]
	public struct RemoteMoveData
    {
		[DataMember]
		public int Count;

		[DataMember]
		public CursorGetFlags Flags;
    }
    
	/// <nodoc/>
	[DataContract]
	public struct RemoteGotoData
    {
		[DataMember]
		public bool Success;

		[DataMember]
		public CursorGetFlags Flags;
    }
}
