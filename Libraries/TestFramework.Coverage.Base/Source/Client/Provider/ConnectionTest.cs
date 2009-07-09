using System;
using System.Collections.Generic;
using System.Text;
using Alphora.Dataphor.DAE.Client.Provider;
using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Diagnostics.Client.Provider
{
    [TestFixture]
    public class ConnectionTest
    {
        
        [Test]
        public void OpenConnection()
        {
            DAEConnection daeConnection;
            string connectionString="";
            using(daeConnection = new DAEConnection(connectionString))
            {
                daeConnection.Open();
            }
        }

    }
}