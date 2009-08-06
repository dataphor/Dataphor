
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language.D4;


namespace Alphora.Dataphor.Device.PGSQL
{

	public class PostgreSQLOLEDBConnectionStringBuilder : ConnectionStringBuilder
	{
		public PostgreSQLOLEDBConnectionStringBuilder()
		{
			FParameters.AddOrUpdate("Provider", "MSDAORA");
			FLegend.AddOrUpdate("HostName", "Data source");
			FLegend.AddOrUpdate("UserName", "user id");
			FLegend.AddOrUpdate("Password", "password");
		}
	}

	// for the PostgreSQL.NET data provider
	public class PostgreSQLConnectionStringBuilder : ConnectionStringBuilder
	{
		public PostgreSQLConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "Data Source");
			FLegend.AddOrUpdate("UserName", "User Id");
		}
	}

	public class PostgreSQLODBCConnectionStringBuilder : ConnectionStringBuilder
	{
		public PostgreSQLODBCConnectionStringBuilder()
		{
			FLegend.AddOrUpdate("HostName", "DSN");
			FLegend.AddOrUpdate("UserName", "UID");
			FLegend.AddOrUpdate("Password", "PWD");
		}
	}

}