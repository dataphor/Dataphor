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
    [Flags]
	/// <nodoc/>
	public enum CursorGetFlags : byte { None = 0, BOF = 1, EOF = 2 }
	
	/// <nodoc/>
	[DataContract]
    public struct CatalogResult
    {
		[DataMember]
		public string Catalog;

		[DataMember]
		public long CacheTimeStamp;

		[DataMember]
		public long ClientCacheTimeStamp;

		[DataMember]
		public bool CacheChanged;
	}

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
		[DataMember]
		public bool Success;
		
		[DataMember]
		public RemoteRowBody Body;
    }

	public enum RemoteParamModifier : byte { In, Var, Out, Const }
	
	/// <nodoc/>
	[DataContract]
	public struct RemoteParam
    {
		[DataMember]
		public string Name;

		[DataMember]
		public string TypeName;

		[DataMember]
		public RemoteParamModifier Modifier;
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
    public struct ExecuteResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
    }

    /// <nodoc/>
	[DataContract]
    public struct EvaluateResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public byte[] Result;
    }

    /// <nodoc/>
	[DataContract]
    public struct DirectEvaluateResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public byte[] Result;
		
		[DataMember]
		public PlanDescriptor PlanDescriptor;
    }

    /// <nodoc/>
	[DataContract]
    public struct CursorResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public CursorDescriptor CursorDescriptor;
    }

    /// <nodoc/>
	[DataContract]
    public struct DirectCursorResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public PlanDescriptor PlanDescriptor;
		
		[DataMember]
		public CursorDescriptor CursorDescriptor;
    }

    /// <nodoc/>
	[DataContract]
    public struct CursorWithFetchResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public CursorDescriptor CursorDescriptor;
		
		[DataMember]
		public Guid[] Bookmarks;
		
		[DataMember]
		public RemoteFetchData FetchData;
    }
    
    /// <nodoc/>
	[DataContract]
    public struct DirectCursorWithFetchResult
    {
		[DataMember]
		public ProgramStatistics ExecuteTime;
		
		[DataMember]
		public RemoteRowBody ParamData;
		
		[DataMember]
		public PlanDescriptor PlanDescriptor;
		
		[DataMember]
		public CursorDescriptor CursorDescriptor;
		
		[DataMember]
		public Guid[] Bookmarks;
		
		[DataMember]
		public RemoteFetchData FetchData;
    }
    
    /// <nodoc/>
	[DataContract]
    public struct FetchResult
    {
		[DataMember]
		public Guid[] Bookmarks;
		
		[DataMember]
		public RemoteFetchData FetchData;
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
