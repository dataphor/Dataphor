/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Language.PGSQL
{
			
	public class DropIndexStatement : SQL.DropIndexStatement
	{
		public DropIndexStatement() : base(){}
		
		// TableSchema
		protected string FTableSchema = String.Empty;
		public string TableSchema
		{
			get { return FTableSchema; }
			set { FTableSchema = value == null ? String.Empty : value; }
		}

		// TableName
		protected string FTableName = String.Empty;
		public string TableName
		{
			get { return FTableName; }
			set { FTableName = value == null ? String.Empty : value; }
		}
	}
}