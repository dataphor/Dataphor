using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Server
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
}
