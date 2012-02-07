/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.Linter
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	using SQL = Alphora.Dataphor.DAE.Language.SQL;
	
	public class LinterTextEmitter : SQL.SQLTextEmitter
	{
		protected override void EmitCreateIndexStatement(SQL.CreateIndexStatement statement)
		{
			// literally copied from the SQL Text emitter except ...
			Indent();
			AppendFormat("{0} ", SQL.Keywords.Create);
			// removed unique keyword and check here.
			AppendFormat("{0} ", SQL.Keywords.Index);
			if (statement.IndexSchema != String.Empty)
			{
				EmitIdentifier(statement.IndexSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(statement.IndexName);
			AppendFormat(" {0} ", SQL.Keywords.On);
			if (statement.TableSchema != String.Empty)
			{
				EmitIdentifier(statement.TableSchema);
				Append(SQL.Keywords.Qualifier);
			}
			EmitIdentifier(statement.TableName);
			Append(SQL.Keywords.BeginGroup);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			Append(SQL.Keywords.EndGroup);
		}

	}
}