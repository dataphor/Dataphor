/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
    [Flags]
    [DataContract]
	/// <nodoc/>
	public enum CursorGetFlags : byte {None = 0, BOF = 1, EOF = 2}
    
	public enum LogEvent
	{
		LogStarted,
		ServerStarted,
		LogStopped,
		ServerStopped
	}
	
	public enum LogEntryType
	{
		Information,
		Warning,
		Error
	}
	
	/// <summary> Library notify event for the Library notification events in the Dataphor Server </summary>	
	/// <remarks> Note that these events are only surfaced in process, and cannot be used through the remoting boundary. </remarks>
	public delegate void LibraryNotifyEvent(Engine AServer, string ALibraryName);

	/// <summary> Device notify event for the Device notification events in the Dataphor Server </summary>
	/// <remarks> Note that these events are only surfaced in process, and cannot be used through the remoting boundary. </remarks>
	public delegate void DeviceNotifyEvent(Engine AServer, Schema.Device ADevice);
		
	public class LoadingContext : System.Object
	{
		public LoadingContext(Schema.User AUser, string ALibraryName) : base()
		{
			FUser = AUser;
			FLibraryName = ALibraryName;
		}
		
		public LoadingContext(Schema.User AUser, bool AIsInternalContext)
		{
			FUser = AUser;
			FLibraryName = String.Empty;
			FIsInternalContext = AIsInternalContext;
		}
		
		public LoadingContext(Schema.User AUser, string ALibraryName, bool AIsLoadingContext)
		{
			FUser = AUser;
			FLibraryName = ALibraryName;
			FIsLoadingContext = AIsLoadingContext;
		}
		
		private Schema.User FUser;
		public Schema.User User { get { return FUser; } }
		
		private string FLibraryName;
		public string LibraryName { get { return FLibraryName; } }
		
		private bool FIsInternalContext = false;
		/// <summary>Indicates whether this is a true loading context, or an internal context entered to prevent logging of DDL.</summary>
		/// <remarks>
		/// Because loading contexts are non-logging, they are also used by the server to build internal management structures such
		/// as constraint check tables. However, these contexts may result in the creation of objects that should be logged, such
		/// as sorts for types involved in the constraints. This flag indicates that this context is an internal context and that
		/// a logging context may be pushed on top of it.
		/// </remarks>
		public bool IsInternalContext { get { return FIsInternalContext; } }
		
		private bool FIsLoadingContext = true;
		/// <summary>Indicates whether the context is a loading context, or a context pushed to enable logging within a loading context.</summary>
		/// <remarks>
		/// Pushing a non-loading context is only allowed if the current loading context is an internal context, because it should be an error
		/// to create any logged objects as a result of the creation of a deserializing object.
		/// </remarks>
		public bool IsLoadingContext { get { return FIsLoadingContext; } }
		
		internal Schema.LoadedLibrary FCurrentLibrary;
		
		internal bool FSuppressWarnings;
	}
	
	public class LoadingContexts : List<LoadingContext> { }
	
	internal class Bookmarks : Dictionary<Guid, Row>
	{
	}
	
	[DataContract]
	public class ServerFileInfo
	{
		[DataMember]
		public string LibraryName;
		[DataMember]
		public string FileName;
		[DataMember]
		public DateTime FileDate;
		[DataMember]
		public bool IsDotNetAssembly;
		[DataMember]
		public bool ShouldRegister;
	}
	
	public class ServerFileInfos : List<ServerFileInfo>
	{
		public int IndexOf(string AFileName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (this[LIndex].FileName == AFileName)
					return LIndex;
					
			return -1;
		}
		
		public bool Contains(string AFileName)
		{
			return IndexOf(AFileName) >= 0;
		}
	}
}
