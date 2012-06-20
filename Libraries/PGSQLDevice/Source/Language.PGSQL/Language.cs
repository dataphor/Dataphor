/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Language.PGSQL
{
	public enum ForSpecifier
	{
		None,
		Update,
		Share
	}

	public class SelectExpression : SQL.SelectExpression
	{
		public ForSpecifier ForSpecifier { get; set; }
	}
				
	public class DropIndexStatement : SQL.DropIndexStatement
	{
		public DropIndexStatement() : base(){}
		
		// TableSchema
		protected string _tableSchema = String.Empty;
		public string TableSchema
		{
			get { return _tableSchema; }
			set { _tableSchema = value == null ? String.Empty : value; }
		}

		// TableName
		protected string _tableName = String.Empty;
		public string TableName
		{
			get { return _tableName; }
			set { _tableName = value == null ? String.Empty : value; }
		}
	}
}
