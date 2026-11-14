
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language.D4;


namespace Alphora.Dataphor.DAE.Device.PGSQL
{
	
	// for the PostgreSQL.NET data provider
	public class PostgreSQLConnectionStringBuilder : ConnectionStringBuilder
	{
		public PostgreSQLConnectionStringBuilder()
		{
			_legend.AddOrUpdate("HostName", "Server");
			_legend.AddOrUpdate("UserName", "User Id");
			_legend.AddOrUpdate("Password", "Password");
			_legend.AddOrUpdate("Database", "Database");
			_legend.AddOrUpdate("SearchPath", "SearchPath");			
		}
	}

}