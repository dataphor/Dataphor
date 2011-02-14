/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SAS
{
	using System;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class OuterJoinFieldExpression : SQL.QualifiedFieldExpression
	{
		public OuterJoinFieldExpression() : base(){}
		public OuterJoinFieldExpression(string fieldName) : base(fieldName){}
		public OuterJoinFieldExpression(string fieldName, string tableAlias) : base(fieldName, tableAlias){}
	}
}