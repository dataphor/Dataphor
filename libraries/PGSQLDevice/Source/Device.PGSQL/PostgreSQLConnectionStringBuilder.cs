
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Language.D4;


namespace Alphora.Dataphor.DAE.Device.PGSQL
{
	
	// for the PostgreSQL.NET data provider
	// NOTE: only three of the five entries below ever fire, because PostgreSQLDeviceSession.InternalCreateConnection
	// names its tags with the driver's own keywords rather than with abstract ones: it writes Server, Port, Database,
	// SearchPath, User Id and Password, and ConnectionStringBuilder.Map passes a tag the legend does not know through
	// unchanged. So Database, Password and SearchPath map to themselves (changing one of those values does change the
	// connection string the device builds), while HostName and UserName match nothing and are dead. That is the
	// opposite of the MSSQL device, whose session writes ServerName/DatabaseName/UserName and relies on its legend to
	// translate them. Do not "tidy" either half into agreement: a deployment may name its own builder through the
	// device's ConnectionStringBuilderClass attribute, and such a builder - written against 3.1, where the session did
	// the same thing - has to key on the driver keywords this session actually produces. The string that comes out is
	// pinned by tests/goldens/devices/PGSQL/Connection.sql.
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