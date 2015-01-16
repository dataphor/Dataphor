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

	public class TopClause : Statement
	{
		protected decimal _quota;
		public decimal Quota
		{
			get { return _quota; }
			set { _quota = value; }
		}

		protected bool _isPercent;
		public bool IsPercent
		{
			get { return _isPercent; }
			set { _isPercent = value; }
		}

		protected bool _withTies;
		public bool WithTies
		{
			get { return _withTies; }
			set { _withTies = value; }
		}
	}

	public class SelectStatement : SQL.SelectStatement
	{
		// TopClause
		protected TopClause _topClause;
		public TopClause TopClause
		{
			get { return _topClause; }
			set { _topClause = value; }
		}
	}
	
	public class TableExpression : SQL.TableExpression
	{
		public TableExpression() : base(){}
		public TableExpression(string tableName) : base(tableName){}
		public TableExpression(string tableName, string optimizerHints) : base(tableName)
		{
			_optimizerHints = optimizerHints;
		}
		
		public TableExpression(string tableSchema, string tableName, string optimizerHints) : base(tableSchema, tableName)
		{
			_optimizerHints = optimizerHints;
		}

        // OptimizerHints
        protected string _optimizerHints = String.Empty;
        public virtual string OptimizerHints
        {
			get { return _optimizerHints; }
			set { _optimizerHints = value == null ? String.Empty : value; }
        }
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