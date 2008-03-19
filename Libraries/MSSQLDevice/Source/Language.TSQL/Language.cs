/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.TSQL
{
	using System;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class TableExpression : SQL.TableExpression
	{
		public TableExpression() : base(){}
		public TableExpression(string ATableName) : base(ATableName){}
		public TableExpression(string ATableName, string AOptimizerHints) : base(ATableName)
		{
			FOptimizerHints = AOptimizerHints;
		}
		
		public TableExpression(string ATableSchema, string ATableName, string AOptimizerHints) : base(ATableSchema, ATableName)
		{
			FOptimizerHints = AOptimizerHints;
		}

        // OptimizerHints
        protected string FOptimizerHints = String.Empty;
        public virtual string OptimizerHints
        {
			get { return FOptimizerHints; }
			set { FOptimizerHints = value == null ? String.Empty : value; }
        }
	}
	
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