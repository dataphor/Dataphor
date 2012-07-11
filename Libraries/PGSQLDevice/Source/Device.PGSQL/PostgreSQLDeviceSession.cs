using System;
using Alphora.Dataphor.DAE.Connection;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Server;


namespace Alphora.Dataphor.DAE.Device.PGSQL
{
       
    public class PostgreSQLDeviceSession : SQLDeviceSession
    {
        public PostgreSQLDeviceSession(PostgreSQLDevice device, ServerProcess serverProcess,
                                  DeviceSessionInfo deviceSessionInfo)
            : base(device, serverProcess, deviceSessionInfo)
        {
        }

        public new PostgreSQLDevice Device
        {
            get { return (PostgreSQLDevice)base.Device; }
        }

        protected override SQLConnection InternalCreateConnection()
        {
        	string connectionClassName = Device.ConnectionClass == String.Empty
        		? "PostgreSQLConnection.PostgreSQLConnection"
				: Device.ConnectionClass;
        	
			var classDefinition = new ClassDefinition(connectionClassName);

        	string connectionStringBuilderClassName = Device.ConnectionStringBuilderClass == String.Empty
        		? "PostgreSQLDevice.PostgreSQLConnectionStringBuilder" 
				: Device.ConnectionStringBuilderClass;
        	
			var builderClassDefinition =new ClassDefinition(connectionStringBuilderClassName);

            var connectionStringBuilder =
                (ConnectionStringBuilder)ServerProcess.CreateObject
                (
                    builderClassDefinition,
                    new object[] { }
                );

            var tags = new Tags();
            tags.AddOrUpdate("Server", Device.Server);
            tags.AddOrUpdate("Port", Device.Port);
            tags.AddOrUpdate("Database", Device.Database);
            tags.AddOrUpdate("SearchPath", Device.SearchPath);            
            tags.AddOrUpdate("User Id", DeviceSessionInfo.UserName);
            tags.AddOrUpdate("Password", DeviceSessionInfo.Password);
            

            tags = connectionStringBuilder.Map(tags);
            Device.GetConnectionParameters(tags, DeviceSessionInfo);
            string connectionString = SQLDevice.TagsToString(tags);
            var connection = (SQLConnection)ServerProcess.CreateObject
            (
                classDefinition,
                new object[] { connectionString }
            );
            return connection;
        }
    }

    
}
