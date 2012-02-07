/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

using NUnit.Framework;

namespace Alphora.Dataphor.DAE.Client.Tests.Provider
{
	using Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Client.Provider;

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