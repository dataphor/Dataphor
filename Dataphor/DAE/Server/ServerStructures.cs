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
		public LoadingContext(Schema.User user, string libraryName) : base()
		{
			_user = user;
			_libraryName = libraryName;
		}
		
		public LoadingContext(Schema.User user, bool isInternalContext)
		{
			_user = user;
			_libraryName = String.Empty;
			_isInternalContext = isInternalContext;
		}
		
		public LoadingContext(Schema.User user, string libraryName, bool isLoadingContext)
		{
			_user = user;
			_libraryName = libraryName;
			_isLoadingContext = isLoadingContext;
		}
		
		private Schema.User _user;
		public Schema.User User { get { return _user; } }
		
		private string _libraryName;
		public string LibraryName { get { return _libraryName; } }
		
		private bool _isInternalContext = false;
		/// <summary>Indicates whether this is a true loading context, or an internal context entered to prevent logging of DDL.</summary>
		/// <remarks>
		/// Because loading contexts are non-logging, they are also used by the server to build internal management structures such
		/// as constraint check tables. However, these contexts may result in the creation of objects that should be logged, such
		/// as sorts for types involved in the constraints. This flag indicates that this context is an internal context and that
		/// a logging context may be pushed on top of it.
		/// </remarks>
		public bool IsInternalContext { get { return _isInternalContext; } }
		
		private bool _isLoadingContext = true;
		/// <summary>Indicates whether the context is a loading context, or a context pushed to enable logging within a loading context.</summary>
		/// <remarks>
		/// Pushing a non-loading context is only allowed if the current loading context is an internal context, because it should be an error
		/// to create any logged objects as a result of the creation of a deserializing object.
		/// </remarks>
		public bool IsLoadingContext { get { return _isLoadingContext; } }
		
		internal Schema.LoadedLibrary _currentLibrary;
		
		internal bool _suppressWarnings;
	}
	
	public class LoadingContexts : List<LoadingContext> { }
	
	internal class Bookmarks : Dictionary<Guid, IRow>
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
		public int IndexOf(string fileName)
		{
			for (int index = 0; index < Count; index++)
				if (this[index].FileName == fileName)
					return index;
					
			return -1;
		}
		
		public bool Contains(string fileName)
		{
			return IndexOf(fileName) >= 0;
		}
	}
}
