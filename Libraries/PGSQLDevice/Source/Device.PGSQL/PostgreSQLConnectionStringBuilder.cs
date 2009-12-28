
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language.D4;


namespace Alphora.Dataphor.DAE.Device.PGSQL
{
	
	// for the PostgreSQL.NET data provider
	public class PostgreSQLConnectionStringBuilder : ConnectionStringBuilder
	{
		public PostgreSQLConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "Server");
			FLegend.AddOrUpdate("UserName", "User Id");
			FLegend.AddOrUpdate("Password", "Password");
			FLegend.AddOrUpdate("Database", "Database");
			FLegend.AddOrUpdate("SearchPath", "SearchPath");			
		}
	}

}