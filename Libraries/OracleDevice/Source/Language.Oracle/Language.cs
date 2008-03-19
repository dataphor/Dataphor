/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.Oracle
{
	using System;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class OuterJoinFieldExpression : SQL.QualifiedFieldExpression
	{
		public OuterJoinFieldExpression() : base(){}
		public OuterJoinFieldExpression(string AFieldName) : base(AFieldName){}
		public OuterJoinFieldExpression(string AFieldName, string ATableAlias) : base(AFieldName, ATableAlias){}
	}
	
	public class SelectExpression : SQL.SelectExpression
	{
		// OptimizerHints
		private string FOptimizerHints = String.Empty;
		public string OptimizerHints
		{
			get { return FOptimizerHints; }
			set { FOptimizerHints = value == null ? String.Empty : value; }
		}
	}
}