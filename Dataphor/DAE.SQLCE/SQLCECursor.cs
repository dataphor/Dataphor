/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Data;
using System.Data.SqlServerCe;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE
{
	public class SQLCECursor : DotNetCursor
	{
		public SQLCECursor(SQLCECommand command, SqlCeResultSet resultSet) : base(command, resultSet)
		{
			_resultSet = resultSet;
		}
		
		private SqlCeResultSet _resultSet;
		public SqlCeResultSet ResultSet { get { return _resultSet; } }
	}
}
