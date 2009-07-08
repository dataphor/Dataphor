/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Text;
using System.Collections.Generic;

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
	public delegate void LibraryNotifyEvent(Server AServer, string ALibraryName);

	/// <summary> Device notify event for the Device notification events in the Dataphor Server </summary>
	/// <remarks> Note that these events are only surfaced in process, and cannot be used through the remoting boundary. </remarks>
	public delegate void DeviceNotifyEvent(Server AServer, Schema.Device ADevice);
		
	public enum ServerType 
	{ 
		/// <summary>Standard Dataphor Server, the default behavior is used.</summary>
		Standard, 
	
		/// <summary>Embedded Dataphor Server, the server is intended to be used as a single-user data access engine embedded within a client.</summary>
		Embedded, 
	
		/// <summary>Repository, the server is intended to be used as a client-side catalog repository for a client connecting to a remote server.</summary>
		Repository 
	}

	public class CursorContext : System.Object
	{
		public CursorContext() : base() {}
		public CursorContext(CursorType ACursorType, CursorCapability ACapabilities, CursorIsolation AIsolation) : base()
		{
			FCursorType = ACursorType;
			FCursorCapabilities = ACapabilities;
			FCursorIsolation = AIsolation;
		}
		// CursorType
		private CursorType FCursorType;
		public CursorType CursorType
		{
			get { return FCursorType; }
			set { FCursorType = value; }
		}
		
		// CursorCapabilities
		private CursorCapability FCursorCapabilities;
		public CursorCapability CursorCapabilities
		{
			get { return FCursorCapabilities; }
			set { FCursorCapabilities = value; }
		}
		
		// CursorIsolation
		private CursorIsolation FCursorIsolation;
		public CursorIsolation CursorIsolation
		{
			get { return FCursorIsolation; }
			set { FCursorIsolation = value; }
		}
	}
	
	public class CursorContexts : List<CursorContext> { }

	public enum StatementType { Select, Insert, Update, Delete, Assignment }
	
	public class StatementContext : System.Object
	{
		public StatementContext(StatementType AStatementType) : base()
		{
			FStatementType = AStatementType;
		}
		
		private StatementType FStatementType;
		public StatementType StatementType { get { return FStatementType; } }
	}
	
	public class StatementContexts : List<StatementContext> { }

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
	
	public class SecurityContext : System.Object
	{
		public SecurityContext(Schema.User AUser) : base()
		{
			FUser = AUser;
		}
		
		private Schema.User FUser;
		public Schema.User User { get { return FUser; } }
		internal void SetUser(Schema.User AUser)
		{
			FUser = AUser;
		}
	}
	
	public class SecurityContexts : List<SecurityContext> { }

	internal class Bookmarks : System.Collections.Hashtable
	{
		public Row this[Guid AKey] { get { return (Row)base[AKey]; } }
	}
}
