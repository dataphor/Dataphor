/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Compiling
{
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
}
