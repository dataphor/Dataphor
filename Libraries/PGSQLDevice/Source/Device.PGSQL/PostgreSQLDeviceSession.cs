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

            var builderClassDefinition = new ClassDefinition(connectionStringBuilderClassName);

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

        protected override void InternalRollbackTransaction()
        {
            if (Device.UseTransactions && _transactionStarted)
            {
                for (int index = _executePool.Count - 1; index >= 0; index--)
                {
                    var header = _executePool[index];

                    if (header.DeviceCursor != null)
                        header.DeviceCursor.ReleaseConnection(header, true);

                    try
                    {
                        if (header.Connection.InTransaction)
                            header.Connection.RollbackTransaction();
                    }
                    catch
                    {
                        _transactionFailure = header.Connection.TransactionFailure;
                        // Don't rethrow, we do not care if there was an issue rolling back on the server, there's nothing we can do about it here
                    }

                    // Dispose the connection to release it back to the server
                    try
                    {
                        _executePool.DisownAt(index).Dispose();
                    }
                    catch
                    {
                        // Ignore errors disposing the connection, as long as it is removed from the execute pool, we are good
                    }
                }

                for (int index = 0; index < _executePool.Count; index++)
                {
                    if (_executePool[index].DeviceCursor != null)
                        _executePool[index].DeviceCursor.ReleaseConnection(_executePool[index], true);

                    try
                    {
                        if (_executePool[index].Connection.InTransaction)
                            _executePool[index].Connection.RollbackTransaction();
                    }
                    catch
                    {
                        _transactionFailure = _executePool[index].Connection.TransactionFailure;
                    }
                }
                _transactionStarted = false;
            }
        }
    }
}
