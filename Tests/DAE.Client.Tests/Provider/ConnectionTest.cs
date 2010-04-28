using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Client.Provider;
using NUnit.Framework;

namespace Alphora.Dataphor.DAE.ClientTests.Provider
{
    [TestFixture]
    public class ConnectionTest
    {
       

        [Test]
        public void OpenConnection()
        {
            DAEConnection LConnection;
            string connectionString = "LocalInstance";            
            using(LConnection = new DAEConnection(connectionString))
            {
                LConnection.Open();
                using (DbCommand LCommand = LConnection.CreateCommand())
                {
                    LCommand.Connection = LConnection;
                    LCommand.CommandText = "select  Libraries group add {Count() LibCount}";
                    var LCount = LCommand.ExecuteScalar();
                    Console.WriteLine(LCount.GetType());
                    Console.WriteLine(LCount);
                }
            }
        }

    }
}