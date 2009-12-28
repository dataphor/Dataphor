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
        public PostgreSQLDeviceSession(PostgreSQLDevice ADevice, ServerProcess AServerProcess,
                                  DeviceSessionInfo ADeviceSessionInfo)
            : base(ADevice, AServerProcess, ADeviceSessionInfo)
        {
        }

        public new PostgreSQLDevice Device
        {
            get { return (PostgreSQLDevice)base.Device; }
        }

        protected override SQLConnection InternalCreateConnection()
        {
        	string LConnectionClassName = Device.ConnectionClass == String.Empty
        	            	?"Connection.PostgreSQLConnection": Device.ConnectionClass;
        	
			var LClassDefinition = new ClassDefinition(LConnectionClassName);

        	string LConnectionStringBuilderClassName = Device.ConnectionStringBuilderClass == String.Empty
        	               	?
								"PostgreSQLDevice.PostgreSQLConnectionStringBuilder" : Device.ConnectionStringBuilderClass;
        	
			var LBuilderClassDefinition =new ClassDefinition(LConnectionStringBuilderClassName);

            var LConnectionStringBuilder =
                (ConnectionStringBuilder)ServerProcess.CreateObject
                (
                    LBuilderClassDefinition,
                    new object[] { }
                );

            var LTags = new Tags();
            LTags.AddOrUpdate("Server", Device.Server);
            LTags.AddOrUpdate("Port", Device.Port);
            LTags.AddOrUpdate("Database", Device.Database);
            LTags.AddOrUpdate("SearchPath", Device.SearchPath);            
            if (Device.UseIntegratedSecurity)
                LTags.AddOrUpdate("Integrated Security", "true");
            else
            {
                LTags.AddOrUpdate("User Id", DeviceSessionInfo.UserName);
                LTags.AddOrUpdate("Password", DeviceSessionInfo.Password);
            }

            
            

            LTags = LConnectionStringBuilder.Map(LTags);
            Device.GetConnectionParameters(LTags, DeviceSessionInfo);
            string LConnectionString = SQLDevice.TagsToString(LTags);
            SQLConnection LConnection;
            LConnection = (SQLConnection)ServerProcess.CreateObject
                (
                LClassDefinition,
                new object[] { LConnectionString }
                );
            return LConnection;
        }
    }

    
}
