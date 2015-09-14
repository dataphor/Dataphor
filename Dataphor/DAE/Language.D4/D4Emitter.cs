/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.D4
{
	using System;
	using System.Text;
	
	using Alphora.Dataphor.DAE.Language;
	
	public class D4TextEmitter : BasicTextEmitter
	{
		public D4TextEmitter() : base()
		{
			_emitMode = EmitMode.ForCopy;
		}
		
		public D4TextEmitter(EmitMode emitMode) : base()
		{
			_emitMode = emitMode;
		}
		
		private EmitMode _emitMode;
		public EmitMode EmitMode
		{
			get { return _emitMode; }
			set { _emitMode = value; }
		}
		
		private void EmitTags(Tags tags, bool staticValue)
		{
			if (tags.Count > 2)
			{
				NewLine();
				IncreaseIndent();
				Indent();
				if (staticValue)
					AppendFormat("{0} ", Keywords.Static);
				Append(Keywords.Tags);
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.BeginList);
				NewLine();
				IncreaseIndent();
				Indent();
			}
			else
			{
				if (staticValue)
					AppendFormat(" {0}", Keywords.Static);
				AppendFormat(" {0} {1} ", Keywords.Tags, Keywords.BeginList);
			}
			
			bool first = true;
			#if USEHASHTABLEFORTAGS
			foreach (Tag tag in ATags)
			{
			#else
			Tag tag;
			for (int index = 0; index < tags.Count; index++)
			{
				tag = tags[index];
			#endif
				if (!first)
					EmitListSeparator();
				else
					first = false;
				
				if (tags.Count > 2)
				{
					NewLine();
					Indent();
				}
				AppendFormat(@"{0} {1} ""{2}""", tag.Name, Keywords.Equal, tag.Value.Replace(@"""", @"""""")); 
			}
			if (tags.Count > 2)
			{
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
				DecreaseIndent();
			}
			else
				AppendFormat(" {0}", Keywords.EndList);
		}

		protected virtual void EmitMetaData(MetaData metaData)
		{
			if (metaData != null)
			{
				// Emit dynamic tags
				Tags tags = new Tags();
				#if USEHASHTABLEFORTAGS
				foreach (Tag tag in AMetaData.Tags)
				{
				#else
				Tag tag;
				for (int index = 0; index < metaData.Tags.Count; index++)
				{
					tag = metaData.Tags[index];
				#endif
					if ((!tag.IsStatic) && ((_emitMode == EmitMode.ForRemote) || !(/*AMetaData.Tags.IsReference(LTag.Name) || */tag.IsInherited)))
						tags.Add(tag);
				}
				
				if (tags.Count > 0)
					EmitTags(tags, false);
				
				// Emit static tags
				tags = new Tags();
				#if USEHASHTABLEFORTAGS
				foreach (Tag tag2 in AMetaData.Tags)
				{
				#else
				for (int index = 0; index < metaData.Tags.Count; index++)
				{
					tag = metaData.Tags[index];
				#endif
					if (tag.IsStatic)
						tags.Add(tag);
				}
				
				if (tags.Count > 0)
					EmitTags(tags, true);
			}
		}
		
		protected virtual void EmitAlterMetaData(AlterMetaData alterMetaData)
		{
			if (alterMetaData != null)
			{
				Tags createTags = alterMetaData.CreateTags;
				Tags alterTags = alterMetaData.AlterTags;
				Tags dropTags = alterMetaData.DropTags;
				if (((createTags.Count > 0) || (alterTags.Count > 0) || (dropTags.Count > 0)))
				{
					AppendFormat(" {0} {1} {2} ", Keywords.Alter, Keywords.Tags, Keywords.BeginList);
					bool first = true;
					#if USEHASHTABLEFORTAGS
					foreach (Tag tag in createTags)
					{
					#else
					Tag tag;
					for (int index = 0; index < createTags.Count; index++)
					{
						tag = createTags[index];
					#endif
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat(@"{0} {1} {2} {3} ""{4}""", new object[]{Keywords.Create, tag.IsStatic ? Keywords.Static : Keywords.Dynamic, tag.Name, Keywords.Equal, tag.Value.Replace(@"""", @"""""")});
					}
					
					#if USEHASHTABLEFORTAGS
					foreach (Tag tag in alterTags)
					{
					#else
					for (int index = 0; index < alterTags.Count; index++)
					{
						tag = alterTags[index];
					#endif
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat(@"{0} {1} {2} {3} ""{4}""", new object[]{Keywords.Alter, tag.IsStatic ? Keywords.Static : Keywords.Dynamic, tag.Name, Keywords.Equal, tag.Value.Replace(@"""", @"""""")});
					}
					
					#if USEHASHTABLEFORTAGS
					foreach (Tag tag in dropTags)
					{
					#else
					for (int index = 0; index < dropTags.Count; index++)
					{
						tag = dropTags[index];
					#endif
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat("{0} {1}", Keywords.Drop, tag.Name);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitLanguageModifiers(Statement statement)
		{
			if ((statement.Modifiers != null) && (statement.Modifiers.Count > 0))
			{
				AppendFormat(" {0} {1} ", D4.Keywords.With, D4.Keywords.BeginList);
				for (int index = 0; index < statement.Modifiers.Count; index++)
				{
					if (index > 0)
						AppendFormat("{0} ", D4.Keywords.ListSeparator);
					AppendFormat(@"{0} {1} ""{2}""", statement.Modifiers[index].Name, D4.Keywords.Equal, statement.Modifiers[index].Value);
				}
				AppendFormat(" {0}", D4.Keywords.EndList);
			}
		}
		
		protected override void EmitExpression(Expression expression)
		{
			if ((expression.Modifiers != null) && (expression.Modifiers.Count > 0))
				Append(Keywords.BeginGroup);

			bool emitModifiers = true;
			if (expression is QualifierExpression)
				EmitQualifierExpression((QualifierExpression)expression);
			else if (expression is AsExpression)
				EmitAsExpression((AsExpression)expression);
			else if (expression is IsExpression)
				EmitIsExpression((IsExpression)expression);
			#if CALCULESQUE
			else if (AExpression is NamedExpression)
				EmitNamedExpression((NamedExpression)AExpression);
			#endif
			else if (expression is ParameterExpression)
				EmitParameterExpression((ParameterExpression)expression);
			else if (expression is D4IndexerExpression)
				EmitIndexerExpression((D4IndexerExpression)expression);
			else if (expression is IfExpression)
				EmitIfExpression((IfExpression)expression);
			else if (expression is RowSelectorExpressionBase)
				EmitRowSelectorExpressionBase((RowSelectorExpressionBase)expression);
			else if (expression is RowExtractorExpressionBase)
				EmitRowExtractorExpressionBase((RowExtractorExpressionBase)expression);
			else if (expression is ColumnExtractorExpression)
				EmitColumnExtractorExpression((ColumnExtractorExpression)expression);
			else if (expression is ListSelectorExpression)
				EmitListSelectorExpression((ListSelectorExpression)expression);
			else if (expression is CursorSelectorExpression)
				EmitCursorSelectorExpression((CursorSelectorExpression)expression);
			else if (expression is CursorDefinition)
				EmitCursorDefinition((CursorDefinition)expression);
			else if (expression is ExplodeColumnExpression)
				EmitExplodeColumnExpression((ExplodeColumnExpression)expression);
			else if (expression is AdornExpression)
			{
				EmitAdornExpression((AdornExpression)expression);
				emitModifiers = false;
			}
			else if (expression is RedefineExpression)
			{
				EmitRedefineExpression((RedefineExpression)expression);
				emitModifiers = false;
			}
			else if (expression is OnExpression)
			{
				EmitOnExpression((OnExpression)expression);
				emitModifiers = false;
			}
			else if (expression is RenameAllExpression)
			{
				EmitRenameAllExpression((RenameAllExpression)expression);
				emitModifiers = false;
			}
			else if (expression is TableSelectorExpressionBase)
				EmitTableSelectorExpressionBase((TableSelectorExpressionBase)expression);
			else if (expression is RestrictExpression)
			{
				EmitRestrictExpression((RestrictExpression)expression);
				emitModifiers = false;
			}
			else if (expression is ProjectExpression)
			{
				EmitProjectExpression((ProjectExpression)expression);
				emitModifiers = false;
			}
			else if (expression is RemoveExpression)
			{
				EmitRemoveExpression((RemoveExpression)expression);
				emitModifiers = false;
			}
			else if (expression is ExtendExpression)
			{
				EmitExtendExpression((ExtendExpression)expression);
				emitModifiers = false;
			}
			else if (expression is SpecifyExpression)
			{
				EmitSpecifyExpression((SpecifyExpression)expression);
				emitModifiers = false;
			}
			else if (expression is RenameExpression)
			{
				EmitRenameExpression((RenameExpression)expression);
				emitModifiers = false;
			}
			else if (expression is AggregateExpression)
			{
				EmitAggregateExpression((AggregateExpression)expression);
				emitModifiers = false;
			}
			else if (expression is BaseOrderExpression)
				EmitBaseOrderExpression((BaseOrderExpression)expression);
			else if (expression is QuotaExpression)
			{
				EmitQuotaExpression((QuotaExpression)expression);
				emitModifiers = false;
			}
			else if (expression is ExplodeExpression)
			{
				EmitExplodeExpression((ExplodeExpression)expression);
				emitModifiers = false;
			}
			else if (expression is BinaryTableExpression)
			{
				EmitBinaryTableExpression((BinaryTableExpression)expression);
				emitModifiers = false;
			}
			else
				base.EmitExpression(expression);

			if (emitModifiers)
				EmitLanguageModifiers(expression);

			if ((expression.Modifiers != null) && (expression.Modifiers.Count > 0))
				Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitQualifierExpression(QualifierExpression expression)
		{
			EmitExpression(expression.LeftExpression);
			Append(Keywords.Qualifier);
			EmitExpression(expression.RightExpression);
		}
		
		protected virtual void EmitParameterExpression(ParameterExpression expression)
		{
			if (expression.Modifier == Modifier.Var)
				AppendFormat("{0} ", Keywords.Var);
			EmitExpression(expression.Expression);
		}
		
		protected virtual void EmitIndexerExpression(D4IndexerExpression expression)
		{
			EmitExpression(expression.Expression);
			Append(Keywords.BeginIndexer);
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitExpression(expression.Expressions[index]);
			}
			
			if (expression.HasByClause)
			{
				AppendFormat(" {0} {1} ", Keywords.By, Keywords.BeginList);
				for (int index = 0; index < expression.ByClause.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					EmitKeyColumnDefinition(expression.ByClause[index]);
				}
				if (expression.ByClause.Count > 0)
					AppendFormat(" {0}", Keywords.EndList);
				else
					AppendFormat("{0}", Keywords.EndList);
			}
			
			Append(Keywords.EndIndexer);
		}
		
		protected virtual void EmitIfExpression(IfExpression expression)
		{
			Append(Keywords.BeginGroup);
			AppendFormat("{0} ", Keywords.If);
			EmitExpression(expression.Expression);
			AppendFormat(" {0} ", Keywords.Then);
			EmitExpression(expression.TrueExpression);
			AppendFormat(" {0} ", Keywords.Else);
			EmitExpression(expression.FalseExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAdornExpression(AdornExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Adorn);
			NewLine();
			Indent();
			if 
			(
				(expression.Expressions.Count > 0) || 
				(expression.Constraints.Count > 0) || 
				(expression.Orders.Count > 0) || 
				(expression.AlterOrders.Count > 0) ||
				(expression.DropOrders.Count > 0) ||
				(expression.Keys.Count > 0) || 
				(expression.AlterKeys.Count > 0) ||
				(expression.DropKeys.Count > 0) ||
				(expression.References.Count > 0) ||
				(expression.AlterReferences.Count > 0) ||
				(expression.DropReferences.Count > 0)
			)
			{
				Append(Keywords.BeginList);
				IncreaseIndent();																   
				bool first = true;
				for (int index = 0; index < expression.Expressions.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAdornColumnExpression(expression.Expressions[index]);
				}
				for (int index = 0; index < expression.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(expression.Constraints[index]);
				}
				for (int index = 0; index < expression.Orders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitOrderDefinition(expression.Orders[index]);
				}
				for (int index = 0; index < expression.AlterOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(expression.AlterOrders[index]);
				}
				for (int index = 0; index < expression.DropOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(expression.DropOrders[index]);
				}
				for (int index = 0; index < expression.Keys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitKeyDefinition(expression.Keys[index]);
				}
				for (int index = 0; index < expression.AlterKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(expression.AlterKeys[index]);
				}
				for (int index = 0; index < expression.DropKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(expression.DropKeys[index]);
				}
				for (int index = 0; index < expression.References.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(expression.References[index]);
				}
				for (int index = 0; index < expression.AlterReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(expression.AlterReferences[index]);
				}
				for (int index = 0; index < expression.DropReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(expression.DropReferences[index]);
				}
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(expression.MetaData);
			EmitAlterMetaData(expression.AlterMetaData);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAdornColumnExpression(AdornColumnExpression expression)
		{
			Append(expression.ColumnName);
			
			if (expression.ChangeNilable)
			{
				if (expression.IsNilable)
					AppendFormat(" {0}", Keywords.Nil);
				else
					AppendFormat(" {0} {1}", Keywords.Not, Keywords.Nil);
			}

			bool first = true;			
			if ((expression.Default != null) || (expression.Constraints.Count > 0))
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				
				if (expression.Default != null)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;

					NewLine();
					Indent();
					EmitDefaultDefinition(expression.Default);
				}
					
				for (int index = 0; index < expression.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(expression.Constraints[index]);
				}
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(expression.MetaData);
			EmitAlterMetaData(expression.AlterMetaData);
		}
		
		protected virtual void EmitRedefineExpression(RedefineExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Redefine);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitRedefineColumnExpression(expression.Expressions[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRedefineColumnExpression(NamedColumnExpression expression)
		{
			AppendFormat("{0} {1} ", expression.ColumnAlias, Keywords.Assign);
			EmitExpression(expression.Expression);
		}
		
		protected virtual void EmitOnExpression(OnExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.On, expression.ServerName);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRenameAllExpression(RenameAllExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Rename, expression.Identifier);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitIsExpression(IsExpression expression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(expression.Expression);
			AppendFormat(" {0} ", Keywords.Is);
			EmitTypeSpecifier(expression.TypeSpecifier);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAsExpression(AsExpression expression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(expression.Expression);
			AppendFormat(" {0} ", Keywords.As);
			EmitTypeSpecifier(expression.TypeSpecifier);
			Append(Keywords.EndGroup);
		}
		
		#if CALCULESQUE
		protected virtual void EmitNamedExpression(NamedExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0}", AExpression.Name);
		}
		#endif
		
		protected virtual void EmitTableSelectorExpressionBase(TableSelectorExpressionBase expression)
		{
			Append(Keywords.Table);
			if (expression.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.Of);
				if (expression.TypeSpecifier is TableTypeSpecifier)
				{
					TableTypeSpecifier typeSpecifier = (TableTypeSpecifier)expression.TypeSpecifier;
					AppendFormat("{0} ", Keywords.BeginList);
					for (int index = 0; index < typeSpecifier.Columns.Count; index++)
					{
						if (index > 0)
							EmitListSeparator();
						EmitNamedTypeSpecifier(typeSpecifier.Columns[index]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
				else
					EmitTypeSpecifier(expression.TypeSpecifier);
			}
			
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitExpression(expression.Expressions[index]);
			}
			
			for (int index = 0; index < expression.Keys.Count; index++)
			{
				if ((expression.Expressions.Count > 0) || (index > 0))
					EmitListSeparator();
				NewLine();
				Indent();
				EmitKeyDefinition(expression.Keys[index]);
			}

			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
		}
		
		protected virtual void EmitRowSelectorExpressionBase(RowSelectorExpressionBase expression)
		{
			Append(Keywords.Row);

			if (expression.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.Of);
				if (expression.TypeSpecifier is RowTypeSpecifier)
				{
					RowTypeSpecifier typeSpecifier = (RowTypeSpecifier)expression.TypeSpecifier;
					AppendFormat("{0} ", Keywords.BeginList);
					for (int index = 0; index < typeSpecifier.Columns.Count; index++)
					{
						if (index > 0)
							EmitListSeparator();
						EmitNamedTypeSpecifier(typeSpecifier.Columns[index]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
				else
					EmitTypeSpecifier(expression.TypeSpecifier);
			}
			
			AppendFormat(" {0} ", Keywords.BeginList);
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitNamedColumnExpression(expression.Expressions[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitRowExtractorExpressionBase(RowExtractorExpressionBase expression)
		{
			Append(Keywords.BeginGroup);
			AppendFormat("{0} {1} ", Keywords.Row, Keywords.From);
			EmitExpression(expression.Expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnExtractorExpression(ColumnExtractorExpression expression)
		{
			Append(Keywords.BeginGroup);
			if (expression.Columns.Count == 1)
				AppendFormat("{0} {1} ", expression.Columns[0].ColumnName, Keywords.From);
			else
			{
				AppendFormat("{0} ", Keywords.BeginList);
				for (int index = 0; index < expression.Columns.Count; index++)
				{
					if (index > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					Append(expression.Columns[index].ColumnName);
				}
				AppendFormat(" {0} {1} ", Keywords.EndList, Keywords.From);
			}
			EmitExpression(expression.Expression);
			
			if (expression.HasByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int index = 0; index < expression.OrderColumns.Count; index++)
				{
					if (index > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(expression.OrderColumns[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitListSelectorExpression(ListSelectorExpression expression)
		{
			if (expression.TypeSpecifier != null)
				EmitListTypeSpecifier((ListTypeSpecifier)expression.TypeSpecifier);
			AppendFormat("{0} ", Keywords.BeginList);
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitExpression(expression.Expressions[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitCursorDefinition(CursorDefinition expression)
		{
			EmitExpression(expression.Expression);
			if (expression.Capabilities != 0)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Capabilities, Keywords.BeginList);
				bool first = true;
				CursorCapability capability;
				for (int index = 0; index < 7; index++)
				{
					capability = (CursorCapability)Math.Pow(2, index);
					if ((expression.Capabilities & capability) != 0)
					{
						if (!first)
							EmitListSeparator();
						else
							first = false;
							
						Append(capability.ToString().ToLower());
					}
				}
				AppendFormat(" {0}", Keywords.EndList);
			}

			if (expression.Isolation != CursorIsolation.None)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1}", Keywords.Isolation, expression.Isolation.ToString().ToLower());
			}
			
			if (expression.SpecifiesType)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1}", Keywords.Type, expression.CursorType.ToString().ToLower());
			}
		}
		
		protected virtual void EmitCursorSelectorExpression(CursorSelectorExpression expression)
		{
			Append(Keywords.Cursor);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			EmitCursorDefinition(expression.CursorDefinition);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRestrictExpression(RestrictExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Where);
			EmitExpression(expression.Condition);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnExpression(ColumnExpression expression)
		{
			Append(expression.ColumnName);
		}
		
		protected virtual void EmitProjectExpression(ProjectExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} ", Keywords.Over, Keywords.BeginList);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitColumnExpression(expression.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRemoveExpression(RemoveExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} ", Keywords.Remove, Keywords.BeginList);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitColumnExpression(expression.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitNamedColumnExpression(NamedColumnExpression expression)
		{
			EmitExpression(expression.Expression);
			AppendFormat(" {0}", expression.ColumnAlias);
			EmitMetaData(expression.MetaData);
		}
		
		protected virtual void EmitExtendExpression(ExtendExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Add);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitNamedColumnExpression(expression.Expressions[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitSpecifyExpression(SpecifyExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitNamedColumnExpression(expression.Expressions[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRenameColumnExpression(RenameColumnExpression expression)
		{
			AppendFormat("{0} {1}", expression.ColumnName, expression.ColumnAlias);
			EmitMetaData(expression.MetaData);
		}
		
		protected virtual void EmitRenameExpression(RenameExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Rename);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < expression.Expressions.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitRenameColumnExpression(expression.Expressions[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAggregateColumnExpression(AggregateColumnExpression expression)
		{
			AppendFormat("{0}{1}", expression.AggregateOperator, Keywords.BeginGroup);
			if (expression.Distinct)
				AppendFormat("{0} ", Keywords.Distinct);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					AppendFormat("{0} ", Keywords.ListSeparator);
				Append(expression.Columns[index].ColumnName);
			}

			if (expression.HasByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int index = 0; index < expression.OrderColumns.Count; index++)
				{
					if (index > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(expression.OrderColumns[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			AppendFormat("{0} {1}", Keywords.EndGroup, expression.ColumnAlias);
			EmitMetaData(expression.MetaData);
		}
		
		protected virtual void EmitAggregateExpression(AggregateExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0}", Keywords.Group);
			IncreaseIndent();
			if (expression.ByColumns.Count > 0)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.By, Keywords.BeginList);
				for (int index = 0; index < expression.ByColumns.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					EmitColumnExpression(expression.ByColumns[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			NewLine();
			Indent();
			Append(Keywords.Add);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < expression.ComputeColumns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitAggregateColumnExpression(expression.ComputeColumns[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitBaseOrderExpression(BaseOrderExpression expression)
		{
			if (expression is OrderExpression)
				EmitOrderExpression((OrderExpression)expression);
			else if (expression is BrowseExpression)
				EmitBrowseExpression((BrowseExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
		}
		
		protected virtual void EmitOrderExpression(OrderExpression expression)
		{
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(expression.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitIncludeColumnExpression(expression.SequenceColumn, Schema.TableVarColumnType.Sequence);
			DecreaseIndent();
		}
		
		protected virtual void EmitBrowseExpression(BrowseExpression expression)
		{
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} {2} ", Keywords.Browse, Keywords.By, Keywords.BeginList);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(expression.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
		}

		protected virtual void EmitQuotaExpression(QuotaExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Return);
			EmitExpression(expression.Quota);
			AppendFormat(" {0} {1} ", Keywords.By, Keywords.BeginList);
			for (int index = 0; index < expression.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(expression.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitExplodeColumnExpression(ExplodeColumnExpression expression)
		{
			AppendFormat("{0}{1}{2}", Keywords.Parent, Keywords.Qualifier, expression.ColumnName);
		}
		
		protected virtual void EmitIncludeColumnExpression(IncludeColumnExpression expression, Schema.TableVarColumnType columnType)
		{
			if (expression != null)
			{
				AppendFormat(" {0}", Keywords.Include);
				switch (columnType)
				{
					case Schema.TableVarColumnType.Sequence: AppendFormat(" {0}", Keywords.Sequence); break;
					case Schema.TableVarColumnType.Level: AppendFormat(" {0}", Keywords.Level); break;
					case Schema.TableVarColumnType.RowExists: AppendFormat(" {0}", Keywords.RowExists); break;
				}
				if (expression.ColumnAlias != String.Empty)
					AppendFormat(" {0}", expression.ColumnAlias);
				EmitMetaData(expression.MetaData);
			}
		}
		
		protected virtual void EmitExplodeExpression(ExplodeExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Explode);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.By);
			EmitExpression(expression.ByExpression);
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Where);
			EmitExpression(expression.RootExpression);

			if (expression.HasOrderByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int index = 0; index < expression.OrderColumns.Count; index++)
				{
					if (index > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(expression.OrderColumns[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			if (expression.LevelColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(expression.LevelColumn, Schema.TableVarColumnType.Level);
			}
			
			if (expression.SequenceColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(expression.SequenceColumn, Schema.TableVarColumnType.Sequence);
			}

			DecreaseIndent();
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitBinaryTableExpression(BinaryTableExpression expression)
		{
			if (expression is JoinExpression)
				EmitJoinExpression((JoinExpression)expression);
			else if (expression is HavingExpression)
				EmitHavingExpression((HavingExpression)expression);
			else if (expression is WithoutExpression)
				EmitWithoutExpression((WithoutExpression)expression);
			else if (expression is UnionExpression)
				EmitUnionExpression((UnionExpression)expression);
			else if (expression is IntersectExpression)
				EmitIntersectExpression((IntersectExpression)expression);
			else if (expression is DifferenceExpression)
				EmitDifferenceExpression((DifferenceExpression)expression);
			else if (expression is ProductExpression)
				EmitProductExpression((ProductExpression)expression);
			else if (expression is DivideExpression)
				EmitDivideExpression((DivideExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
		}
		
		protected virtual void EmitUnionExpression(UnionExpression expression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(expression.LeftExpression);
			AppendFormat(" {0} ", Keywords.Union);
			EmitExpression(expression.RightExpression);
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitIntersectExpression(IntersectExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Intersect);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDifferenceExpression(DifferenceExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Minus);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitProductExpression(ProductExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Times);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDivideExpression(DivideExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Divide);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitJoinExpression(JoinExpression expression)
		{
			if (expression is OuterJoinExpression)
				EmitOuterJoinExpression((OuterJoinExpression)expression);
			else if (expression is InnerJoinExpression)
				EmitInnerJoinExpression((InnerJoinExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
		}
		
		protected virtual void EmitHavingExpression(HavingExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Having);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			if (expression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(expression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitWithoutExpression(WithoutExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Without);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			if (expression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(expression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitInnerJoinExpression(InnerJoinExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(expression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);
			if (expression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(expression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitOuterJoinExpression(OuterJoinExpression expression)
		{
			if (expression is LeftOuterJoinExpression)
				EmitLeftOuterJoinExpression((LeftOuterJoinExpression)expression);
			else if (expression is RightOuterJoinExpression)
				EmitRightOuterJoinExpression((RightOuterJoinExpression)expression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, expression.GetType().Name);
		}
		
		protected virtual void EmitLeftOuterJoinExpression(LeftOuterJoinExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Left, expression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);

			if (expression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(expression.Condition);
			}

			if (expression.RowExistsColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(expression.RowExistsColumn, Schema.TableVarColumnType.RowExists);
			}

			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRightOuterJoinExpression(RightOuterJoinExpression expression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(expression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Right, expression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(expression.RightExpression);

			if (expression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(expression.Condition);
			}

			if (expression.RowExistsColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(expression.RowExistsColumn, Schema.TableVarColumnType.RowExists);
			}

			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTerminatedStatement(Statement statement)
		{
			NewLine();
			Indent();
			EmitStatement(statement);
			EmitStatementTerminator();
		}
		
		protected override void EmitStatement(Statement statement)
		{
			if (statement is D4Statement)
				EmitD4Statement((D4Statement)statement);
			else if (statement is AssignmentStatement)
				EmitAssignmentStatement((AssignmentStatement)statement);
			else if (statement is VariableStatement)
				EmitVariableStatement((VariableStatement)statement);
			else if (statement is ExpressionStatement)
				EmitExpressionStatement((ExpressionStatement)statement);
			else if (statement is IfStatement)
				EmitIfStatement((IfStatement)statement);
			else if (statement is DelimitedBlock)
				EmitDelimitedBlock((DelimitedBlock)statement);
			else if (statement is Block)
				EmitBlock((Block)statement);
			else if (statement is ExitStatement)
				EmitExitStatement((ExitStatement)statement);
			else if (statement is WhileStatement)
				EmitWhileStatement((WhileStatement)statement);
			else if (statement is DoWhileStatement)
				EmitDoWhileStatement((DoWhileStatement)statement);
			else if (statement is ForEachStatement)
				EmitForEachStatement((ForEachStatement)statement);
			else if (statement is BreakStatement)
				EmitBreakStatement((BreakStatement)statement);
			else if (statement is ContinueStatement)
				EmitContinueStatement((ContinueStatement)statement);
			else if (statement is CaseStatement)
				EmitCaseStatement((CaseStatement)statement);
			else if (statement is RaiseStatement)
				EmitRaiseStatement((RaiseStatement)statement);
			else if (statement is TryFinallyStatement)
				EmitTryFinallyStatement((TryFinallyStatement)statement);
			else if (statement is TryExceptStatement)
				EmitTryExceptStatement((TryExceptStatement)statement);
			else if (statement is EmptyStatement)
				EmitEmptyStatement((EmptyStatement)statement);
			else if (statement is SourceStatement)
				EmitSourceStatement((SourceStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitAssignmentStatement(AssignmentStatement statement)
		{
			EmitExpression(statement.Target);
			AppendFormat(" {0} ", Keywords.Assign);
			EmitExpression(statement.Expression);
		}
		
		protected virtual void EmitVariableStatement(VariableStatement statement)
		{
			AppendFormat("{0} ", Keywords.Var);
			EmitIdentifierExpression(statement.VariableName);
			if (statement.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.TypeSpecifier);
			}

			if (statement.Expression != null)
			{
				AppendFormat(" {0} ", Keywords.Assign);
				EmitExpression(statement.Expression);
			}
		}
		
		protected virtual void EmitExpressionStatement(ExpressionStatement statement)
		{
			EmitExpression(statement.Expression);
		}
		
		protected virtual void EmitIfStatement(IfStatement statement)
		{
			AppendFormat("{0} ", Keywords.If);
			EmitExpression(statement.Expression);
			AppendFormat(" {0}", Keywords.Then);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitStatement(statement.TrueStatement);
			DecreaseIndent();

			if (statement.FalseStatement != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Else);
				IncreaseIndent();
				NewLine();
				Indent();
				EmitStatement(statement.FalseStatement);
				DecreaseIndent();
			}
		}
		
		protected virtual void EmitDelimitedBlock(DelimitedBlock statement)
		{
			Append(Keywords.Begin);
			IncreaseIndent();
			EmitBlock(statement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitBlock(Block statement)
		{
			for (int index = 0; index < statement.Statements.Count; index++)
				EmitTerminatedStatement(statement.Statements[index]);
		}
		
		protected virtual void EmitExitStatement(ExitStatement statement)
		{
			Append(Keywords.Exit);
		}
		
		protected virtual void EmitWhileStatement(WhileStatement statement)
		{
			AppendFormat("{0} ", Keywords.While);
			EmitExpression(statement.Condition);
			AppendFormat(" {0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(statement.Statement);
			DecreaseIndent();
		}
		
		protected virtual void EmitDoWhileStatement(DoWhileStatement statement)
		{
			AppendFormat("{0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(statement.Statement);
			DecreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.While);
			EmitExpression(statement.Condition);
		}
		
		protected virtual void EmitForEachStatement(ForEachStatement statement)
		{
			AppendFormat("{0} ", Keywords.ForEach);
			if (statement.VariableName == String.Empty)
				AppendFormat("{0} ", Keywords.Row);
			else
			{
				if (statement.IsAllocation)
					AppendFormat("{0} ", Keywords.Var);
				AppendFormat("{0} ", statement.VariableName);
			}
			AppendFormat("{0} ", Keywords.In);
			EmitCursorDefinition(statement.Expression);
			AppendFormat(" {0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(statement.Statement);
			DecreaseIndent();
		}
		
		protected virtual void EmitBreakStatement(BreakStatement statement)
		{
			Append(Keywords.Break);
		}
		
		protected virtual void EmitContinueStatement(ContinueStatement statement)
		{
			Append(Keywords.Continue);
		}
		
		protected virtual void EmitCaseStatement(CaseStatement statement)
		{
			Append(Keywords.Case);
			if (statement.Expression != null)
			{
				Append(" ");
				EmitExpression(statement.Expression);
			}
			IncreaseIndent();

			for (int index = 0; index < statement.CaseItems.Count; index++)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.When);
				EmitExpression(statement.CaseItems[index].WhenExpression);
				AppendFormat(" {0} ", Keywords.Then);
				EmitTerminatedStatement(statement.CaseItems[index].ThenStatement);
			}
			
			if (statement.ElseStatement != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Else);
				EmitTerminatedStatement(statement.ElseStatement);
			}
			
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitRaiseStatement(RaiseStatement statement)
		{
			Append(Keywords.Raise);
			if (statement.Expression != null)
			{
				Append(" ");
				EmitExpression(statement.Expression);
			}
		}
		
		protected virtual void EmitTryFinallyStatement(TryFinallyStatement statement)
		{
			Append(Keywords.Try);
			IncreaseIndent();
			EmitTerminatedStatement(statement.TryStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Finally);
			IncreaseIndent();
			EmitTerminatedStatement(statement.FinallyStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitTryExceptStatement(TryExceptStatement statement)
		{
			Append(Keywords.Try);
			IncreaseIndent();
			EmitTerminatedStatement(statement.TryStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Except);
			IncreaseIndent();
			for (int index = 0; index < statement.ErrorHandlers.Count; index++)
				EmitErrorHandler(statement.ErrorHandlers[index]);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitErrorHandler(GenericErrorHandler statement)
		{
			if (statement is SpecificErrorHandler)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.On);
				if (statement is ParameterizedErrorHandler)
					AppendFormat("{0} {1} ", ((ParameterizedErrorHandler)statement).VariableName, Keywords.TypeSpecifier);
				AppendFormat("{0} ", ((SpecificErrorHandler)statement).ErrorTypeName);
				AppendFormat("{0} ", Keywords.Do);
				IncreaseIndent();
			}
			EmitTerminatedStatement(statement.Statement);
			if (statement is SpecificErrorHandler)
			{
				DecreaseIndent();
			}
		}
		
		protected virtual void EmitEmptyStatement(Statement statement)
		{
		}
		
		protected virtual void EmitSourceStatement(SourceStatement statement)
		{
			Append(statement.Source);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitD4Statement(Statement statement)
		{
			if (statement is D4DMLStatement)
				EmitD4DMLStatement((D4DMLStatement)statement);
			else if (statement is CreateTableVarStatement)
				EmitCreateTableVarStatement((CreateTableVarStatement)statement);
			else if (statement is CreateScalarTypeStatement)
				EmitCreateScalarTypeStatement((CreateScalarTypeStatement)statement);
			else if (statement is CreateOperatorStatementBase)
				EmitCreateOperatorStatementBase((CreateOperatorStatementBase)statement);
			else if (statement is CreateServerStatement)
				EmitCreateServerStatement((CreateServerStatement)statement);
			else if (statement is CreateDeviceStatement)
				EmitCreateDeviceStatement((CreateDeviceStatement)statement);
			else if (statement is AlterTableVarStatement)
				EmitAlterTableVarStatement((AlterTableVarStatement)statement);
			else if (statement is AlterScalarTypeStatement)
				EmitAlterScalarTypeStatement((AlterScalarTypeStatement)statement);
			else if (statement is AlterOperatorStatementBase)
				EmitAlterOperatorStatementBase((AlterOperatorStatementBase)statement);
			else if (statement is AlterServerStatement)
				EmitAlterServerStatement((AlterServerStatement)statement);
			else if (statement is AlterDeviceStatement)
				EmitAlterDeviceStatement((AlterDeviceStatement)statement);
			else if (statement is DropObjectStatement)
				EmitDropObjectStatement((DropObjectStatement)statement);
			else if (statement is CreateReferenceStatement)
				EmitCreateReferenceStatement((CreateReferenceStatement)statement);
			else if (statement is AlterReferenceStatement)
				EmitAlterReferenceStatement((AlterReferenceStatement)statement);
			else if (statement is DropReferenceStatement)
				EmitDropReferenceStatement((DropReferenceStatement)statement);
			else if (statement is CreateConstraintStatement)
				EmitCreateConstraintStatement((CreateConstraintStatement)statement);
			else if (statement is AlterConstraintStatement)
				EmitAlterConstraintStatement((AlterConstraintStatement)statement);
			else if (statement is DropConstraintStatement)
				EmitDropConstraintStatement((DropConstraintStatement)statement);
			else if (statement is CreateSortStatement)
				EmitCreateSortStatement((CreateSortStatement)statement);
			else if (statement is AlterSortStatement)
				EmitAlterSortStatement((AlterSortStatement)statement);
			else if (statement is DropSortStatement)
				EmitDropSortStatement((DropSortStatement)statement);
			else if (statement is CreateConversionStatement)
				EmitCreateConversionStatement((CreateConversionStatement)statement);
			else if (statement is DropConversionStatement)
				EmitDropConversionStatement((DropConversionStatement)statement);
			else if (statement is CreateRoleStatement)
				EmitCreateRoleStatement((CreateRoleStatement)statement);
			else if (statement is AlterRoleStatement)
				EmitAlterRoleStatement((AlterRoleStatement)statement);
			else if (statement is DropRoleStatement)
				EmitDropRoleStatement((DropRoleStatement)statement);
			else if (statement is CreateRightStatement)
				EmitCreateRightStatement((CreateRightStatement)statement);
			else if (statement is DropRightStatement)
				EmitDropRightStatement((DropRightStatement)statement);
			else if (statement is AttachStatementBase)
				EmitAttachStatementBase((AttachStatementBase)statement);
			else if (statement is RightStatementBase)
				EmitRightStatementBase((RightStatementBase)statement);
			else if (statement is TypeSpecifier)
				EmitTypeSpecifier((TypeSpecifier)statement); // Not part of the grammer proper, used to emit type specifiers explicitly
			else if (statement is KeyDefinition)
				EmitKeyDefinition((KeyDefinition)statement); // ditto
			else if (statement is OrderDefinition)
				EmitOrderDefinition((OrderDefinition)statement); // ditto
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitD4DMLStatement(Statement statement)
		{
			if (statement is SelectStatement)
				EmitSelectStatement((SelectStatement)statement);
			else if (statement is InsertStatement)
				EmitInsertStatement((InsertStatement)statement);
			else if (statement is UpdateStatement)
				EmitUpdateStatement((UpdateStatement)statement);
			else if (statement is DeleteStatement)
				EmitDeleteStatement((DeleteStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}

		protected virtual void EmitSelectStatement(SelectStatement statement)
		{
			AppendFormat("{0} ", Keywords.Select);
			EmitCursorDefinition(statement.CursorDefinition);
		}
		
		protected virtual void EmitInsertStatement(InsertStatement statement)
		{
			AppendFormat("{0} ", Keywords.Insert);
			EmitLanguageModifiers(statement);
			EmitExpression(statement.SourceExpression);
			AppendFormat(" {0} ", Keywords.Into);
			EmitExpression(statement.Target);
		}
		
		protected virtual void EmitUpdateStatement(UpdateStatement statement)
		{
			AppendFormat("{0} ", Keywords.Update);
			EmitLanguageModifiers(statement);
			EmitExpression(statement.Target);
			AppendFormat(" {0} {1} ", Keywords.Set, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitUpdateColumnExpression(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			if (statement.Condition != null)
			{
				AppendFormat(" {0} ", Keywords.Where);
				EmitExpression(statement.Condition);
			}
		}
		
		protected virtual void EmitUpdateColumnExpression(UpdateColumnExpression expression)
		{
			EmitExpression(expression.Target);
			AppendFormat(" {0} ", Keywords.Assign);
			EmitExpression(expression.Expression);
		}
		
		protected virtual void EmitDeleteStatement(DeleteStatement statement)
		{
			AppendFormat("{0} ", Keywords.Delete);
			EmitLanguageModifiers(statement);
			EmitExpression(statement.Target);
		}
		
		protected virtual void EmitCreateTableVarStatement(CreateTableVarStatement statement)
		{
			if (statement is CreateTableStatement)
				EmitCreateTableStatement((CreateTableStatement)statement);
			else if (statement is CreateViewStatement)
				EmitCreateViewStatement((CreateViewStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitCreateTableStatement(CreateTableStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}", Keywords.Table, statement.TableVarName);
			if (statement.DeviceName != null)
			{
				AppendFormat(" {0} ", Keywords.In);
				EmitIdentifierExpression(statement.DeviceName);
			}
			if (statement.FromExpression != null)
			{
				AppendFormat(" {0} ", Keywords.From);
				EmitExpression(statement.FromExpression);
			}
			else
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				for (int index = 0; index < statement.Columns.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitColumnDefinition(statement.Columns[index]);
				}
				
				for (int index = 0; index < statement.Keys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitKeyDefinition(statement.Keys[index]);
				}
				
				for (int index = 0; index < statement.Orders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitOrderDefinition(statement.Orders[index]);
				}
				
				for (int index = 0; index < statement.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(statement.Constraints[index]);
				}
				
				for (int index = 0; index < statement.References.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(statement.References[index]);
				}
				
				NewLine();
				Indent();
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateViewStatement(CreateViewStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}", Keywords.View, statement.TableVarName);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(statement.Expression);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
			NewLine();
			Indent();
			if 
				(
					(statement.Keys.Count > 0) || 
					(statement.Orders.Count > 0) || 
					(statement.Constraints.Count > 0) || 
					(statement.References.Count > 0)
				)
			{
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;

				for (int index = 0; index < statement.Keys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitKeyDefinition(statement.Keys[index]);
				}
				
				for (int index = 0; index < statement.Orders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitOrderDefinition(statement.Orders[index]);
				}
				
				for (int index = 0; index < statement.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(statement.Constraints[index]);
				}
				
				for (int index = 0; index < statement.References.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(statement.References[index]);
				}

				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateScalarTypeStatement(CreateScalarTypeStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Type, statement.ScalarTypeName);

			if (statement.FromClassDefinition != null)
			{
				AppendFormat(" {0}", Keywords.From);
				EmitClassDefinition(statement.FromClassDefinition);
			}

			if (statement.ParentScalarTypes.Count > 0)
			{
				AppendFormat(" {0} {1} ", Keywords.Is, Keywords.BeginList);
				for (int index = 0; index < statement.ParentScalarTypes.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					EmitScalarTypeNameDefinition(statement.ParentScalarTypes[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}

			if (statement.LikeScalarTypeName != String.Empty)
				AppendFormat(" {0} {1}", Keywords.Like, statement.LikeScalarTypeName);

			if 
				(
					(statement.Default != null) || 
					(statement.Constraints.Count > 0) || 
					(statement.Representations.Count > 0) || 
					(statement.Specials.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				if (statement.Default != null)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDefaultDefinition(statement.Default);
				}
				
				for (int index = 0; index < statement.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(statement.Constraints[index]);
				}

				for (int index = 0; index < statement.Representations.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitRepresentationDefinition(statement.Representations[index]);
				}

				for (int index = 0; index < statement.Specials.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitSpecialDefinition(statement.Specials[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Append(Keywords.EndList);
			}
			EmitClassDefinition(statement.ClassDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateOperatorStatementBase(Statement statement)
		{
			if (statement is CreateOperatorStatement)
				EmitCreateOperatorStatement((CreateOperatorStatement)statement);
			else if (statement is CreateAggregateOperatorStatement)
				EmitCreateAggregateOperatorStatement((CreateAggregateOperatorStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitCreateOperatorStatement(CreateOperatorStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}{2}", Keywords.Operator, statement.OperatorName, Keywords.BeginGroup);

			for (int index = 0; index < statement.FormalParameters.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitFormalParameter(statement.FormalParameters[index]);
			}
			
			Append(Keywords.EndGroup);

			if (statement.ReturnType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.ReturnType);
			}
			
			#if VirtualSupport
			if (AStatement.IsReintroduced)
				AppendFormat(" {0}", Keywords.Reintroduce);
				
			if (AStatement.IsAbstract)
				AppendFormat(" {0}", Keywords.Abstract);
			else if (AStatement.IsVirtual)
				AppendFormat(" {0}", Keywords.Virtual);
			else if (AStatement.IsOverride)
				AppendFormat(" {0}", Keywords.Override);
			#endif
				
			NewLine();
			Indent();
			if (statement.Block.ClassDefinition != null)
				EmitClassDefinition(statement.Block.ClassDefinition);
			else if (statement.Block.Block != null)
			{
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(statement.Block.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateAggregateOperatorStatement(CreateAggregateOperatorStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2}{3}", Keywords.Aggregate, Keywords.Operator, statement.OperatorName, Keywords.BeginGroup);

			for (int index = 0; index < statement.FormalParameters.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitFormalParameter(statement.FormalParameters[index]);
			}
			
			Append(Keywords.EndGroup);

			if (statement.ReturnType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.ReturnType);
			}
			
			#if VirtualSupport
			if (AStatement.IsReintroduced)
				AppendFormat(" {0}", Keywords.Reintroduce);
				
			if (AStatement.IsAbstract)
				AppendFormat(" {0}", Keywords.Abstract);
			else if (AStatement.IsVirtual)
				AppendFormat(" {0}", Keywords.Virtual);
			else if (AStatement.IsOverride)
				AppendFormat(" {0}", Keywords.Override);
			#endif

			if ((statement.Initialization.ClassDefinition != null) || (statement.Initialization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Initialization);
				NewLine();
				Indent();
				if (statement.Initialization.ClassDefinition != null)
					EmitClassDefinition(statement.Initialization.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Initialization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((statement.Aggregation.ClassDefinition != null) || (statement.Aggregation.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Aggregation);
				NewLine();
				Indent();
				if (statement.Aggregation.ClassDefinition != null)
					EmitClassDefinition(statement.Aggregation.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Aggregation.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((statement.Finalization.ClassDefinition != null) || (statement.Finalization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Finalization);
				NewLine();
				Indent();
				if (statement.Finalization.ClassDefinition != null)
					EmitClassDefinition(statement.Finalization.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Finalization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateServerStatement(CreateServerStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Server, statement.ServerName);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceStatement(CreateDeviceStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Device, statement.DeviceName);
			if ((statement.DeviceScalarTypeMaps.Count > 0) || (statement.DeviceOperatorMaps.Count > 0))
			{
				bool first = true;
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				
				for (int index = 0; index < statement.DeviceScalarTypeMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDeviceScalarTypeMap(statement.DeviceScalarTypeMaps[index]);
				}

				for (int index = 0; index < statement.DeviceOperatorMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDeviceOperatorMap(statement.DeviceOperatorMaps[index]);
				}

				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			
			NewLine();
			Indent();
			EmitReconciliationSettings(statement.ReconciliationSettings, false);
			EmitClassDefinition(statement.ClassDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitReconciliationSettings(ReconciliationSettings reconciliationSettings, bool isAlter)
		{
			if ((reconciliationSettings != null) && (reconciliationSettings.ReconcileModeSet || reconciliationSettings.ReconcileMasterSet))
			{
				if (isAlter)
					AppendFormat(" {0}", Keywords.Alter);
				AppendFormat(" {0} {1} ", Keywords.Reconciliation, Keywords.BeginList);
				bool first = true;
				if (reconciliationSettings.ReconcileModeSet)
				{
					AppendFormat("{0} {1} {2}", Keywords.Mode, Keywords.Equal, Keywords.BeginList);
					bool firstMode = true;
					if (reconciliationSettings.ReconcileMode == ReconcileMode.None)
						Append(ReconcileMode.None.ToString().ToLower());
					else
					{
						if ((reconciliationSettings.ReconcileMode & ReconcileMode.Startup) != 0)
						{
							if (!firstMode)
								EmitListSeparator();
							else
								firstMode = false;
							Append(ReconcileMode.Startup.ToString().ToLower());
						}
						
						if ((reconciliationSettings.ReconcileMode & ReconcileMode.Command) != 0)
						{
							if (!firstMode)
								EmitListSeparator();
							else
								firstMode = false;
							Append(ReconcileMode.Command.ToString().ToLower());
						}
						
						if ((reconciliationSettings.ReconcileMode & ReconcileMode.Automatic) != 0)
						{
							if (!firstMode)
								EmitListSeparator();
							else
								firstMode = false;
							Append(ReconcileMode.Automatic.ToString().ToLower());
						}
					}
					Append(Keywords.EndList);
					first = false;
				}
				
				if (reconciliationSettings.ReconcileMasterSet)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
						
					AppendFormat("{0} {1} {2}", Keywords.Master, Keywords.Equal, reconciliationSettings.ReconcileMaster.ToString().ToLower());
				}
				
				AppendFormat(" {0}", Keywords.EndList);
			}
		}
		
		protected virtual void EmitAlterTableVarStatement(Statement statement)
		{
			if (statement is AlterTableStatement)
				EmitAlterTableStatement((AlterTableStatement)statement);
			else if (statement is AlterViewStatement)
				EmitAlterViewStatement((AlterViewStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitAlterTableStatement(AlterTableStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Table, statement.TableVarName);
			if
				(
					(statement.CreateColumns.Count > 0) ||
					(statement.AlterColumns.Count > 0) ||
					(statement.DropColumns.Count > 0) ||
					(statement.CreateKeys.Count > 0) ||
					(statement.AlterKeys.Count > 0) ||
					(statement.DropKeys.Count > 0) ||
					(statement.CreateOrders.Count > 0) ||
					(statement.AlterOrders.Count > 0) ||
					(statement.DropOrders.Count > 0) ||
					(statement.CreateConstraints.Count > 0) ||
					(statement.AlterConstraints.Count > 0) ||
					(statement.DropConstraints.Count > 0) ||
					(statement.CreateReferences.Count > 0) ||
					(statement.AlterReferences.Count > 0) ||
					(statement.DropReferences.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;

				for (int index = 0; index < statement.CreateColumns.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateColumnDefinition(statement.CreateColumns[index]);
				}
				
				for (int index = 0; index < statement.AlterColumns.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterColumnDefinition(statement.AlterColumns[index]);
				}
				
				for (int index = 0; index < statement.DropColumns.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropColumnDefinition(statement.DropColumns[index]);
				}
				
				for (int index = 0; index < statement.CreateKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateKeyDefinition(statement.CreateKeys[index]);
				}
				
				for (int index = 0; index < statement.AlterKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(statement.AlterKeys[index]);
				}
				
				for (int index = 0; index < statement.DropKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(statement.DropKeys[index]);
				}
				
				for (int index = 0; index < statement.CreateOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateOrderDefinition(statement.CreateOrders[index]);
				}
				
				for (int index = 0; index < statement.AlterOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(statement.AlterOrders[index]);
				}
				
				for (int index = 0; index < statement.DropOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(statement.DropOrders[index]);
				}
				
				for (int index = 0; index < statement.CreateReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateReferenceDefinition(statement.CreateReferences[index]);
				}
				
				for (int index = 0; index < statement.AlterReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(statement.AlterReferences[index]);
				}
				
				for (int index = 0; index < statement.DropReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(statement.DropReferences[index]);
				}
				
				for (int index = 0; index < statement.CreateConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateCreateConstraintDefinition(statement.CreateConstraints[index]);
				}
				
				for (int index = 0; index < statement.AlterConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(statement.AlterConstraints[index]);
				}
				
				for (int index = 0; index < statement.DropConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(statement.DropConstraints[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterViewStatement(AlterViewStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.View, statement.TableVarName);
			if
				(
					(statement.CreateKeys.Count > 0) ||
					(statement.AlterKeys.Count > 0) ||
					(statement.DropKeys.Count > 0) ||
					(statement.CreateOrders.Count > 0) ||
					(statement.AlterOrders.Count > 0) ||
					(statement.DropOrders.Count > 0) ||
					(statement.CreateConstraints.Count > 0) ||
					(statement.AlterConstraints.Count > 0) ||
					(statement.DropConstraints.Count > 0) ||
					(statement.CreateReferences.Count > 0) ||
					(statement.AlterReferences.Count > 0) ||
					(statement.DropReferences.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;

				for (int index = 0; index < statement.CreateKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateKeyDefinition(statement.CreateKeys[index]);
				}
				
				for (int index = 0; index < statement.AlterKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(statement.AlterKeys[index]);
				}
				
				for (int index = 0; index < statement.DropKeys.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(statement.DropKeys[index]);
				}
				
				for (int index = 0; index < statement.CreateOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateOrderDefinition(statement.CreateOrders[index]);
				}
				
				for (int index = 0; index < statement.AlterOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(statement.AlterOrders[index]);
				}
				
				for (int index = 0; index < statement.DropOrders.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(statement.DropOrders[index]);
				}
				
				for (int index = 0; index < statement.CreateReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateReferenceDefinition(statement.CreateReferences[index]);
				}
				
				for (int index = 0; index < statement.AlterReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(statement.AlterReferences[index]);
				}
				
				for (int index = 0; index < statement.DropReferences.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(statement.DropReferences[index]);
				}
				
				for (int index = 0; index < statement.CreateConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateCreateConstraintDefinition(statement.CreateConstraints[index]);
				}
				
				for (int index = 0; index < statement.AlterConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(statement.AlterConstraints[index]);
				}
				
				for (int index = 0; index < statement.DropConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(statement.DropConstraints[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterScalarTypeStatement(AlterScalarTypeStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Type, statement.ScalarTypeName);
			if
				(
					(statement.Default != null) ||
					(statement.CreateRepresentations.Count > 0) ||
					(statement.AlterRepresentations.Count > 0) ||
					(statement.DropRepresentations.Count > 0) ||
					(statement.CreateSpecials.Count > 0) ||
					(statement.AlterSpecials.Count > 0) ||
					(statement.DropSpecials.Count > 0) ||
					(statement.CreateConstraints.Count > 0) ||
					(statement.AlterConstraints.Count > 0) ||
					(statement.DropConstraints.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				
				if (statement.Default != null)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					if (statement.Default is DefaultDefinition)
						EmitCreateDefaultDefinition((DefaultDefinition)statement.Default);
					else if (statement.Default is AlterDefaultDefinition)
						EmitAlterDefaultDefinition((AlterDefaultDefinition)statement.Default);
					else if (statement.Default is DropDefaultDefinition)
						EmitDropDefaultDefinition((DropDefaultDefinition)statement.Default);
					else
						throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.Default.GetType().Name);
				}
				
				for (int index = 0; index < statement.CreateRepresentations.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateRepresentationDefinition(statement.CreateRepresentations[index]);
				}
				
				for (int index = 0; index < statement.AlterRepresentations.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterRepresentationDefinition(statement.AlterRepresentations[index]);
				}
				
				for (int index = 0; index < statement.DropRepresentations.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropRepresentationDefinition(statement.DropRepresentations[index]);
				}
				
				for (int index = 0; index < statement.CreateSpecials.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateSpecialDefinition(statement.CreateSpecials[index]);
				}
				
				for (int index = 0; index < statement.AlterSpecials.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterSpecialDefinition(statement.AlterSpecials[index]);
				}
				
				for (int index = 0; index < statement.DropSpecials.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropSpecialDefinition(statement.DropSpecials[index]);
				}
				
				for (int index = 0; index < statement.CreateConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(statement.CreateConstraints[index]);
				}
				
				for (int index = 0; index < statement.AlterConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(statement.AlterConstraints[index]);
				}
				
				for (int index = 0; index < statement.DropConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(statement.DropConstraints[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterClassDefinition(statement.AlterClassDefinition);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterOperatorStatementBase(Statement statement)
		{
			if (statement is AlterOperatorStatement)
				EmitAlterOperatorStatement((AlterOperatorStatement)statement);
			else if (statement is AlterAggregateOperatorStatement)
				EmitAlterAggregateOperatorStatement((AlterAggregateOperatorStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitAlterOperatorStatement(AlterOperatorStatement statement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Operator);
			EmitOperatorSpecifier(statement.OperatorSpecifier);
			if (statement.Block.AlterClassDefinition != null)
				EmitAlterClassDefinition(statement.Block.AlterClassDefinition);
			else if (statement.Block.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(statement.Block.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterAggregateOperatorStatement(AlterAggregateOperatorStatement statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Aggregate, Keywords.Operator);
			EmitOperatorSpecifier(statement.OperatorSpecifier);

			if ((statement.Initialization.AlterClassDefinition != null) || (statement.Initialization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Initialization);
				NewLine();
				Indent();
				if (statement.Initialization.AlterClassDefinition != null)
					EmitAlterClassDefinition(statement.Initialization.AlterClassDefinition);
				else if (statement.Initialization.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Initialization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((statement.Aggregation.AlterClassDefinition != null) || (statement.Aggregation.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Aggregation);
				NewLine();
				Indent();
				if (statement.Aggregation.AlterClassDefinition != null)
					EmitAlterClassDefinition(statement.Aggregation.AlterClassDefinition);
				else if (statement.Aggregation.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Aggregation.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((statement.Finalization.AlterClassDefinition != null) || (statement.Finalization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Finalization);
				NewLine();
				Indent();
				if (statement.Finalization.AlterClassDefinition != null)
					EmitAlterClassDefinition(statement.Finalization.AlterClassDefinition);
				else if (statement.Finalization.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(statement.Finalization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterServerStatement(AlterServerStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Server, statement.ServerName);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterDeviceStatement(AlterDeviceStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Device, statement.DeviceName);
			if
				(
					(statement.CreateDeviceScalarTypeMaps.Count > 0) ||
					(statement.AlterDeviceScalarTypeMaps.Count > 0) ||
					(statement.DropDeviceScalarTypeMaps.Count > 0) ||
					(statement.CreateDeviceOperatorMaps.Count > 0) ||
					(statement.AlterDeviceOperatorMaps.Count > 0) ||
					(statement.DropDeviceOperatorMaps.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				
				for (int index = 0; index < statement.CreateDeviceScalarTypeMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateDeviceScalarTypeMap(statement.CreateDeviceScalarTypeMaps[index]);
				}
				
				for (int index = 0; index < statement.AlterDeviceScalarTypeMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterDeviceScalarTypeMap(statement.AlterDeviceScalarTypeMaps[index]);
				}
				
				for (int index = 0; index < statement.DropDeviceScalarTypeMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropDeviceScalarTypeMap(statement.DropDeviceScalarTypeMaps[index]);
				}
				
				for (int index = 0; index < statement.CreateDeviceOperatorMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateDeviceOperatorMap(statement.CreateDeviceOperatorMaps[index]);
				}
				
				for (int index = 0; index < statement.AlterDeviceOperatorMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterDeviceOperatorMap(statement.AlterDeviceOperatorMaps[index]);
				}
				
				for (int index = 0; index < statement.DropDeviceOperatorMaps.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropDeviceOperatorMap(statement.DropDeviceOperatorMaps[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitReconciliationSettings(statement.ReconciliationSettings, true);
			EmitAlterClassDefinition(statement.AlterClassDefinition);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropObjectStatement(Statement statement)
		{
			if (statement is DropTableStatement)
				EmitDropTableStatement((DropTableStatement)statement);
			else if (statement is DropViewStatement)
				EmitDropViewStatement((DropViewStatement)statement);
			else if (statement is DropScalarTypeStatement)
				EmitDropScalarTypeStatement((DropScalarTypeStatement)statement);
			else if (statement is DropOperatorStatement)
				EmitDropOperatorStatement((DropOperatorStatement)statement);
			else if (statement is DropServerStatement)
				EmitDropServerStatement((DropServerStatement)statement);
			else if (statement is DropDeviceStatement)
				EmitDropDeviceStatement((DropDeviceStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}

		protected virtual void EmitDropTableStatement(DropTableStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Table, statement.ObjectName);
		}
		
		protected virtual void EmitDropViewStatement(DropViewStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.View, statement.ObjectName);
		}
		
		protected virtual void EmitDropScalarTypeStatement(DropScalarTypeStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Type, statement.ObjectName);
		}
		
		protected virtual void EmitDropOperatorStatement(DropOperatorStatement statement)
		{
			AppendFormat("{0} {1} {2}{3}", Keywords.Drop, Keywords.Operator, statement.ObjectName, Keywords.BeginGroup);
			for (int index = 0; index < statement.FormalParameterSpecifiers.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitFormalParameterSpecifier(statement.FormalParameterSpecifiers[index]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDropServerStatement(DropServerStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Server, statement.ObjectName);
		}
		
		protected virtual void EmitDropDeviceStatement(DropDeviceStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Device, statement.ObjectName);
		}
		
		protected virtual void EmitScalarTypeNameDefinition(ScalarTypeNameDefinition statement)
		{
			Append(statement.ScalarTypeName);
		}
		
		protected virtual void EmitAccessorBlock(AccessorBlock accessorBlock)
		{
			if (accessorBlock.ClassDefinition != null)
				EmitClassDefinition(accessorBlock.ClassDefinition);
			else if (accessorBlock.Expression != null)
				EmitExpression(accessorBlock.Expression);
			else if (accessorBlock.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(accessorBlock.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
		}
		
		protected virtual void EmitAlterAccessorBlock(AlterAccessorBlock accessorBlock)
		{
			if (accessorBlock.AlterClassDefinition != null)
				EmitAlterClassDefinition(accessorBlock.AlterClassDefinition);
			else if (accessorBlock.Expression != null)
				EmitExpression(accessorBlock.Expression);
			else if (accessorBlock.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(accessorBlock.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
		}
		
		protected virtual void EmitRepresentationDefinitionBase(RepresentationDefinitionBase statement)
		{
			if (statement is RepresentationDefinition)
				EmitRepresentationDefinition((RepresentationDefinition)statement);
			else if (statement is AlterRepresentationDefinition)
				EmitAlterRepresentationDefinition((AlterRepresentationDefinition)statement);
			else if (statement is DropRepresentationDefinition)
				EmitDropRepresentationDefinition((DropRepresentationDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitRepresentationDefinition(RepresentationDefinition statement)
		{
			AppendFormat("{0} {1}", Keywords.Representation, statement.RepresentationName);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int index = 0; index < statement.Properties.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitPropertyDefinition(statement.Properties[index]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			if (statement.SelectorAccessorBlock != null)
			{
				AppendFormat("{0} ", Keywords.Selector);
				EmitAccessorBlock(statement.SelectorAccessorBlock);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateRepresentationDefinition(RepresentationDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitRepresentationDefinition(statement);
		}
		
		protected virtual void EmitAlterRepresentationDefinition(AlterRepresentationDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Representation, statement.RepresentationName);
			if 
				(
					(statement.CreateProperties.Count > 0) ||
					(statement.AlterProperties.Count > 0) ||
					(statement.DropProperties.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();

				for (int index = 0; index < statement.CreateProperties.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitCreatePropertyDefinition(statement.CreateProperties[index]);
				}

				for (int index = 0; index < statement.AlterProperties.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitAlterPropertyDefinition(statement.AlterProperties[index]);
				}

				for (int index = 0; index < statement.DropProperties.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitDropPropertyDefinition(statement.DropProperties[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			if (statement.SelectorAccessorBlock != null)
			{
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Selector);
				EmitAlterAccessorBlock(statement.SelectorAccessorBlock);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropRepresentationDefinition(DropRepresentationDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Representation, statement.RepresentationName);
		}
		
		protected virtual void EmitPropertyDefinitionBase(PropertyDefinitionBase statement)
		{
			if (statement is PropertyDefinition)
				EmitPropertyDefinition((PropertyDefinition)statement);
			else if (statement is AlterPropertyDefinition)
				EmitAlterPropertyDefinition((AlterPropertyDefinition)statement);
			else if (statement is DropPropertyDefinition)
				EmitDropPropertyDefinition((DropPropertyDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitPropertyDefinition(PropertyDefinition statement)
		{
			AppendFormat("{0} {1} ", statement.PropertyName, Keywords.TypeSpecifier);
			EmitTypeSpecifier(statement.PropertyType);
			IncreaseIndent();
			
			if (statement.ReadAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Read);
				EmitAccessorBlock(statement.ReadAccessorBlock);
			}
			
			if (statement.WriteAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Write);
				EmitAccessorBlock(statement.WriteAccessorBlock);
			}

			if (statement.MetaData != null)
			{
				NewLine();
				Indent();
				EmitMetaData(statement.MetaData);
			}

			DecreaseIndent();
		}
		
		protected virtual void EmitCreatePropertyDefinition(PropertyDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitPropertyDefinition(statement);
		}
		
		protected virtual void EmitAlterPropertyDefinition(AlterPropertyDefinition statement)
		{
			AppendFormat("{0} {1}", Keywords.Alter, statement.PropertyName);
			if (statement.PropertyType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.PropertyType);
			}
			
			IncreaseIndent();
			
			if (statement.ReadAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Read);
				EmitAlterAccessorBlock(statement.ReadAccessorBlock);
			}
			
			if (statement.WriteAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Write);
				EmitAlterAccessorBlock(statement.WriteAccessorBlock);
			}
			
			if (statement.AlterMetaData != null)
			{
				NewLine();
				Indent();
				EmitAlterMetaData(statement.AlterMetaData);
			}
			
			DecreaseIndent();
		}
		
		protected virtual void EmitDropPropertyDefinition(DropPropertyDefinition statement)
		{
			AppendFormat("{0} {1}", Keywords.Drop, statement.PropertyName);
		}
		
		protected virtual void EmitSpecialDefinition(SpecialDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Special, statement.Name, Keywords.BeginGroup);
			EmitExpression(statement.Value);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateSpecialDefinition(SpecialDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitSpecialDefinition(statement);
		}
		
		protected virtual void EmitAlterSpecialDefinition(AlterSpecialDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Special, statement.Name);
			if (statement.Value != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(statement.Value);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropSpecialDefinition(DropSpecialDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Special, statement.Name);
		}
		
		protected virtual void EmitColumnDefinitionBase(ColumnDefinitionBase statement)
		{
			if (statement is ColumnDefinition)
				EmitColumnDefinition((ColumnDefinition)statement);
			else if (statement is AlterColumnDefinition)
				EmitAlterColumnDefinition((AlterColumnDefinition)statement);
			else if (statement is DropColumnDefinition)
				EmitDropColumnDefinition((DropColumnDefinition)statement);
			else if (statement is KeyColumnDefinition)
				EmitKeyColumnDefinition((KeyColumnDefinition)statement);
			else if (statement is ReferenceColumnDefinition)
				EmitReferenceColumnDefinition((ReferenceColumnDefinition)statement);
			else if (statement is OrderColumnDefinition)
				EmitOrderColumnDefinition((OrderColumnDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitColumnDefinition(ColumnDefinition statement)
		{
			AppendFormat("{0} {1} ", statement.ColumnName, Keywords.TypeSpecifier);
			EmitTypeSpecifier(statement.TypeSpecifier);
			if (statement.IsNilable)
				AppendFormat(" {0}", Keywords.Nil);
			
			if ((statement.Default != null) || (statement.Constraints.Count > 0))
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				
				if (statement.Default != null)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDefaultDefinition(statement.Default);
				}
				
				for (int index = 0; index < statement.Constraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(statement.Constraints[index]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateColumnDefinition(ColumnDefinition statement)
		{
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Column);
			EmitColumnDefinition(statement);
		}
		
		protected virtual void EmitAlterColumnDefinition(AlterColumnDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Column, statement.ColumnName);
			if (statement.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.TypeSpecifier);
			}
			
			if 
				(
					(statement.ChangeNilable) ||
					(statement.Default != null) ||
					(statement.CreateConstraints.Count > 0) ||
					(statement.AlterConstraints.Count > 0) ||
					(statement.DropConstraints.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool first = true;
				
				if (statement.ChangeNilable)
				{
					first = false;
					NewLine();
					Indent();
					if (!statement.IsNilable)
						AppendFormat("{0} ", Keywords.Not);
					Append(Keywords.Nil);
				}

				if (statement.Default != null)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					if (statement.Default is DefaultDefinition)
						EmitCreateDefaultDefinition((DefaultDefinition)statement.Default);
					else if (statement.Default is AlterDefaultDefinition)
						EmitAlterDefaultDefinition((AlterDefaultDefinition)statement.Default);
					else if (statement.Default is DropDefaultDefinition)
						EmitDropDefaultDefinition((DropDefaultDefinition)statement.Default);
					else
						throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.Default.GetType().Name);
				}
				
				for (int index = 0; index < statement.CreateConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(statement.CreateConstraints[index]);
				}
				
				for (int index = 0; index < statement.AlterConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(statement.AlterConstraints[index]);
				}
				
				for (int index = 0; index < statement.DropConstraints.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(statement.DropConstraints[index]);
				}

				DecreaseIndent();
				NewLine();
				Indent();				
				Append(Keywords.EndList);
			}
			
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropColumnDefinition(DropColumnDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Column, statement.ColumnName);
		}
		
		protected virtual void EmitKeyColumnDefinition(KeyColumnDefinition statement)
		{
			Append(statement.ColumnName);
		}
		
		protected virtual void EmitReferenceColumnDefinition(ReferenceColumnDefinition statement)
		{
			Append(statement.ColumnName);
		}
		
		protected virtual void EmitOrderColumnDefinition(OrderColumnDefinition statement)
		{
			AppendFormat("{0} ", statement.ColumnName);
			if (statement.Sort != null)
			{
				EmitSortDefinition(statement.Sort);
				Append(" ");
			}
			AppendFormat("{0}", statement.Ascending ? Keywords.Asc : Keywords.Desc);
			if (statement.IncludeNils)
				AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
		}
		
		protected virtual void EmitKeyDefinitionBase(KeyDefinitionBase statement)
		{
			if (statement is KeyDefinition)
				EmitKeyDefinition((KeyDefinition)statement);
			else if (statement is AlterKeyDefinition)
				EmitAlterKeyDefinition((AlterKeyDefinition)statement);
			else if (statement is DropKeyDefinition)
				EmitDropKeyDefinition((DropKeyDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitKeyDefinition(KeyDefinition statement)
		{
			AppendFormat("{0} {1} ", Keywords.Key, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateKeyDefinition(KeyDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitKeyDefinition(statement);
		}
		
		protected virtual void EmitAlterKeyDefinition(AlterKeyDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Key, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropKeyDefinition(DropKeyDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Drop, Keywords.Key, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitReferenceDefinitionBase(ReferenceDefinitionBase statement)
		{
			if (statement is ReferenceDefinition)
				EmitReferenceDefinition((ReferenceDefinition)statement);
			else if (statement is AlterReferenceDefinition)
				EmitAlterReferenceDefinition((AlterReferenceDefinition)statement);
			else if (statement is DropReferenceDefinition)
				EmitDropReferenceDefinition((DropReferenceDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitReferenceDefinition(ReferenceDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Reference, statement.ReferenceName, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0} ", Keywords.EndList);
			EmitReferencesDefinition(statement.ReferencesDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateReferenceDefinition(ReferenceDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitReferenceDefinition(statement);
		}
		
		protected virtual void EmitCreateReferenceStatement(CreateReferenceStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2} {3} ", Keywords.Reference, statement.ReferenceName, statement.TableVarName, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0} ", Keywords.EndList);
			EmitReferencesDefinition(statement.ReferencesDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitAlterReferenceDefinition(AlterReferenceDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Reference, statement.ReferenceName);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterReferenceStatement(AlterReferenceStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Reference, statement.ReferenceName);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropReferenceDefinition(DropReferenceDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Reference, statement.ReferenceName);
		}
		
		protected virtual void EmitDropReferenceStatement(DropReferenceStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Reference, statement.ReferenceName);
		}
		
		protected virtual void EmitReferencesDefinition(ReferencesDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.References, statement.TableVarName, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0} ", Keywords.EndList);

			switch (statement.UpdateReferenceAction)
			{
				case ReferenceAction.Require: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Require); break;
				case ReferenceAction.Cascade: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Cascade); break;
				case ReferenceAction.Clear: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Clear); break;
				case ReferenceAction.Set:
					AppendFormat(" {0} {1} {2} ", Keywords.Update, Keywords.Set, Keywords.BeginList);
					for (int index = 0; index < statement.UpdateReferenceExpressions.Count; index++)
					{
						if (index > 0)
							EmitListSeparator();
						EmitExpression(statement.UpdateReferenceExpressions[index]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}

			switch (statement.DeleteReferenceAction)
			{
				case ReferenceAction.Require: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Require); break;
				case ReferenceAction.Cascade: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Cascade); break;
				case ReferenceAction.Clear: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Clear); break;
				case ReferenceAction.Set:
					AppendFormat(" {0} {1} {2} ", Keywords.Delete, Keywords.Set, Keywords.BeginList);
					for (int index = 0; index < statement.DeleteReferenceExpressions.Count; index++)
					{
						if (index > 0)
							EmitListSeparator();
						EmitExpression(statement.DeleteReferenceExpressions[index]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}
		}
		
		protected virtual void EmitOrderDefinitionBase(OrderDefinitionBase statement)
		{
			if (statement is OrderDefinition)
				EmitOrderDefinition((OrderDefinition)statement);
			else if (statement is AlterOrderDefinition)
				EmitAlterOrderDefinition((AlterOrderDefinition)statement);
			else if (statement is DropOrderDefinition)
				EmitDropOrderDefinition((DropOrderDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitOrderDefinition(OrderDefinition statement)
		{
			AppendFormat("{0} {1} ", Keywords.Order, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateOrderDefinition(OrderDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitOrderDefinition(statement);
		}
		
		protected virtual void EmitAlterOrderDefinition(AlterOrderDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Order, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropOrderDefinition(DropOrderDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Drop, Keywords.Order, Keywords.BeginList);
			for (int index = 0; index < statement.Columns.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(statement.Columns[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitConstraintDefinitionBase(ConstraintDefinitionBase statement)
		{
			if (statement is ConstraintDefinition)
				EmitConstraintDefinition((ConstraintDefinition)statement);
			else if (statement is AlterConstraintDefinition)
				EmitAlterConstraintDefinition((AlterConstraintDefinition)statement);
			else if (statement is DropConstraintDefinition)
				EmitDropConstraintDefinition((DropConstraintDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitConstraintDefinition(ConstraintDefinition statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Constraint, statement.ConstraintName, Keywords.BeginGroup);
			EmitExpression(statement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateConstraintDefinition(CreateConstraintDefinition statement)
		{
			if (statement is ConstraintDefinition)
				EmitConstraintDefinition((ConstraintDefinition)statement);
			else
				EmitTransitionConstraintDefinition((TransitionConstraintDefinition)statement);
		}
		
		protected virtual void EmitTransitionConstraintDefinition(TransitionConstraintDefinition statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Transition, Keywords.Constraint, statement.ConstraintName);
			IncreaseIndent();
			if (statement.OnInsertExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Insert);
				EmitExpression(statement.OnInsertExpression);
			}
			if (statement.OnUpdateExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Update);
				EmitExpression(statement.OnUpdateExpression);
			}
			if (statement.OnDeleteExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Delete);
				EmitExpression(statement.OnDeleteExpression);
			}
			NewLine();
			Indent();
			EmitMetaData(statement.MetaData);
			DecreaseIndent();
		}
		
		protected virtual void EmitCreateConstraintDefinition(ConstraintDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitConstraintDefinition(statement);
		}
		
		protected virtual void EmitCreateCreateConstraintDefinition(CreateConstraintDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitCreateConstraintDefinition(statement);
		}
		
		protected virtual void EmitCreateConstraintStatement(CreateConstraintStatement statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (statement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2}", Keywords.Constraint, statement.ConstraintName, Keywords.BeginGroup);
			EmitExpression(statement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitAlterConstraintDefinitionBase(AlterConstraintDefinitionBase statement)
		{
			if (statement is AlterConstraintDefinition)
				EmitAlterConstraintDefinition((AlterConstraintDefinition)statement);
			else
				EmitAlterTransitionConstraintDefinition((AlterTransitionConstraintDefinition)statement);
		}
		
		protected virtual void EmitAlterConstraintDefinition(AlterConstraintDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Alter);
			AppendFormat("{0} {1}", Keywords.Constraint, statement.ConstraintName);
			if (statement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(statement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitAlterTransitionConstraintDefinitionItem(string transition, AlterTransitionConstraintDefinitionItemBase item)
		{
			if (item is AlterTransitionConstraintDefinitionCreateItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2} ", Keywords.Create, Keywords.On, transition);
				EmitExpression(((AlterTransitionConstraintDefinitionCreateItem)item).Expression);
			}
			else if (item is AlterTransitionConstraintDefinitionAlterItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.On, transition);
				EmitExpression(((AlterTransitionConstraintDefinitionAlterItem)item).Expression);
			}
			else if (item is AlterTransitionConstraintDefinitionDropItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.On, transition);
			}
		}
		
		protected virtual void EmitAlterTransitionConstraintDefinition(AlterTransitionConstraintDefinition statement)
		{
			AppendFormat("{0} {1} {2} {3}", Keywords.Alter, Keywords.Transition, Keywords.Constraint, statement.ConstraintName);
			IncreaseIndent();
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Insert, statement.OnInsert);
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Update, statement.OnUpdate);
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Delete, statement.OnDelete);
			if (statement.AlterMetaData != null)
			{
				NewLine();
				Indent();
				EmitAlterMetaData(statement.AlterMetaData);
			}
			DecreaseIndent();
			NewLine();
			Indent();
		}
		
		protected virtual void EmitAlterConstraintStatement(AlterConstraintStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Constraint, statement.ConstraintName);
			if (statement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(statement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropConstraintDefinition(DropConstraintDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Drop);
			if (statement.IsTransition)
				AppendFormat("{0} ", Keywords.Transition);
			AppendFormat("{0} {1}", Keywords.Constraint, statement.ConstraintName);
		}
		
		protected virtual void EmitDropConstraintStatement(DropConstraintStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Constraint, statement.ConstraintName);
		}
		
		protected virtual void EmitAttachStatementBase(AttachStatementBase statement)
		{
			if (statement is AttachStatement)
				EmitAttachStatement((AttachStatement)statement);
			else if (statement is InvokeStatement)
				EmitInvokeStatement((InvokeStatement)statement);
			else if (statement is DetachStatement)
				EmitDetachStatement((DetachStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitAttachStatement(AttachStatement statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Attach, statement.OperatorName, Keywords.To);
			EmitEventSourceSpecifier(statement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(statement.EventSpecifier);
			Append(" ");
			if (statement.BeforeOperatorNames.Count > 0)
			{
				AppendFormat("{0} {1} ", Keywords.Before, Keywords.BeginList);
				for (int index = 0; index < statement.BeforeOperatorNames.Count; index++)
				{
					if (index > 1)
						EmitListSeparator();
					Append(statement.BeforeOperatorNames[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitInvokeStatement(InvokeStatement statement)
		{	
			AppendFormat("{0} {1} {2} ", Keywords.Invoke, statement.OperatorName, Keywords.On);
			EmitEventSourceSpecifier(statement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(statement.EventSpecifier);
			AppendFormat("{0} {1} ", Keywords.Before, Keywords.BeginList);
			for (int index = 0; index < statement.BeforeOperatorNames.Count; index++)
			{
				if (index > 1)
					EmitListSeparator();
				Append(statement.BeforeOperatorNames[index]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitDetachStatement(DetachStatement statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Detach, statement.OperatorName, Keywords.From);
			EmitEventSourceSpecifier(statement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(statement.EventSpecifier);
		}
		
		protected virtual void EmitEventSourceSpecifier(EventSourceSpecifier statement)
		{
			if (statement is ObjectEventSourceSpecifier)
				EmitObjectEventSourceSpecifier((ObjectEventSourceSpecifier)statement);
			else if (statement is ColumnEventSourceSpecifier)
				EmitColumnEventSourceSpecifier((ColumnEventSourceSpecifier)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitObjectEventSourceSpecifier(ObjectEventSourceSpecifier statement)
		{
			Append(statement.ObjectName);
		}
		
		protected virtual void EmitColumnEventSourceSpecifier(ColumnEventSourceSpecifier statement)
		{
			AppendFormat("{0} {1} {2}", statement.ColumnName, Keywords.In, statement.TableVarName);
		}
		
		protected virtual void EmitEventSpecifier(EventSpecifier statement)
		{
			AppendFormat("{0} {1} ", Keywords.On, Keywords.BeginList);
			bool first = true;
			if ((statement.EventType & EventType.BeforeInsert) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Insert);
			}

			if ((statement.EventType & EventType.BeforeUpdate) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Update);
			}

			if ((statement.EventType & EventType.BeforeDelete) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Delete);
			}

			if ((statement.EventType & EventType.AfterInsert) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Insert);
			}

			if ((statement.EventType & EventType.AfterUpdate) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Update);
			}

			if ((statement.EventType & EventType.AfterDelete) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Delete);
			}

			if ((statement.EventType & EventType.Default) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				Append(Keywords.Default);
			}

			if ((statement.EventType & EventType.Change) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				Append(Keywords.Change);
			}

			if ((statement.EventType & EventType.Validate) != 0)
			{
				if (!first)
					EmitListSeparator();
				else
					first = false;
				Append(Keywords.Validate);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitRightStatementBase(RightStatementBase statement)
		{
			if (statement is GrantStatement)
				EmitGrantStatement((GrantStatement)statement);
			else if (statement is RevokeStatement)
				EmitRevokeStatement((RevokeStatement)statement);
			else if (statement is RevertStatement)
				EmitRevertStatement((RevertStatement)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitGrantStatement(GrantStatement statement)
		{
			AppendFormat("{0} ", Keywords.Grant);
			EmitRightSpecifier(statement);
			if (statement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(statement.Target);
			}
			AppendFormat(" {0} ", Keywords.To);
			EmitSecuritySpecifier(statement);
		}
		
		protected virtual void EmitRevokeStatement(RevokeStatement statement)
		{
			AppendFormat("{0} ", Keywords.Revoke);
			EmitRightSpecifier(statement);
			if (statement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(statement.Target);
			}
			AppendFormat(" {0} ", Keywords.From);
			EmitSecuritySpecifier(statement);
		}
		
		protected virtual void EmitRevertStatement(RevertStatement statement)
		{
			AppendFormat("{0} ", Keywords.Revert);
			EmitRightSpecifier(statement);
			if (statement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(statement.Target);
			}
			AppendFormat(" {0} ", Keywords.For);
			EmitSecuritySpecifier(statement);
		}
		
		protected virtual void EmitRightSpecifier(RightStatementBase statement)
		{
			switch (statement.RightType)
			{
				case RightSpecifierType.All : Append(Keywords.All); break;
				case RightSpecifierType.Usage : Append(Keywords.Usage); break;
				default:
					AppendFormat("{0} ", Keywords.BeginList);
					for (int index = 0; index < statement.Rights.Count; index++)
					{
						if (index > 0)
							AppendFormat("{0} ", Keywords.ListSeparator);
						Append(statement.Rights[index].RightName);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}
		}
		
		protected virtual void EmitCatalogObjectSpecifier(CatalogObjectSpecifier specifier)
		{
			if (specifier.IsOperator)
				EmitOperatorSpecifier(specifier.ObjectName, specifier.FormalParameterSpecifiers);
			else
				Append(specifier.ObjectName);
		}
		
		protected virtual void EmitSecuritySpecifier(RightStatementBase statement)
		{
			switch (statement.GranteeType)
			{
				case GranteeType.User :
					AppendFormat(@"{0} ""{1}""i", Keywords.User, statement.Grantee.Replace(@"""", @""""""));
				break;
				
				case GranteeType.Role :
					AppendFormat(@"{0} {1}", Keywords.Role, statement.Grantee);
					if (statement.IsInherited)
						AppendFormat(" {0}", Keywords.Inherited);
				break;
				
				case GranteeType.Group :
					AppendFormat(@"{0} ""{1}""i", Keywords.Group, statement.Grantee.Replace(@"""", @""""""));
					if (statement.IsInherited)
						AppendFormat(" {0}", Keywords.Inherited);
					if (statement.ApplyRecursively)
						AppendFormat(" {0} {1}", Keywords.Apply, Keywords.Recursively);
					if (statement.IncludeUsers)
						AppendFormat(" {0} {1}", Keywords.Include, Keywords.Users);
				break;
			}
		}
		
		protected virtual void EmitDefaultDefinitionBase(DefaultDefinitionBase statement)
		{
			if (statement is DefaultDefinition)
				EmitDefaultDefinition((DefaultDefinition)statement);
			else if (statement is AlterDefaultDefinition)
				EmitAlterDefaultDefinition((AlterDefaultDefinition)statement);
			else if (statement is DropDefaultDefinition)
				EmitDropDefaultDefinition((DropDefaultDefinition)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitSortDefinition(SortDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Sort);
			EmitExpression(statement.Expression);
		}
		
		protected virtual void EmitCreateSortStatement(CreateSortStatement statement)
		{
			AppendFormat("{0} {1} {2} {3} ", Keywords.Create, Keywords.Sort, statement.ScalarTypeName, Keywords.Using);
			EmitExpression(statement.Expression);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitAlterSortStatement(AlterSortStatement statement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Sort, statement.ScalarTypeName);
			if (statement.Expression != null)
			{
				AppendFormat("{0} ", Keywords.Using);
				EmitExpression(statement.Expression);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropSortStatement(DropSortStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Sort, statement.ScalarTypeName);
		}
		
		protected virtual void EmitCreateRoleStatement(CreateRoleStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Role, statement.RoleName);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitAlterRoleStatement(AlterRoleStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Role, statement.RoleName);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropRoleStatement(DropRoleStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Role, statement.RoleName);
		}
		
		protected virtual void EmitCreateRightStatement(CreateRightStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Right, statement.RightName);
		}
		
		protected virtual void EmitDropRightStatement(DropRightStatement statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Right, statement.RightName);
		}
		
		protected virtual void EmitCreateConversionStatement(CreateConversionStatement statement)
		{
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Conversion);
			EmitTypeSpecifier(statement.SourceScalarTypeName);
			AppendFormat(" {0} ", Keywords.To);
			EmitTypeSpecifier(statement.TargetScalarTypeName);
			AppendFormat(" {0} ", Keywords.Using);
			EmitIdentifierExpression(statement.OperatorName);
			AppendFormat(" {0}", statement.IsNarrowing ? Keywords.Narrowing : Keywords.Widening);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitDropConversionStatement(DropConversionStatement statement)
		{
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Conversion);
			EmitTypeSpecifier(statement.SourceScalarTypeName);
			AppendFormat(" {0} ", Keywords.To);
			EmitTypeSpecifier(statement.TargetScalarTypeName);
		}
		
		protected virtual void EmitDefaultDefinition(DefaultDefinition statement)
		{
			AppendFormat("{0} {1}", Keywords.Default, Keywords.BeginGroup);
			EmitExpression(statement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateDefaultDefinition(DefaultDefinition statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDefaultDefinition(statement);
		}
		
		protected virtual void EmitAlterDefaultDefinition(AlterDefaultDefinition statement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Default);
			if (statement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(statement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropDefaultDefinition(DropDefaultDefinition statement)
		{
			AppendFormat("{0} {1}", Keywords.Drop, Keywords.Default);
		}
		
		protected virtual void EmitClassDefinition(ClassDefinition statement)
		{
			if (statement != null)
			{
				AppendFormat(@" {0} ""{1}""", Keywords.Class, statement.ClassName.Replace(@"""", @""""""));
				
				if (statement.Attributes.Count > 0)
				{
					AppendFormat(" {0} {1} ", Keywords.Attributes, Keywords.BeginList);
					for (int index = 0; index < statement.Attributes.Count; index++)
					{
						if (index > 0)
							EmitListSeparator();
						EmitClassAttributeDefinition(statement.Attributes[index]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitAlterClassDefinition(AlterClassDefinition statement)
		{
			if (statement != null)
			{
				AppendFormat(" {0} {1}", Keywords.Alter, Keywords.Class);
				if (statement.ClassName != String.Empty)
					AppendFormat(@" ""{0}""", statement.ClassName.Replace(@"""", @""""""));
				if 
					(
						(statement.CreateAttributes.Count > 0) || 
						(statement.AlterAttributes.Count > 0) || 
						(statement.DropAttributes.Count > 0)
					)
				{
					AppendFormat(" {0} ", Keywords.BeginList);
					bool first = true;
					foreach (ClassAttributeDefinition attribute in statement.CreateAttributes)
					{
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat("{0} ", Keywords.Create);
						EmitClassAttributeDefinition(attribute);
					}
					
					foreach (ClassAttributeDefinition attribute in statement.AlterAttributes)
					{
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat("{0} ", Keywords.Alter);
						EmitClassAttributeDefinition(attribute);
					}
					
					foreach (ClassAttributeDefinition attribute in statement.DropAttributes)
					{
						if (!first)
							EmitListSeparator();
						else
							first = false;
						AppendFormat(@"{0} ""{1}""", new object[]{Keywords.Drop, attribute.AttributeName.Replace(@"""", @"""""")}); 
					}
					
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitClassAttributeDefinition(ClassAttributeDefinition statement)
		{
			AppendFormat(@"""{0}"" {1} ""{2}""", statement.AttributeName.Replace(@"""", @""""""), Keywords.Equal, statement.AttributeValue.Replace(@"""", @""""""));
		}
		
		protected virtual void EmitNamedTypeSpecifier(NamedTypeSpecifier statement)
		{
			if (statement is FormalParameter)
				EmitFormalParameter((FormalParameter)statement);
			else
			{
				AppendFormat("{0} {1} ", statement.Identifier, Keywords.TypeSpecifier);
				EmitTypeSpecifier(statement.TypeSpecifier);
			};
		}
		
		protected virtual void EmitFormalParameter(FormalParameter statement)
		{
			switch (statement.Modifier)
			{
				case Modifier.Var: AppendFormat("{0} ", Keywords.Var); break;
				case Modifier.Const: AppendFormat("{0} ", Keywords.Const); break;
			}
			AppendFormat("{0} {1} ", statement.Identifier, Keywords.TypeSpecifier);
			EmitTypeSpecifier(statement.TypeSpecifier);
		}
		
		protected virtual void EmitFormalParameterSpecifier(FormalParameterSpecifier statement)
		{
			switch (statement.Modifier)
			{
				case Modifier.Var: AppendFormat("{0} ", Keywords.Var); break;
				case Modifier.Const: AppendFormat("{0} ", Keywords.Const); break;
			}
			EmitTypeSpecifier(statement.TypeSpecifier);
		}
		
		protected void EmitOperatorSpecifier(OperatorSpecifier specifier)
		{
			EmitOperatorSpecifier(specifier.OperatorName, specifier.FormalParameterSpecifiers);
		}
		
		protected virtual void EmitOperatorSpecifier(string operatorName, FormalParameterSpecifiers formalParameterSpecifiers)
		{
			AppendFormat("{0}{1}", operatorName, Keywords.BeginGroup);
			for (int index = 0; index < formalParameterSpecifiers.Count; index++)
			{
				if (index > 0)
					EmitListSeparator();
				EmitFormalParameterSpecifier(formalParameterSpecifiers[index]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTypeSpecifier(TypeSpecifier statement)
		{
			if (statement is GenericTypeSpecifier)
				EmitGenericTypeSpecifier((GenericTypeSpecifier)statement);
			else if (statement is ScalarTypeSpecifier)
				EmitScalarTypeSpecifier((ScalarTypeSpecifier)statement);
			else if (statement is RowTypeSpecifier)
				EmitRowTypeSpecifier((RowTypeSpecifier)statement);
			else if (statement is TableTypeSpecifier)
				EmitTableTypeSpecifier((TableTypeSpecifier)statement);
			else if (statement is ListTypeSpecifier)
				EmitListTypeSpecifier((ListTypeSpecifier)statement);
			else if (statement is CursorTypeSpecifier)
				EmitCursorTypeSpecifier((CursorTypeSpecifier)statement);
			else if (statement is TypeOfTypeSpecifier)
				EmitTypeOfTypeSpecifier((TypeOfTypeSpecifier)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitGenericTypeSpecifier(GenericTypeSpecifier statement)
		{
			Append(Keywords.Generic);
		}
		
		protected virtual void EmitScalarTypeSpecifier(ScalarTypeSpecifier statement)
		{
			if (statement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Scalar);
			else
				Append(statement.ScalarTypeName);
		}
		
		protected virtual void EmitRowTypeSpecifier(RowTypeSpecifier statement)
		{
			if (statement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Row);
			else
			{
				AppendFormat("{0} {1} ", Keywords.Row, Keywords.BeginList);
				for (int index = 0; index < statement.Columns.Count; index++)
				{
					if (index > 0)
						EmitListSeparator();
					EmitNamedTypeSpecifier(statement.Columns[index]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
		}

		protected virtual void EmitTableTypeSpecifier(TableTypeSpecifier statement)
		{
			if (statement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Table);
			else
			{
				bool first = true;
				AppendFormat("{0} {1} ", Keywords.Table, Keywords.BeginList);
				for (int index = 0; index < statement.Columns.Count; index++)
				{
					if (!first)
						EmitListSeparator();
					else
						first = false;
					EmitNamedTypeSpecifier(statement.Columns[index]);
				}

				AppendFormat(" {0}", Keywords.EndList);
			}
		}

		protected virtual void EmitListTypeSpecifier(ListTypeSpecifier statement)
		{
			if (statement.IsGeneric)
				AppendFormat("{0}", Keywords.List);
			else
			{
				AppendFormat("{0}{1}", Keywords.List, Keywords.BeginGroup);
				EmitTypeSpecifier(statement.TypeSpecifier);
				Append(Keywords.EndGroup);
			}
		}
		
		protected virtual void EmitCursorTypeSpecifier(CursorTypeSpecifier statement)
		{
			if (statement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Cursor);
			else
			{
				AppendFormat("{0}{1}", Keywords.Cursor, Keywords.BeginGroup);
				EmitTypeSpecifier(statement.TypeSpecifier);
				Append(Keywords.EndGroup);
			}
		}
		
		protected virtual void EmitTypeOfTypeSpecifier(TypeOfTypeSpecifier statement)
		{
			AppendFormat("{0}{1}", Keywords.TypeOf, Keywords.BeginGroup);
			EmitExpression(statement.Expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDeviceMapItem(DeviceMapItem statement)
		{
			if (statement is DeviceScalarTypeMapBase)
				EmitDeviceScalarTypeMapBase((DeviceScalarTypeMapBase)statement);
			else if (statement is DeviceOperatorMapBase)
				EmitDeviceOperatorMapBase((DeviceOperatorMapBase)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitDeviceScalarTypeMapBase(DeviceScalarTypeMapBase statement)
		{
			if (statement is DeviceScalarTypeMap)
				EmitDeviceScalarTypeMap((DeviceScalarTypeMap)statement);
			else if (statement is AlterDeviceScalarTypeMap)
				EmitAlterDeviceScalarTypeMap((AlterDeviceScalarTypeMap)statement);
			else if (statement is DropDeviceScalarTypeMap)
				EmitDropDeviceScalarTypeMap((DropDeviceScalarTypeMap)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitDeviceScalarTypeMap(DeviceScalarTypeMap statement)
		{
			AppendFormat("{0} {1}", Keywords.Type, statement.ScalarTypeName);
			EmitClassDefinition(statement.ClassDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceScalarTypeMap(DeviceScalarTypeMap statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDeviceScalarTypeMap(statement);
		}
		
		protected virtual void EmitAlterDeviceScalarTypeMap(AlterDeviceScalarTypeMap statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Type, statement.ScalarTypeName);
			EmitAlterClassDefinition(statement.AlterClassDefinition);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropDeviceScalarTypeMap(DropDeviceScalarTypeMap statement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Type, statement.ScalarTypeName);
		}
		
		protected virtual void EmitDeviceOperatorMapBase(DeviceOperatorMapBase statement)
		{
			if (statement is DeviceOperatorMap)
				EmitDeviceOperatorMap((DeviceOperatorMap)statement);
			else if (statement is AlterDeviceOperatorMap)
				EmitAlterDeviceOperatorMap((AlterDeviceOperatorMap)statement);
			else if (statement is DropDeviceOperatorMap)
				EmitDropDeviceOperatorMap((DropDeviceOperatorMap)statement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, statement.GetType().Name);
		}
		
		protected virtual void EmitDeviceOperatorMap(DeviceOperatorMap statement)
		{
			AppendFormat("{0} ", Keywords.Operator);
			EmitOperatorSpecifier(statement.OperatorSpecifier);
			if (statement.ClassDefinition != null)
				EmitClassDefinition(statement.ClassDefinition);
			EmitMetaData(statement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceOperatorMap(DeviceOperatorMap statement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDeviceOperatorMap(statement);
		}
		
		protected virtual void EmitAlterDeviceOperatorMap(AlterDeviceOperatorMap statement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Operator);
			EmitOperatorSpecifier(statement.OperatorSpecifier);
			EmitAlterClassDefinition(statement.AlterClassDefinition);
			EmitAlterMetaData(statement.AlterMetaData);
		}
		
		protected virtual void EmitDropDeviceOperatorMap(DropDeviceOperatorMap statement)
		{
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Operator);
			EmitOperatorSpecifier(statement.OperatorSpecifier);
		}
	}
}

