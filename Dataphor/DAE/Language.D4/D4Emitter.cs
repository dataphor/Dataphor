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
			FEmitMode = EmitMode.ForCopy;
		}
		
		public D4TextEmitter(EmitMode AEmitMode) : base()
		{
			FEmitMode = AEmitMode;
		}
		
		private EmitMode FEmitMode;
		public EmitMode EmitMode
		{
			get { return FEmitMode; }
			set { FEmitMode = value; }
		}
		
		private void EmitTags(Tags ATags, bool AStatic)
		{
			if (ATags.Count > 2)
			{
				NewLine();
				IncreaseIndent();
				Indent();
				if (AStatic)
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
				if (AStatic)
					AppendFormat(" {0}", Keywords.Static);
				AppendFormat(" {0} {1} ", Keywords.Tags, Keywords.BeginList);
			}
			
			bool LFirst = true;
			#if USEHASHTABLEFORTAGS
			foreach (Tag LTag in ATags)
			{
			#else
			Tag LTag;
			for (int LIndex = 0; LIndex < ATags.Count; LIndex++)
			{
				LTag = ATags[LIndex];
			#endif
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				
				if (ATags.Count > 2)
				{
					NewLine();
					Indent();
				}
				AppendFormat(@"{0} {1} ""{2}""", LTag.Name, Keywords.Equal, LTag.Value.Replace(@"""", @"""""")); 
			}
			if (ATags.Count > 2)
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

		protected virtual void EmitMetaData(MetaData AMetaData)
		{
			if (AMetaData != null)
			{
				// Emit dynamic tags
				Tags LTags = new Tags();
				#if USEHASHTABLEFORTAGS
				foreach (Tag LTag in AMetaData.Tags)
				{
				#else
				Tag LTag;
				for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
				{
					LTag = AMetaData.Tags[LIndex];
				#endif
					if ((!LTag.IsStatic) && ((FEmitMode == EmitMode.ForRemote) || !(/*AMetaData.Tags.IsReference(LTag.Name) || */LTag.IsInherited)))
						LTags.Add(LTag);
				}
				
				if (LTags.Count > 0)
					EmitTags(LTags, false);
				
				// Emit static tags
				LTags = new Tags();
				#if USEHASHTABLEFORTAGS
				foreach (Tag LTag in AMetaData.Tags)
				{
				#else
				for (int LIndex = 0; LIndex < AMetaData.Tags.Count; LIndex++)
				{
					LTag = AMetaData.Tags[LIndex];
				#endif
					if (LTag.IsStatic)
						LTags.Add(LTag);
				}
				
				if (LTags.Count > 0)
					EmitTags(LTags, true);
			}
		}
		
		protected virtual void EmitAlterMetaData(AlterMetaData AAlterMetaData)
		{
			if (AAlterMetaData != null)
			{
				Tags LCreateTags = AAlterMetaData.CreateTags;
				Tags LAlterTags = AAlterMetaData.AlterTags;
				Tags LDropTags = AAlterMetaData.DropTags;
				if (((LCreateTags.Count > 0) || (LAlterTags.Count > 0) || (LDropTags.Count > 0)))
				{
					AppendFormat(" {0} {1} {2} ", Keywords.Alter, Keywords.Tags, Keywords.BeginList);
					bool LFirst = true;
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LCreateTags)
					{
					#else
					Tag LTag;
					for (int LIndex = 0; LIndex < LCreateTags.Count; LIndex++)
					{
						LTag = LCreateTags[LIndex];
					#endif
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat(@"{0} {1} {2} {3} ""{4}""", new object[]{Keywords.Create, LTag.IsStatic ? Keywords.Static : Keywords.Dynamic, LTag.Name, Keywords.Equal, LTag.Value.Replace(@"""", @"""""")});
					}
					
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LAlterTags)
					{
					#else
					for (int LIndex = 0; LIndex < LAlterTags.Count; LIndex++)
					{
						LTag = LAlterTags[LIndex];
					#endif
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat(@"{0} {1} {2} {3} ""{4}""", new object[]{Keywords.Alter, LTag.IsStatic ? Keywords.Static : Keywords.Dynamic, LTag.Name, Keywords.Equal, LTag.Value.Replace(@"""", @"""""")});
					}
					
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LDropTags)
					{
					#else
					for (int LIndex = 0; LIndex < LDropTags.Count; LIndex++)
					{
						LTag = LDropTags[LIndex];
					#endif
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat("{0} {1}", Keywords.Drop, LTag.Name);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitLanguageModifiers(Statement AStatement)
		{
			if ((AStatement.Modifiers != null) && (AStatement.Modifiers.Count > 0))
			{
				AppendFormat(" {0} {1} ", D4.Keywords.With, D4.Keywords.BeginList);
				for (int LIndex = 0; LIndex < AStatement.Modifiers.Count; LIndex++)
				{
					if (LIndex > 0)
						AppendFormat("{0} ", D4.Keywords.ListSeparator);
					AppendFormat(@"{0} {1} ""{2}""", AStatement.Modifiers[LIndex].Name, D4.Keywords.Equal, AStatement.Modifiers[LIndex].Value);
				}
				AppendFormat(" {0}", D4.Keywords.EndList);
			}
		}
		
		protected override void EmitExpression(Expression AExpression)
		{
			if ((AExpression.Modifiers != null) && (AExpression.Modifiers.Count > 0))
				Append(Keywords.BeginGroup);

			bool LEmitModifiers = true;
			if (AExpression is QualifierExpression)
				EmitQualifierExpression((QualifierExpression)AExpression);
			else if (AExpression is AsExpression)
				EmitAsExpression((AsExpression)AExpression);
			else if (AExpression is IsExpression)
				EmitIsExpression((IsExpression)AExpression);
			#if CALCULESQUE
			else if (AExpression is NamedExpression)
				EmitNamedExpression((NamedExpression)AExpression);
			#endif
			else if (AExpression is ParameterExpression)
				EmitParameterExpression((ParameterExpression)AExpression);
			else if (AExpression is D4IndexerExpression)
				EmitIndexerExpression((D4IndexerExpression)AExpression);
			else if (AExpression is IfExpression)
				EmitIfExpression((IfExpression)AExpression);
			else if (AExpression is RowSelectorExpressionBase)
				EmitRowSelectorExpressionBase((RowSelectorExpressionBase)AExpression);
			else if (AExpression is RowExtractorExpressionBase)
				EmitRowExtractorExpressionBase((RowExtractorExpressionBase)AExpression);
			else if (AExpression is ColumnExtractorExpression)
				EmitColumnExtractorExpression((ColumnExtractorExpression)AExpression);
			else if (AExpression is ListSelectorExpression)
				EmitListSelectorExpression((ListSelectorExpression)AExpression);
			else if (AExpression is CursorSelectorExpression)
				EmitCursorSelectorExpression((CursorSelectorExpression)AExpression);
			else if (AExpression is CursorDefinition)
				EmitCursorDefinition((CursorDefinition)AExpression);
			else if (AExpression is ExplodeColumnExpression)
				EmitExplodeColumnExpression((ExplodeColumnExpression)AExpression);
			else if (AExpression is AdornExpression)
			{
				EmitAdornExpression((AdornExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is RedefineExpression)
			{
				EmitRedefineExpression((RedefineExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is OnExpression)
			{
				EmitOnExpression((OnExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is RenameAllExpression)
			{
				EmitRenameAllExpression((RenameAllExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is TableSelectorExpressionBase)
				EmitTableSelectorExpressionBase((TableSelectorExpressionBase)AExpression);
			else if (AExpression is RestrictExpression)
			{
				EmitRestrictExpression((RestrictExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is ProjectExpression)
			{
				EmitProjectExpression((ProjectExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is RemoveExpression)
			{
				EmitRemoveExpression((RemoveExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is ExtendExpression)
			{
				EmitExtendExpression((ExtendExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is SpecifyExpression)
			{
				EmitSpecifyExpression((SpecifyExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is RenameExpression)
			{
				EmitRenameExpression((RenameExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is AggregateExpression)
			{
				EmitAggregateExpression((AggregateExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is BaseOrderExpression)
				EmitBaseOrderExpression((BaseOrderExpression)AExpression);
			else if (AExpression is QuotaExpression)
			{
				EmitQuotaExpression((QuotaExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is ExplodeExpression)
			{
				EmitExplodeExpression((ExplodeExpression)AExpression);
				LEmitModifiers = false;
			}
			else if (AExpression is BinaryTableExpression)
			{
				EmitBinaryTableExpression((BinaryTableExpression)AExpression);
				LEmitModifiers = false;
			}
			else
				base.EmitExpression(AExpression);

			if (LEmitModifiers)
				EmitLanguageModifiers(AExpression);

			if ((AExpression.Modifiers != null) && (AExpression.Modifiers.Count > 0))
				Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitQualifierExpression(QualifierExpression AExpression)
		{
			EmitExpression(AExpression.LeftExpression);
			Append(Keywords.Qualifier);
			EmitExpression(AExpression.RightExpression);
		}
		
		protected virtual void EmitParameterExpression(ParameterExpression AExpression)
		{
			if (AExpression.Modifier == Modifier.Var)
				AppendFormat("{0} ", Keywords.Var);
			EmitExpression(AExpression.Expression);
		}
		
		protected virtual void EmitIndexerExpression(D4IndexerExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			Append(Keywords.BeginIndexer);
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			
			if (AExpression.HasByClause)
			{
				AppendFormat(" {0} {1} ", Keywords.By, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.ByClause.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitKeyColumnDefinition(AExpression.ByClause[LIndex]);
				}
				if (AExpression.ByClause.Count > 0)
					AppendFormat(" {0}", Keywords.EndList);
				else
					AppendFormat("{0}", Keywords.EndList);
			}
			
			Append(Keywords.EndIndexer);
		}
		
		protected virtual void EmitIfExpression(IfExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			AppendFormat("{0} ", Keywords.If);
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0} ", Keywords.Then);
			EmitExpression(AExpression.TrueExpression);
			AppendFormat(" {0} ", Keywords.Else);
			EmitExpression(AExpression.FalseExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAdornExpression(AdornExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Adorn);
			NewLine();
			Indent();
			if 
			(
				(AExpression.Expressions.Count > 0) || 
				(AExpression.Constraints.Count > 0) || 
				(AExpression.Orders.Count > 0) || 
				(AExpression.AlterOrders.Count > 0) ||
				(AExpression.DropOrders.Count > 0) ||
				(AExpression.Keys.Count > 0) || 
				(AExpression.AlterKeys.Count > 0) ||
				(AExpression.DropKeys.Count > 0) ||
				(AExpression.References.Count > 0) ||
				(AExpression.AlterReferences.Count > 0) ||
				(AExpression.DropReferences.Count > 0)
			)
			{
				Append(Keywords.BeginList);
				IncreaseIndent();																   
				bool LFirst = true;
				for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAdornColumnExpression(AExpression.Expressions[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(AExpression.Constraints[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.Orders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitOrderDefinition(AExpression.Orders[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.AlterOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(AExpression.AlterOrders[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.DropOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(AExpression.DropOrders[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.Keys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitKeyDefinition(AExpression.Keys[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.AlterKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(AExpression.AlterKeys[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.DropKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(AExpression.DropKeys[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.References.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(AExpression.References[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.AlterReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(AExpression.AlterReferences[LIndex]);
				}
				for (int LIndex = 0; LIndex < AExpression.DropReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(AExpression.DropReferences[LIndex]);
				}
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(AExpression.MetaData);
			EmitAlterMetaData(AExpression.AlterMetaData);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAdornColumnExpression(AdornColumnExpression AExpression)
		{
			Append(AExpression.ColumnName);
			
			if (AExpression.ChangeNilable)
			{
				if (AExpression.IsNilable)
					AppendFormat(" {0}", Keywords.Nil);
				else
					AppendFormat(" {0} {1}", Keywords.Not, Keywords.Nil);
			}

			bool LFirst = true;			
			if ((AExpression.Default != null) || (AExpression.Constraints.Count > 0))
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				
				if (AExpression.Default != null)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;

					NewLine();
					Indent();
					EmitDefaultDefinition(AExpression.Default);
				}
					
				for (int LIndex = 0; LIndex < AExpression.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(AExpression.Constraints[LIndex]);
				}
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(AExpression.MetaData);
			EmitAlterMetaData(AExpression.AlterMetaData);
		}
		
		protected virtual void EmitRedefineExpression(RedefineExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Redefine);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitRedefineColumnExpression(AExpression.Expressions[LIndex]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRedefineColumnExpression(NamedColumnExpression AExpression)
		{
			AppendFormat("{0} {1} ", AExpression.ColumnAlias, Keywords.Assign);
			EmitExpression(AExpression.Expression);
		}
		
		protected virtual void EmitOnExpression(OnExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.On, AExpression.ServerName);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRenameAllExpression(RenameAllExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Rename, AExpression.Identifier);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitIsExpression(IsExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0} ", Keywords.Is);
			EmitTypeSpecifier(AExpression.TypeSpecifier);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAsExpression(AsExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0} ", Keywords.As);
			EmitTypeSpecifier(AExpression.TypeSpecifier);
			Append(Keywords.EndGroup);
		}
		
		#if CALCULESQUE
		protected virtual void EmitNamedExpression(NamedExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0}", AExpression.Name);
		}
		#endif
		
		protected virtual void EmitTableSelectorExpressionBase(TableSelectorExpressionBase AExpression)
		{
			Append(Keywords.Table);
			if (AExpression.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.Of);
				if (AExpression.TypeSpecifier is TableTypeSpecifier)
				{
					TableTypeSpecifier LTypeSpecifier = (TableTypeSpecifier)AExpression.TypeSpecifier;
					AppendFormat("{0} ", Keywords.BeginList);
					for (int LIndex = 0; LIndex < LTypeSpecifier.Columns.Count; LIndex++)
					{
						if (LIndex > 0)
							EmitListSeparator();
						EmitNamedTypeSpecifier(LTypeSpecifier.Columns[LIndex]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
				else
					EmitTypeSpecifier(AExpression.TypeSpecifier);
			}
			
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			
			for (int LIndex = 0; LIndex < AExpression.Keys.Count; LIndex++)
			{
				if ((AExpression.Expressions.Count > 0) || (LIndex > 0))
					EmitListSeparator();
				NewLine();
				Indent();
				EmitKeyDefinition(AExpression.Keys[LIndex]);
			}

			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
		}
		
		protected virtual void EmitRowSelectorExpressionBase(RowSelectorExpressionBase AExpression)
		{
			Append(Keywords.Row);

			if (AExpression.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.Of);
				if (AExpression.TypeSpecifier is RowTypeSpecifier)
				{
					RowTypeSpecifier LTypeSpecifier = (RowTypeSpecifier)AExpression.TypeSpecifier;
					AppendFormat("{0} ", Keywords.BeginList);
					for (int LIndex = 0; LIndex < LTypeSpecifier.Columns.Count; LIndex++)
					{
						if (LIndex > 0)
							EmitListSeparator();
						EmitNamedTypeSpecifier(LTypeSpecifier.Columns[LIndex]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
				else
					EmitTypeSpecifier(AExpression.TypeSpecifier);
			}
			
			AppendFormat(" {0} ", Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitNamedColumnExpression(AExpression.Expressions[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitRowExtractorExpressionBase(RowExtractorExpressionBase AExpression)
		{
			Append(Keywords.BeginGroup);
			AppendFormat("{0} {1} ", Keywords.Row, Keywords.From);
			EmitExpression(AExpression.Expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnExtractorExpression(ColumnExtractorExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			if (AExpression.Columns.Count == 1)
				AppendFormat("{0} {1} ", AExpression.Columns[0].ColumnName, Keywords.From);
			else
			{
				AppendFormat("{0} ", Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
				{
					if (LIndex > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					Append(AExpression.Columns[LIndex].ColumnName);
				}
				AppendFormat(" {0} {1} ", Keywords.EndList, Keywords.From);
			}
			EmitExpression(AExpression.Expression);
			
			if (AExpression.HasByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.OrderColumns.Count; LIndex++)
				{
					if (LIndex > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(AExpression.OrderColumns[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitListSelectorExpression(ListSelectorExpression AExpression)
		{
			if (AExpression.TypeSpecifier != null)
				EmitListTypeSpecifier((ListTypeSpecifier)AExpression.TypeSpecifier);
			AppendFormat("{0} ", Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitExpression(AExpression.Expressions[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitCursorDefinition(CursorDefinition AExpression)
		{
			EmitExpression(AExpression.Expression);
			if (AExpression.Capabilities != 0)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Capabilities, Keywords.BeginList);
				bool LFirst = true;
				CursorCapability LCapability;
				for (int LIndex = 0; LIndex < 7; LIndex++)
				{
					LCapability = (CursorCapability)Math.Pow(2, LIndex);
					if ((AExpression.Capabilities & LCapability) != 0)
					{
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
							
						Append(LCapability.ToString().ToLower());
					}
				}
				AppendFormat(" {0}", Keywords.EndList);
			}

			if (AExpression.Isolation != CursorIsolation.None)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1}", Keywords.Isolation, AExpression.Isolation.ToString().ToLower());
			}
			
			if (AExpression.SpecifiesType)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1}", Keywords.Type, AExpression.CursorType.ToString().ToLower());
			}
		}
		
		protected virtual void EmitCursorSelectorExpression(CursorSelectorExpression AExpression)
		{
			Append(Keywords.Cursor);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			EmitCursorDefinition(AExpression.CursorDefinition);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRestrictExpression(RestrictExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Where);
			EmitExpression(AExpression.Condition);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitColumnExpression(ColumnExpression AExpression)
		{
			Append(AExpression.ColumnName);
		}
		
		protected virtual void EmitProjectExpression(ProjectExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} ", Keywords.Over, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitColumnExpression(AExpression.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRemoveExpression(RemoveExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} ", Keywords.Remove, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitColumnExpression(AExpression.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitNamedColumnExpression(NamedColumnExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			AppendFormat(" {0}", AExpression.ColumnAlias);
			EmitMetaData(AExpression.MetaData);
		}
		
		protected virtual void EmitExtendExpression(ExtendExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Add);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitNamedColumnExpression(AExpression.Expressions[LIndex]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitSpecifyExpression(SpecifyExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitNamedColumnExpression(AExpression.Expressions[LIndex]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRenameColumnExpression(RenameColumnExpression AExpression)
		{
			AppendFormat("{0} {1}", AExpression.ColumnName, AExpression.ColumnAlias);
			EmitMetaData(AExpression.MetaData);
		}
		
		protected virtual void EmitRenameExpression(RenameExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Rename);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AExpression.Expressions.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitRenameColumnExpression(AExpression.Expressions[LIndex]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitAggregateColumnExpression(AggregateColumnExpression AExpression)
		{
			AppendFormat("{0}{1}", AExpression.AggregateOperator, Keywords.BeginGroup);
			if (AExpression.Distinct)
				AppendFormat("{0} ", Keywords.Distinct);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					AppendFormat("{0} ", Keywords.ListSeparator);
				Append(AExpression.Columns[LIndex].ColumnName);
			}

			if (AExpression.HasByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.OrderColumns.Count; LIndex++)
				{
					if (LIndex > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(AExpression.OrderColumns[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			AppendFormat("{0} {1}", Keywords.EndGroup, AExpression.ColumnAlias);
			EmitMetaData(AExpression.MetaData);
		}
		
		protected virtual void EmitAggregateExpression(AggregateExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0}", Keywords.Group);
			IncreaseIndent();
			if (AExpression.ByColumns.Count > 0)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.By, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.ByColumns.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitColumnExpression(AExpression.ByColumns[LIndex]);
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
			for (int LIndex = 0; LIndex < AExpression.ComputeColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitAggregateColumnExpression(AExpression.ComputeColumns[LIndex]);
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
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitBaseOrderExpression(BaseOrderExpression AExpression)
		{
			if (AExpression is OrderExpression)
				EmitOrderExpression((OrderExpression)AExpression);
			else if (AExpression is BrowseExpression)
				EmitBrowseExpression((BrowseExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
		}
		
		protected virtual void EmitOrderExpression(OrderExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AExpression.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitIncludeColumnExpression(AExpression.SequenceColumn, Schema.TableVarColumnType.Sequence);
			DecreaseIndent();
		}
		
		protected virtual void EmitBrowseExpression(BrowseExpression AExpression)
		{
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1} {2} ", Keywords.Browse, Keywords.By, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AExpression.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
		}

		protected virtual void EmitQuotaExpression(QuotaExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Return);
			EmitExpression(AExpression.Quota);
			AppendFormat(" {0} {1} ", Keywords.By, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AExpression.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AExpression.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitExplodeColumnExpression(ExplodeColumnExpression AExpression)
		{
			AppendFormat("{0}{1}{2}", Keywords.Parent, Keywords.Qualifier, AExpression.ColumnName);
		}
		
		protected virtual void EmitIncludeColumnExpression(IncludeColumnExpression AExpression, Schema.TableVarColumnType AColumnType)
		{
			if (AExpression != null)
			{
				AppendFormat(" {0}", Keywords.Include);
				switch (AColumnType)
				{
					case Schema.TableVarColumnType.Sequence: AppendFormat(" {0}", Keywords.Sequence); break;
					case Schema.TableVarColumnType.Level: AppendFormat(" {0}", Keywords.Level); break;
					case Schema.TableVarColumnType.RowExists: AppendFormat(" {0}", Keywords.RowExists); break;
				}
				if (AExpression.ColumnAlias != String.Empty)
					AppendFormat(" {0}", AExpression.ColumnAlias);
				EmitMetaData(AExpression.MetaData);
			}
		}
		
		protected virtual void EmitExplodeExpression(ExplodeExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.Expression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Explode);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.By);
			EmitExpression(AExpression.ByExpression);
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.Where);
			EmitExpression(AExpression.RootExpression);

			if (AExpression.HasOrderByClause)
			{
				AppendFormat(" {0} {1} {2} ", Keywords.Order, Keywords.By, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AExpression.OrderColumns.Count; LIndex++)
				{
					if (LIndex > 0)
						AppendFormat("{0} ", Keywords.ListSeparator);
					EmitOrderColumnDefinition(AExpression.OrderColumns[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			
			if (AExpression.LevelColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(AExpression.LevelColumn, Schema.TableVarColumnType.Level);
			}
			
			if (AExpression.SequenceColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(AExpression.SequenceColumn, Schema.TableVarColumnType.Sequence);
			}

			DecreaseIndent();
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitBinaryTableExpression(BinaryTableExpression AExpression)
		{
			if (AExpression is JoinExpression)
				EmitJoinExpression((JoinExpression)AExpression);
			else if (AExpression is HavingExpression)
				EmitHavingExpression((HavingExpression)AExpression);
			else if (AExpression is WithoutExpression)
				EmitWithoutExpression((WithoutExpression)AExpression);
			else if (AExpression is UnionExpression)
				EmitUnionExpression((UnionExpression)AExpression);
			else if (AExpression is IntersectExpression)
				EmitIntersectExpression((IntersectExpression)AExpression);
			else if (AExpression is DifferenceExpression)
				EmitDifferenceExpression((DifferenceExpression)AExpression);
			else if (AExpression is ProductExpression)
				EmitProductExpression((ProductExpression)AExpression);
			else if (AExpression is DivideExpression)
				EmitDivideExpression((DivideExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
		}
		
		protected virtual void EmitUnionExpression(UnionExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			EmitExpression(AExpression.LeftExpression);
			AppendFormat(" {0} ", Keywords.Union);
			EmitExpression(AExpression.RightExpression);
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitIntersectExpression(IntersectExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Intersect);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDifferenceExpression(DifferenceExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Minus);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitProductExpression(ProductExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Times);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDivideExpression(DivideExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Divide);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitJoinExpression(JoinExpression AExpression)
		{
			if (AExpression is OuterJoinExpression)
				EmitOuterJoinExpression((OuterJoinExpression)AExpression);
			else if (AExpression is InnerJoinExpression)
				EmitInnerJoinExpression((InnerJoinExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
		}
		
		protected virtual void EmitHavingExpression(HavingExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Having);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			if (AExpression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(AExpression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitWithoutExpression(WithoutExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Without);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			if (AExpression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(AExpression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitInnerJoinExpression(InnerJoinExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			Append(AExpression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);
			if (AExpression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(AExpression.Condition);
			}
			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitOuterJoinExpression(OuterJoinExpression AExpression)
		{
			if (AExpression is LeftOuterJoinExpression)
				EmitLeftOuterJoinExpression((LeftOuterJoinExpression)AExpression);
			else if (AExpression is RightOuterJoinExpression)
				EmitRightOuterJoinExpression((RightOuterJoinExpression)AExpression);
			else
				throw new LanguageException(LanguageException.Codes.UnknownExpressionClass, AExpression.GetType().Name);
		}
		
		protected virtual void EmitLeftOuterJoinExpression(LeftOuterJoinExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Left, AExpression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);

			if (AExpression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(AExpression.Condition);
			}

			if (AExpression.RowExistsColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(AExpression.RowExistsColumn, Schema.TableVarColumnType.RowExists);
			}

			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitRightOuterJoinExpression(RightOuterJoinExpression AExpression)
		{
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AExpression.LeftExpression);
			IncreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} {1}", Keywords.Right, AExpression.IsLookup ? Keywords.Lookup : Keywords.Join);
			NewLine();
			Indent();
			EmitExpression(AExpression.RightExpression);

			if (AExpression.Condition != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.By);
				EmitExpression(AExpression.Condition);
			}

			if (AExpression.RowExistsColumn != null)
			{
				NewLine();
				Indent();
				EmitIncludeColumnExpression(AExpression.RowExistsColumn, Schema.TableVarColumnType.RowExists);
			}

			DecreaseIndent();
			DecreaseIndent();
			NewLine();
			Indent();
			EmitLanguageModifiers(AExpression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTerminatedStatement(Statement AStatement)
		{
			NewLine();
			Indent();
			EmitStatement(AStatement);
			EmitStatementTerminator();
		}
		
		protected override void EmitStatement(Statement AStatement)
		{
			if (AStatement is D4Statement)
				EmitD4Statement((D4Statement)AStatement);
			else if (AStatement is AssignmentStatement)
				EmitAssignmentStatement((AssignmentStatement)AStatement);
			else if (AStatement is VariableStatement)
				EmitVariableStatement((VariableStatement)AStatement);
			else if (AStatement is ExpressionStatement)
				EmitExpressionStatement((ExpressionStatement)AStatement);
			else if (AStatement is IfStatement)
				EmitIfStatement((IfStatement)AStatement);
			else if (AStatement is DelimitedBlock)
				EmitDelimitedBlock((DelimitedBlock)AStatement);
			else if (AStatement is Block)
				EmitBlock((Block)AStatement);
			else if (AStatement is ExitStatement)
				EmitExitStatement((ExitStatement)AStatement);
			else if (AStatement is WhileStatement)
				EmitWhileStatement((WhileStatement)AStatement);
			else if (AStatement is DoWhileStatement)
				EmitDoWhileStatement((DoWhileStatement)AStatement);
			else if (AStatement is ForEachStatement)
				EmitForEachStatement((ForEachStatement)AStatement);
			else if (AStatement is BreakStatement)
				EmitBreakStatement((BreakStatement)AStatement);
			else if (AStatement is ContinueStatement)
				EmitContinueStatement((ContinueStatement)AStatement);
			else if (AStatement is CaseStatement)
				EmitCaseStatement((CaseStatement)AStatement);
			else if (AStatement is RaiseStatement)
				EmitRaiseStatement((RaiseStatement)AStatement);
			else if (AStatement is TryFinallyStatement)
				EmitTryFinallyStatement((TryFinallyStatement)AStatement);
			else if (AStatement is TryExceptStatement)
				EmitTryExceptStatement((TryExceptStatement)AStatement);
			else if (AStatement is EmptyStatement)
				EmitEmptyStatement((EmptyStatement)AStatement);
			else if (AStatement is SourceStatement)
				EmitSourceStatement((SourceStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitAssignmentStatement(AssignmentStatement AStatement)
		{
			EmitExpression(AStatement.Target);
			AppendFormat(" {0} ", Keywords.Assign);
			EmitExpression(AStatement.Expression);
		}
		
		protected virtual void EmitVariableStatement(VariableStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Var);
			EmitIdentifierExpression(AStatement.VariableName);
			if (AStatement.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.TypeSpecifier);
			}

			if (AStatement.Expression != null)
			{
				AppendFormat(" {0} ", Keywords.Assign);
				EmitExpression(AStatement.Expression);
			}
		}
		
		protected virtual void EmitExpressionStatement(ExpressionStatement AStatement)
		{
			EmitExpression(AStatement.Expression);
		}
		
		protected virtual void EmitIfStatement(IfStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.If);
			EmitExpression(AStatement.Expression);
			AppendFormat(" {0}", Keywords.Then);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitStatement(AStatement.TrueStatement);
			DecreaseIndent();

			if (AStatement.FalseStatement != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Else);
				IncreaseIndent();
				NewLine();
				Indent();
				EmitStatement(AStatement.FalseStatement);
				DecreaseIndent();
			}
		}
		
		protected virtual void EmitDelimitedBlock(DelimitedBlock AStatement)
		{
			Append(Keywords.Begin);
			IncreaseIndent();
			EmitBlock(AStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitBlock(Block AStatement)
		{
			for (int LIndex = 0; LIndex < AStatement.Statements.Count; LIndex++)
				EmitTerminatedStatement(AStatement.Statements[LIndex]);
		}
		
		protected virtual void EmitExitStatement(ExitStatement AStatement)
		{
			Append(Keywords.Exit);
		}
		
		protected virtual void EmitWhileStatement(WhileStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.While);
			EmitExpression(AStatement.Condition);
			AppendFormat(" {0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.Statement);
			DecreaseIndent();
		}
		
		protected virtual void EmitDoWhileStatement(DoWhileStatement AStatement)
		{
			AppendFormat("{0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.Statement);
			DecreaseIndent();
			NewLine();
			Indent();
			AppendFormat("{0} ", Keywords.While);
			EmitExpression(AStatement.Condition);
		}
		
		protected virtual void EmitForEachStatement(ForEachStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.ForEach);
			if (AStatement.VariableName == String.Empty)
				AppendFormat("{0} ", Keywords.Row);
			else
			{
				if (AStatement.IsAllocation)
					AppendFormat("{0} ", Keywords.Var);
				AppendFormat("{0} ", AStatement.VariableName);
			}
			AppendFormat("{0} ", Keywords.In);
			EmitCursorDefinition(AStatement.Expression);
			AppendFormat(" {0}", Keywords.Do);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.Statement);
			DecreaseIndent();
		}
		
		protected virtual void EmitBreakStatement(BreakStatement AStatement)
		{
			Append(Keywords.Break);
		}
		
		protected virtual void EmitContinueStatement(ContinueStatement AStatement)
		{
			Append(Keywords.Continue);
		}
		
		protected virtual void EmitCaseStatement(CaseStatement AStatement)
		{
			Append(Keywords.Case);
			if (AStatement.Expression != null)
			{
				Append(" ");
				EmitExpression(AStatement.Expression);
			}
			IncreaseIndent();

			for (int LIndex = 0; LIndex < AStatement.CaseItems.Count; LIndex++)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.When);
				EmitExpression(AStatement.CaseItems[LIndex].WhenExpression);
				AppendFormat(" {0} ", Keywords.Then);
				EmitTerminatedStatement(AStatement.CaseItems[LIndex].ThenStatement);
			}
			
			if (AStatement.ElseStatement != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Else);
				EmitTerminatedStatement(AStatement.ElseStatement);
			}
			
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitRaiseStatement(RaiseStatement AStatement)
		{
			Append(Keywords.Raise);
			if (AStatement.Expression != null)
			{
				Append(" ");
				EmitExpression(AStatement.Expression);
			}
		}
		
		protected virtual void EmitTryFinallyStatement(TryFinallyStatement AStatement)
		{
			Append(Keywords.Try);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.TryStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Finally);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.FinallyStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitTryExceptStatement(TryExceptStatement AStatement)
		{
			Append(Keywords.Try);
			IncreaseIndent();
			EmitTerminatedStatement(AStatement.TryStatement);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.Except);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AStatement.ErrorHandlers.Count; LIndex++)
				EmitErrorHandler(AStatement.ErrorHandlers[LIndex]);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.End);
		}
		
		protected virtual void EmitErrorHandler(GenericErrorHandler AStatement)
		{
			if (AStatement is SpecificErrorHandler)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.On);
				if (AStatement is ParameterizedErrorHandler)
					AppendFormat("{0} {1} ", ((ParameterizedErrorHandler)AStatement).VariableName, Keywords.TypeSpecifier);
				AppendFormat("{0} ", ((SpecificErrorHandler)AStatement).ErrorTypeName);
				AppendFormat("{0} ", Keywords.Do);
				IncreaseIndent();
			}
			EmitTerminatedStatement(AStatement.Statement);
			if (AStatement is SpecificErrorHandler)
			{
				DecreaseIndent();
			}
		}
		
		protected virtual void EmitEmptyStatement(Statement AStatement)
		{
		}
		
		protected virtual void EmitSourceStatement(SourceStatement AStatement)
		{
			Append(AStatement.Source);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitD4Statement(Statement AStatement)
		{
			if (AStatement is D4DMLStatement)
				EmitD4DMLStatement((D4DMLStatement)AStatement);
			else if (AStatement is CreateTableVarStatement)
				EmitCreateTableVarStatement((CreateTableVarStatement)AStatement);
			else if (AStatement is CreateScalarTypeStatement)
				EmitCreateScalarTypeStatement((CreateScalarTypeStatement)AStatement);
			else if (AStatement is CreateOperatorStatementBase)
				EmitCreateOperatorStatementBase((CreateOperatorStatementBase)AStatement);
			else if (AStatement is CreateServerStatement)
				EmitCreateServerStatement((CreateServerStatement)AStatement);
			else if (AStatement is CreateDeviceStatement)
				EmitCreateDeviceStatement((CreateDeviceStatement)AStatement);
			else if (AStatement is AlterTableVarStatement)
				EmitAlterTableVarStatement((AlterTableVarStatement)AStatement);
			else if (AStatement is AlterScalarTypeStatement)
				EmitAlterScalarTypeStatement((AlterScalarTypeStatement)AStatement);
			else if (AStatement is AlterOperatorStatementBase)
				EmitAlterOperatorStatementBase((AlterOperatorStatementBase)AStatement);
			else if (AStatement is AlterServerStatement)
				EmitAlterServerStatement((AlterServerStatement)AStatement);
			else if (AStatement is AlterDeviceStatement)
				EmitAlterDeviceStatement((AlterDeviceStatement)AStatement);
			else if (AStatement is DropObjectStatement)
				EmitDropObjectStatement((DropObjectStatement)AStatement);
			else if (AStatement is CreateReferenceStatement)
				EmitCreateReferenceStatement((CreateReferenceStatement)AStatement);
			else if (AStatement is AlterReferenceStatement)
				EmitAlterReferenceStatement((AlterReferenceStatement)AStatement);
			else if (AStatement is DropReferenceStatement)
				EmitDropReferenceStatement((DropReferenceStatement)AStatement);
			else if (AStatement is CreateConstraintStatement)
				EmitCreateConstraintStatement((CreateConstraintStatement)AStatement);
			else if (AStatement is AlterConstraintStatement)
				EmitAlterConstraintStatement((AlterConstraintStatement)AStatement);
			else if (AStatement is DropConstraintStatement)
				EmitDropConstraintStatement((DropConstraintStatement)AStatement);
			else if (AStatement is CreateSortStatement)
				EmitCreateSortStatement((CreateSortStatement)AStatement);
			else if (AStatement is AlterSortStatement)
				EmitAlterSortStatement((AlterSortStatement)AStatement);
			else if (AStatement is DropSortStatement)
				EmitDropSortStatement((DropSortStatement)AStatement);
			else if (AStatement is CreateConversionStatement)
				EmitCreateConversionStatement((CreateConversionStatement)AStatement);
			else if (AStatement is DropConversionStatement)
				EmitDropConversionStatement((DropConversionStatement)AStatement);
			else if (AStatement is CreateRoleStatement)
				EmitCreateRoleStatement((CreateRoleStatement)AStatement);
			else if (AStatement is AlterRoleStatement)
				EmitAlterRoleStatement((AlterRoleStatement)AStatement);
			else if (AStatement is DropRoleStatement)
				EmitDropRoleStatement((DropRoleStatement)AStatement);
			else if (AStatement is CreateRightStatement)
				EmitCreateRightStatement((CreateRightStatement)AStatement);
			else if (AStatement is DropRightStatement)
				EmitDropRightStatement((DropRightStatement)AStatement);
			else if (AStatement is AttachStatementBase)
				EmitAttachStatementBase((AttachStatementBase)AStatement);
			else if (AStatement is RightStatementBase)
				EmitRightStatementBase((RightStatementBase)AStatement);
			else if (AStatement is TypeSpecifier)
				EmitTypeSpecifier((TypeSpecifier)AStatement); // Not part of the grammer proper, used to emit type specifiers explicitly
			else if (AStatement is KeyDefinition)
				EmitKeyDefinition((KeyDefinition)AStatement); // ditto
			else if (AStatement is OrderDefinition)
				EmitOrderDefinition((OrderDefinition)AStatement); // ditto
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitD4DMLStatement(Statement AStatement)
		{
			if (AStatement is SelectStatement)
				EmitSelectStatement((SelectStatement)AStatement);
			else if (AStatement is InsertStatement)
				EmitInsertStatement((InsertStatement)AStatement);
			else if (AStatement is UpdateStatement)
				EmitUpdateStatement((UpdateStatement)AStatement);
			else if (AStatement is DeleteStatement)
				EmitDeleteStatement((DeleteStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}

		protected virtual void EmitSelectStatement(SelectStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Select);
			EmitCursorDefinition(AStatement.CursorDefinition);
		}
		
		protected virtual void EmitInsertStatement(InsertStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Insert);
			EmitLanguageModifiers(AStatement);
			EmitExpression(AStatement.SourceExpression);
			AppendFormat(" {0} ", Keywords.Into);
			EmitExpression(AStatement.Target);
		}
		
		protected virtual void EmitUpdateStatement(UpdateStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Update);
			EmitLanguageModifiers(AStatement);
			EmitExpression(AStatement.Target);
			AppendFormat(" {0} {1} ", Keywords.Set, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitUpdateColumnExpression(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			if (AStatement.Condition != null)
			{
				AppendFormat(" {0} ", Keywords.Where);
				EmitExpression(AStatement.Condition);
			}
		}
		
		protected virtual void EmitUpdateColumnExpression(UpdateColumnExpression AExpression)
		{
			EmitExpression(AExpression.Target);
			AppendFormat(" {0} ", Keywords.Assign);
			EmitExpression(AExpression.Expression);
		}
		
		protected virtual void EmitDeleteStatement(DeleteStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Delete);
			EmitLanguageModifiers(AStatement);
			EmitExpression(AStatement.Target);
		}
		
		protected virtual void EmitCreateTableVarStatement(CreateTableVarStatement AStatement)
		{
			if (AStatement is CreateTableStatement)
				EmitCreateTableStatement((CreateTableStatement)AStatement);
			else if (AStatement is CreateViewStatement)
				EmitCreateViewStatement((CreateViewStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitCreateTableStatement(CreateTableStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}", Keywords.Table, AStatement.TableVarName);
			if (AStatement.DeviceName != null)
			{
				AppendFormat(" {0} ", Keywords.In);
				EmitIdentifierExpression(AStatement.DeviceName);
			}
			if (AStatement.FromExpression != null)
			{
				AppendFormat(" {0} ", Keywords.From);
				EmitExpression(AStatement.FromExpression);
			}
			else
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitColumnDefinition(AStatement.Columns[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Keys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitKeyDefinition(AStatement.Keys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Orders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitOrderDefinition(AStatement.Orders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(AStatement.Constraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.References.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(AStatement.References[LIndex]);
				}
				
				NewLine();
				Indent();
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateViewStatement(CreateViewStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}", Keywords.View, AStatement.TableVarName);
			NewLine();
			Indent();
			Append(Keywords.BeginGroup);
			IncreaseIndent();
			NewLine();
			Indent();
			EmitExpression(AStatement.Expression);
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndGroup);
			NewLine();
			Indent();
			if 
				(
					(AStatement.Keys.Count > 0) || 
					(AStatement.Orders.Count > 0) || 
					(AStatement.Constraints.Count > 0) || 
					(AStatement.References.Count > 0)
				)
			{
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;

				for (int LIndex = 0; LIndex < AStatement.Keys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitKeyDefinition(AStatement.Keys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Orders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitOrderDefinition(AStatement.Orders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(AStatement.Constraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.References.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitReferenceDefinition(AStatement.References[LIndex]);
				}

				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateScalarTypeStatement(CreateScalarTypeStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Type, AStatement.ScalarTypeName);

			#if ALLOWSUBTYPES
			if (AStatement.ParentScalarTypes.Count > 0)
			{
				AppendFormat(" {0} {1} ", Keywords.Is, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AStatement.ParentScalarTypes.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitScalarTypeNameDefinition(AStatement.ParentScalarTypes[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			#endif

			if (AStatement.LikeScalarTypeName != String.Empty)
				AppendFormat(" {0} {1}", Keywords.Like, AStatement.LikeScalarTypeName);

			if 
				(
					(AStatement.Default != null) || 
					(AStatement.Constraints.Count > 0) || 
					(AStatement.Representations.Count > 0) || 
					(AStatement.Specials.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				if (AStatement.Default != null)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDefaultDefinition(AStatement.Default);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(AStatement.Constraints[LIndex]);
				}

				for (int LIndex = 0; LIndex < AStatement.Representations.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitRepresentationDefinition(AStatement.Representations[LIndex]);
				}

				for (int LIndex = 0; LIndex < AStatement.Specials.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitSpecialDefinition(AStatement.Specials[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Append(Keywords.EndList);
			}
			EmitClassDefinition(AStatement.ClassDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateOperatorStatementBase(Statement AStatement)
		{
			if (AStatement is CreateOperatorStatement)
				EmitCreateOperatorStatement((CreateOperatorStatement)AStatement);
			else if (AStatement is CreateAggregateOperatorStatement)
				EmitCreateAggregateOperatorStatement((CreateAggregateOperatorStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitCreateOperatorStatement(CreateOperatorStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1}{2}", Keywords.Operator, AStatement.OperatorName, Keywords.BeginGroup);

			for (int LIndex = 0; LIndex < AStatement.FormalParameters.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitFormalParameter(AStatement.FormalParameters[LIndex]);
			}
			
			Append(Keywords.EndGroup);

			if (AStatement.ReturnType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.ReturnType);
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
			if (AStatement.Block.ClassDefinition != null)
				EmitClassDefinition(AStatement.Block.ClassDefinition);
			else if (AStatement.Block.Block != null)
			{
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(AStatement.Block.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateAggregateOperatorStatement(CreateAggregateOperatorStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2}{3}", Keywords.Aggregate, Keywords.Operator, AStatement.OperatorName, Keywords.BeginGroup);

			for (int LIndex = 0; LIndex < AStatement.FormalParameters.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitFormalParameter(AStatement.FormalParameters[LIndex]);
			}
			
			Append(Keywords.EndGroup);

			if (AStatement.ReturnType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.ReturnType);
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

			if ((AStatement.Initialization.ClassDefinition != null) || (AStatement.Initialization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Initialization);
				NewLine();
				Indent();
				if (AStatement.Initialization.ClassDefinition != null)
					EmitClassDefinition(AStatement.Initialization.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Initialization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((AStatement.Aggregation.ClassDefinition != null) || (AStatement.Aggregation.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Aggregation);
				NewLine();
				Indent();
				if (AStatement.Aggregation.ClassDefinition != null)
					EmitClassDefinition(AStatement.Aggregation.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Aggregation.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((AStatement.Finalization.ClassDefinition != null) || (AStatement.Finalization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Finalization);
				NewLine();
				Indent();
				if (AStatement.Finalization.ClassDefinition != null)
					EmitClassDefinition(AStatement.Finalization.ClassDefinition);
				else
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Finalization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateServerStatement(CreateServerStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Server, AStatement.ServerName);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceStatement(CreateDeviceStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Device, AStatement.DeviceName);
			if ((AStatement.DeviceScalarTypeMaps.Count > 0) || (AStatement.DeviceOperatorMaps.Count > 0))
			{
				bool LFirst = true;
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				
				for (int LIndex = 0; LIndex < AStatement.DeviceScalarTypeMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDeviceScalarTypeMap(AStatement.DeviceScalarTypeMaps[LIndex]);
				}

				for (int LIndex = 0; LIndex < AStatement.DeviceOperatorMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDeviceOperatorMap(AStatement.DeviceOperatorMaps[LIndex]);
				}

				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			
			NewLine();
			Indent();
			EmitReconciliationSettings(AStatement.ReconciliationSettings, false);
			EmitClassDefinition(AStatement.ClassDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitReconciliationSettings(ReconciliationSettings AReconciliationSettings, bool AIsAlter)
		{
			if ((AReconciliationSettings != null) && (AReconciliationSettings.ReconcileModeSet || AReconciliationSettings.ReconcileMasterSet))
			{
				if (AIsAlter)
					AppendFormat(" {0}", Keywords.Alter);
				AppendFormat(" {0} {1} ", Keywords.Reconciliation, Keywords.BeginList);
				bool LFirst = true;
				if (AReconciliationSettings.ReconcileModeSet)
				{
					AppendFormat("{0} {1} {2}", Keywords.Mode, Keywords.Equal, Keywords.BeginList);
					bool LFirstMode = true;
					if (AReconciliationSettings.ReconcileMode == ReconcileMode.None)
						Append(ReconcileMode.None.ToString().ToLower());
					else
					{
						if ((AReconciliationSettings.ReconcileMode & ReconcileMode.Startup) != 0)
						{
							if (!LFirstMode)
								EmitListSeparator();
							else
								LFirstMode = false;
							Append(ReconcileMode.Startup.ToString().ToLower());
						}
						
						if ((AReconciliationSettings.ReconcileMode & ReconcileMode.Command) != 0)
						{
							if (!LFirstMode)
								EmitListSeparator();
							else
								LFirstMode = false;
							Append(ReconcileMode.Command.ToString().ToLower());
						}
						
						if ((AReconciliationSettings.ReconcileMode & ReconcileMode.Automatic) != 0)
						{
							if (!LFirstMode)
								EmitListSeparator();
							else
								LFirstMode = false;
							Append(ReconcileMode.Automatic.ToString().ToLower());
						}
					}
					Append(Keywords.EndList);
					LFirst = false;
				}
				
				if (AReconciliationSettings.ReconcileMasterSet)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
						
					AppendFormat("{0} {1} {2}", Keywords.Master, Keywords.Equal, AReconciliationSettings.ReconcileMaster.ToString().ToLower());
				}
				
				AppendFormat(" {0}", Keywords.EndList);
			}
		}
		
		protected virtual void EmitAlterTableVarStatement(Statement AStatement)
		{
			if (AStatement is AlterTableStatement)
				EmitAlterTableStatement((AlterTableStatement)AStatement);
			else if (AStatement is AlterViewStatement)
				EmitAlterViewStatement((AlterViewStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitAlterTableStatement(AlterTableStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Table, AStatement.TableVarName);
			if
				(
					(AStatement.CreateColumns.Count > 0) ||
					(AStatement.AlterColumns.Count > 0) ||
					(AStatement.DropColumns.Count > 0) ||
					(AStatement.CreateKeys.Count > 0) ||
					(AStatement.AlterKeys.Count > 0) ||
					(AStatement.DropKeys.Count > 0) ||
					(AStatement.CreateOrders.Count > 0) ||
					(AStatement.AlterOrders.Count > 0) ||
					(AStatement.DropOrders.Count > 0) ||
					(AStatement.CreateConstraints.Count > 0) ||
					(AStatement.AlterConstraints.Count > 0) ||
					(AStatement.DropConstraints.Count > 0) ||
					(AStatement.CreateReferences.Count > 0) ||
					(AStatement.AlterReferences.Count > 0) ||
					(AStatement.DropReferences.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;

				for (int LIndex = 0; LIndex < AStatement.CreateColumns.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateColumnDefinition(AStatement.CreateColumns[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterColumns.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterColumnDefinition(AStatement.AlterColumns[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropColumns.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropColumnDefinition(AStatement.DropColumns[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateKeyDefinition(AStatement.CreateKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(AStatement.AlterKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(AStatement.DropKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateOrderDefinition(AStatement.CreateOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(AStatement.AlterOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(AStatement.DropOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateReferenceDefinition(AStatement.CreateReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(AStatement.AlterReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(AStatement.DropReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateCreateConstraintDefinition(AStatement.CreateConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(AStatement.AlterConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(AStatement.DropConstraints[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterViewStatement(AlterViewStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.View, AStatement.TableVarName);
			if
				(
					(AStatement.CreateKeys.Count > 0) ||
					(AStatement.AlterKeys.Count > 0) ||
					(AStatement.DropKeys.Count > 0) ||
					(AStatement.CreateOrders.Count > 0) ||
					(AStatement.AlterOrders.Count > 0) ||
					(AStatement.DropOrders.Count > 0) ||
					(AStatement.CreateConstraints.Count > 0) ||
					(AStatement.AlterConstraints.Count > 0) ||
					(AStatement.DropConstraints.Count > 0) ||
					(AStatement.CreateReferences.Count > 0) ||
					(AStatement.AlterReferences.Count > 0) ||
					(AStatement.DropReferences.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;

				for (int LIndex = 0; LIndex < AStatement.CreateKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateKeyDefinition(AStatement.CreateKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterKeyDefinition(AStatement.AlterKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropKeys.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropKeyDefinition(AStatement.DropKeys[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateOrderDefinition(AStatement.CreateOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterOrderDefinition(AStatement.AlterOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropOrders.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropOrderDefinition(AStatement.DropOrders[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateReferenceDefinition(AStatement.CreateReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterReferenceDefinition(AStatement.AlterReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropReferences.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropReferenceDefinition(AStatement.DropReferences[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateCreateConstraintDefinition(AStatement.CreateConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(AStatement.AlterConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(AStatement.DropConstraints[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterScalarTypeStatement(AlterScalarTypeStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Type, AStatement.ScalarTypeName);
			if
				(
					(AStatement.Default != null) ||
					(AStatement.CreateRepresentations.Count > 0) ||
					(AStatement.AlterRepresentations.Count > 0) ||
					(AStatement.DropRepresentations.Count > 0) ||
					(AStatement.CreateSpecials.Count > 0) ||
					(AStatement.AlterSpecials.Count > 0) ||
					(AStatement.DropSpecials.Count > 0) ||
					(AStatement.CreateConstraints.Count > 0) ||
					(AStatement.AlterConstraints.Count > 0) ||
					(AStatement.DropConstraints.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				
				if (AStatement.Default != null)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					if (AStatement.Default is DefaultDefinition)
						EmitCreateDefaultDefinition((DefaultDefinition)AStatement.Default);
					else if (AStatement.Default is AlterDefaultDefinition)
						EmitAlterDefaultDefinition((AlterDefaultDefinition)AStatement.Default);
					else if (AStatement.Default is DropDefaultDefinition)
						EmitDropDefaultDefinition((DropDefaultDefinition)AStatement.Default);
					else
						throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.Default.GetType().Name);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateRepresentations.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateRepresentationDefinition(AStatement.CreateRepresentations[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterRepresentations.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterRepresentationDefinition(AStatement.AlterRepresentations[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropRepresentations.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropRepresentationDefinition(AStatement.DropRepresentations[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateSpecials.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateSpecialDefinition(AStatement.CreateSpecials[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterSpecials.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterSpecialDefinition(AStatement.AlterSpecials[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropSpecials.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropSpecialDefinition(AStatement.DropSpecials[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(AStatement.CreateConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(AStatement.AlterConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(AStatement.DropConstraints[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitAlterClassDefinition(AStatement.AlterClassDefinition);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterOperatorStatementBase(Statement AStatement)
		{
			if (AStatement is AlterOperatorStatement)
				EmitAlterOperatorStatement((AlterOperatorStatement)AStatement);
			else if (AStatement is AlterAggregateOperatorStatement)
				EmitAlterAggregateOperatorStatement((AlterAggregateOperatorStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitAlterOperatorStatement(AlterOperatorStatement AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Operator);
			EmitOperatorSpecifier(AStatement.OperatorSpecifier);
			if (AStatement.Block.AlterClassDefinition != null)
				EmitAlterClassDefinition(AStatement.Block.AlterClassDefinition);
			else if (AStatement.Block.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(AStatement.Block.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterAggregateOperatorStatement(AlterAggregateOperatorStatement AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Aggregate, Keywords.Operator);
			EmitOperatorSpecifier(AStatement.OperatorSpecifier);

			if ((AStatement.Initialization.AlterClassDefinition != null) || (AStatement.Initialization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Initialization);
				NewLine();
				Indent();
				if (AStatement.Initialization.AlterClassDefinition != null)
					EmitAlterClassDefinition(AStatement.Initialization.AlterClassDefinition);
				else if (AStatement.Initialization.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Initialization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((AStatement.Aggregation.AlterClassDefinition != null) || (AStatement.Aggregation.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Aggregation);
				NewLine();
				Indent();
				if (AStatement.Aggregation.AlterClassDefinition != null)
					EmitAlterClassDefinition(AStatement.Aggregation.AlterClassDefinition);
				else if (AStatement.Aggregation.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Aggregation.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			if ((AStatement.Finalization.AlterClassDefinition != null) || (AStatement.Finalization.Block != null))
			{
				NewLine();
				Indent();
				Append(Keywords.Finalization);
				NewLine();
				Indent();
				if (AStatement.Finalization.AlterClassDefinition != null)
					EmitAlterClassDefinition(AStatement.Finalization.AlterClassDefinition);
				else if (AStatement.Finalization.Block != null)
				{
					Append(Keywords.Begin);
					IncreaseIndent();
					EmitTerminatedStatement(AStatement.Finalization.Block);
					DecreaseIndent();
					NewLine();
					Indent();
					Append(Keywords.End);
				}
			}

			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterServerStatement(AlterServerStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Server, AStatement.ServerName);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterDeviceStatement(AlterDeviceStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Device, AStatement.DeviceName);
			if
				(
					(AStatement.CreateDeviceScalarTypeMaps.Count > 0) ||
					(AStatement.AlterDeviceScalarTypeMaps.Count > 0) ||
					(AStatement.DropDeviceScalarTypeMaps.Count > 0) ||
					(AStatement.CreateDeviceOperatorMaps.Count > 0) ||
					(AStatement.AlterDeviceOperatorMaps.Count > 0) ||
					(AStatement.DropDeviceOperatorMaps.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				
				for (int LIndex = 0; LIndex < AStatement.CreateDeviceScalarTypeMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateDeviceScalarTypeMap(AStatement.CreateDeviceScalarTypeMaps[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterDeviceScalarTypeMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterDeviceScalarTypeMap(AStatement.AlterDeviceScalarTypeMaps[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropDeviceScalarTypeMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropDeviceScalarTypeMap(AStatement.DropDeviceScalarTypeMaps[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateDeviceOperatorMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateDeviceOperatorMap(AStatement.CreateDeviceOperatorMaps[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterDeviceOperatorMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterDeviceOperatorMap(AStatement.AlterDeviceOperatorMaps[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropDeviceOperatorMaps.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropDeviceOperatorMap(AStatement.DropDeviceOperatorMaps[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitReconciliationSettings(AStatement.ReconciliationSettings, true);
			EmitAlterClassDefinition(AStatement.AlterClassDefinition);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropObjectStatement(Statement AStatement)
		{
			if (AStatement is DropTableStatement)
				EmitDropTableStatement((DropTableStatement)AStatement);
			else if (AStatement is DropViewStatement)
				EmitDropViewStatement((DropViewStatement)AStatement);
			else if (AStatement is DropScalarTypeStatement)
				EmitDropScalarTypeStatement((DropScalarTypeStatement)AStatement);
			else if (AStatement is DropOperatorStatement)
				EmitDropOperatorStatement((DropOperatorStatement)AStatement);
			else if (AStatement is DropServerStatement)
				EmitDropServerStatement((DropServerStatement)AStatement);
			else if (AStatement is DropDeviceStatement)
				EmitDropDeviceStatement((DropDeviceStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}

		protected virtual void EmitDropTableStatement(DropTableStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Table, AStatement.ObjectName);
		}
		
		protected virtual void EmitDropViewStatement(DropViewStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.View, AStatement.ObjectName);
		}
		
		protected virtual void EmitDropScalarTypeStatement(DropScalarTypeStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Type, AStatement.ObjectName);
		}
		
		protected virtual void EmitDropOperatorStatement(DropOperatorStatement AStatement)
		{
			AppendFormat("{0} {1} {2}{3}", Keywords.Drop, Keywords.Operator, AStatement.ObjectName, Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AStatement.FormalParameterSpecifiers.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitFormalParameterSpecifier(AStatement.FormalParameterSpecifiers[LIndex]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDropServerStatement(DropServerStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Server, AStatement.ObjectName);
		}
		
		protected virtual void EmitDropDeviceStatement(DropDeviceStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Device, AStatement.ObjectName);
		}
		
		protected virtual void EmitScalarTypeNameDefinition(ScalarTypeNameDefinition AStatement)
		{
			Append(AStatement.ScalarTypeName);
		}
		
		protected virtual void EmitAccessorBlock(AccessorBlock AAccessorBlock)
		{
			if (AAccessorBlock.ClassDefinition != null)
				EmitClassDefinition(AAccessorBlock.ClassDefinition);
			else if (AAccessorBlock.Expression != null)
				EmitExpression(AAccessorBlock.Expression);
			else if (AAccessorBlock.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(AAccessorBlock.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
		}
		
		protected virtual void EmitAlterAccessorBlock(AlterAccessorBlock AAccessorBlock)
		{
			if (AAccessorBlock.AlterClassDefinition != null)
				EmitAlterClassDefinition(AAccessorBlock.AlterClassDefinition);
			else if (AAccessorBlock.Expression != null)
				EmitExpression(AAccessorBlock.Expression);
			else if (AAccessorBlock.Block != null)
			{
				NewLine();
				Indent();
				Append(Keywords.Begin);
				IncreaseIndent();
				EmitTerminatedStatement(AAccessorBlock.Block);
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.End);
			}
		}
		
		protected virtual void EmitRepresentationDefinitionBase(RepresentationDefinitionBase AStatement)
		{
			if (AStatement is RepresentationDefinition)
				EmitRepresentationDefinition((RepresentationDefinition)AStatement);
			else if (AStatement is AlterRepresentationDefinition)
				EmitAlterRepresentationDefinition((AlterRepresentationDefinition)AStatement);
			else if (AStatement is DropRepresentationDefinition)
				EmitDropRepresentationDefinition((DropRepresentationDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitRepresentationDefinition(RepresentationDefinition AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Representation, AStatement.RepresentationName);
			NewLine();
			Indent();
			Append(Keywords.BeginList);
			IncreaseIndent();
			for (int LIndex = 0; LIndex < AStatement.Properties.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				NewLine();
				Indent();
				EmitPropertyDefinition(AStatement.Properties[LIndex]);
			}
			DecreaseIndent();
			NewLine();
			Indent();
			Append(Keywords.EndList);
			if (AStatement.SelectorAccessorBlock != null)
			{
				AppendFormat("{0} ", Keywords.Selector);
				EmitAccessorBlock(AStatement.SelectorAccessorBlock);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateRepresentationDefinition(RepresentationDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitRepresentationDefinition(AStatement);
		}
		
		protected virtual void EmitAlterRepresentationDefinition(AlterRepresentationDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Representation, AStatement.RepresentationName);
			if 
				(
					(AStatement.CreateProperties.Count > 0) ||
					(AStatement.AlterProperties.Count > 0) ||
					(AStatement.DropProperties.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();

				for (int LIndex = 0; LIndex < AStatement.CreateProperties.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitCreatePropertyDefinition(AStatement.CreateProperties[LIndex]);
				}

				for (int LIndex = 0; LIndex < AStatement.AlterProperties.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitAlterPropertyDefinition(AStatement.AlterProperties[LIndex]);
				}

				for (int LIndex = 0; LIndex < AStatement.DropProperties.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					NewLine();
					Indent();
					EmitDropPropertyDefinition(AStatement.DropProperties[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			if (AStatement.SelectorAccessorBlock != null)
			{
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Selector);
				EmitAlterAccessorBlock(AStatement.SelectorAccessorBlock);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropRepresentationDefinition(DropRepresentationDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Representation, AStatement.RepresentationName);
		}
		
		protected virtual void EmitPropertyDefinitionBase(PropertyDefinitionBase AStatement)
		{
			if (AStatement is PropertyDefinition)
				EmitPropertyDefinition((PropertyDefinition)AStatement);
			else if (AStatement is AlterPropertyDefinition)
				EmitAlterPropertyDefinition((AlterPropertyDefinition)AStatement);
			else if (AStatement is DropPropertyDefinition)
				EmitDropPropertyDefinition((DropPropertyDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitPropertyDefinition(PropertyDefinition AStatement)
		{
			AppendFormat("{0} {1} ", AStatement.PropertyName, Keywords.TypeSpecifier);
			EmitTypeSpecifier(AStatement.PropertyType);
			IncreaseIndent();
			
			if (AStatement.ReadAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Read);
				EmitAccessorBlock(AStatement.ReadAccessorBlock);
			}
			
			if (AStatement.WriteAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} ", Keywords.Write);
				EmitAccessorBlock(AStatement.WriteAccessorBlock);
			}

			if (AStatement.MetaData != null)
			{
				NewLine();
				Indent();
				EmitMetaData(AStatement.MetaData);
			}

			DecreaseIndent();
		}
		
		protected virtual void EmitCreatePropertyDefinition(PropertyDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitPropertyDefinition(AStatement);
		}
		
		protected virtual void EmitAlterPropertyDefinition(AlterPropertyDefinition AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Alter, AStatement.PropertyName);
			if (AStatement.PropertyType != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.PropertyType);
			}
			
			IncreaseIndent();
			
			if (AStatement.ReadAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Read);
				EmitAlterAccessorBlock(AStatement.ReadAccessorBlock);
			}
			
			if (AStatement.WriteAccessorBlock != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Write);
				EmitAlterAccessorBlock(AStatement.WriteAccessorBlock);
			}
			
			if (AStatement.AlterMetaData != null)
			{
				NewLine();
				Indent();
				EmitAlterMetaData(AStatement.AlterMetaData);
			}
			
			DecreaseIndent();
		}
		
		protected virtual void EmitDropPropertyDefinition(DropPropertyDefinition AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Drop, AStatement.PropertyName);
		}
		
		protected virtual void EmitSpecialDefinition(SpecialDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Special, AStatement.Name, Keywords.BeginGroup);
			EmitExpression(AStatement.Value);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateSpecialDefinition(SpecialDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitSpecialDefinition(AStatement);
		}
		
		protected virtual void EmitAlterSpecialDefinition(AlterSpecialDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Special, AStatement.Name);
			if (AStatement.Value != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(AStatement.Value);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropSpecialDefinition(DropSpecialDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Special, AStatement.Name);
		}
		
		protected virtual void EmitColumnDefinitionBase(ColumnDefinitionBase AStatement)
		{
			if (AStatement is ColumnDefinition)
				EmitColumnDefinition((ColumnDefinition)AStatement);
			else if (AStatement is AlterColumnDefinition)
				EmitAlterColumnDefinition((AlterColumnDefinition)AStatement);
			else if (AStatement is DropColumnDefinition)
				EmitDropColumnDefinition((DropColumnDefinition)AStatement);
			else if (AStatement is KeyColumnDefinition)
				EmitKeyColumnDefinition((KeyColumnDefinition)AStatement);
			else if (AStatement is ReferenceColumnDefinition)
				EmitReferenceColumnDefinition((ReferenceColumnDefinition)AStatement);
			else if (AStatement is OrderColumnDefinition)
				EmitOrderColumnDefinition((OrderColumnDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitColumnDefinition(ColumnDefinition AStatement)
		{
			AppendFormat("{0} {1} ", AStatement.ColumnName, Keywords.TypeSpecifier);
			EmitTypeSpecifier(AStatement.TypeSpecifier);
			if (AStatement.IsNilable)
				AppendFormat(" {0}", Keywords.Nil);
			
			if ((AStatement.Default != null) || (AStatement.Constraints.Count > 0))
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				
				if (AStatement.Default != null)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDefaultDefinition(AStatement.Default);
				}
				
				for (int LIndex = 0; LIndex < AStatement.Constraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitConstraintDefinition(AStatement.Constraints[LIndex]);
				}
				
				DecreaseIndent();
				NewLine();
				Indent();
				Append(Keywords.EndList);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateColumnDefinition(ColumnDefinition AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Column);
			EmitColumnDefinition(AStatement);
		}
		
		protected virtual void EmitAlterColumnDefinition(AlterColumnDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Column, AStatement.ColumnName);
			if (AStatement.TypeSpecifier != null)
			{
				AppendFormat(" {0} ", Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.TypeSpecifier);
			}
			
			if 
				(
					(AStatement.ChangeNilable) ||
					(AStatement.Default != null) ||
					(AStatement.CreateConstraints.Count > 0) ||
					(AStatement.AlterConstraints.Count > 0) ||
					(AStatement.DropConstraints.Count > 0)
				)
			{
				NewLine();
				Indent();
				Append(Keywords.BeginList);
				IncreaseIndent();
				bool LFirst = true;
				
				if (AStatement.ChangeNilable)
				{
					LFirst = false;
					NewLine();
					Indent();
					if (!AStatement.IsNilable)
						AppendFormat("{0} ", Keywords.Not);
					Append(Keywords.Nil);
				}

				if (AStatement.Default != null)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					if (AStatement.Default is DefaultDefinition)
						EmitCreateDefaultDefinition((DefaultDefinition)AStatement.Default);
					else if (AStatement.Default is AlterDefaultDefinition)
						EmitAlterDefaultDefinition((AlterDefaultDefinition)AStatement.Default);
					else if (AStatement.Default is DropDefaultDefinition)
						EmitDropDefaultDefinition((DropDefaultDefinition)AStatement.Default);
					else
						throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.Default.GetType().Name);
				}
				
				for (int LIndex = 0; LIndex < AStatement.CreateConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitCreateConstraintDefinition(AStatement.CreateConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.AlterConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitAlterConstraintDefinitionBase(AStatement.AlterConstraints[LIndex]);
				}
				
				for (int LIndex = 0; LIndex < AStatement.DropConstraints.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					NewLine();
					Indent();
					EmitDropConstraintDefinition(AStatement.DropConstraints[LIndex]);
				}

				DecreaseIndent();
				NewLine();
				Indent();				
				Append(Keywords.EndList);
			}
			
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropColumnDefinition(DropColumnDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Column, AStatement.ColumnName);
		}
		
		protected virtual void EmitKeyColumnDefinition(KeyColumnDefinition AStatement)
		{
			Append(AStatement.ColumnName);
		}
		
		protected virtual void EmitReferenceColumnDefinition(ReferenceColumnDefinition AStatement)
		{
			Append(AStatement.ColumnName);
		}
		
		protected virtual void EmitOrderColumnDefinition(OrderColumnDefinition AStatement)
		{
			AppendFormat("{0} ", AStatement.ColumnName);
			if (AStatement.Sort != null)
			{
				EmitSortDefinition(AStatement.Sort);
				Append(" ");
			}
			AppendFormat("{0}", AStatement.Ascending ? Keywords.Asc : Keywords.Desc);
			if (AStatement.IncludeNils)
				AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
		}
		
		protected virtual void EmitKeyDefinitionBase(KeyDefinitionBase AStatement)
		{
			if (AStatement is KeyDefinition)
				EmitKeyDefinition((KeyDefinition)AStatement);
			else if (AStatement is AlterKeyDefinition)
				EmitAlterKeyDefinition((AlterKeyDefinition)AStatement);
			else if (AStatement is DropKeyDefinition)
				EmitDropKeyDefinition((DropKeyDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitKeyDefinition(KeyDefinition AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Key, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateKeyDefinition(KeyDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitKeyDefinition(AStatement);
		}
		
		protected virtual void EmitAlterKeyDefinition(AlterKeyDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Key, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropKeyDefinition(DropKeyDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Drop, Keywords.Key, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitKeyColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitReferenceDefinitionBase(ReferenceDefinitionBase AStatement)
		{
			if (AStatement is ReferenceDefinition)
				EmitReferenceDefinition((ReferenceDefinition)AStatement);
			else if (AStatement is AlterReferenceDefinition)
				EmitAlterReferenceDefinition((AlterReferenceDefinition)AStatement);
			else if (AStatement is DropReferenceDefinition)
				EmitDropReferenceDefinition((DropReferenceDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitReferenceDefinition(ReferenceDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Reference, AStatement.ReferenceName, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0} ", Keywords.EndList);
			EmitReferencesDefinition(AStatement.ReferencesDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateReferenceDefinition(ReferenceDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitReferenceDefinition(AStatement);
		}
		
		protected virtual void EmitCreateReferenceStatement(CreateReferenceStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2} {3} ", Keywords.Reference, AStatement.ReferenceName, AStatement.TableVarName, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0} ", Keywords.EndList);
			EmitReferencesDefinition(AStatement.ReferencesDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitAlterReferenceDefinition(AlterReferenceDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Reference, AStatement.ReferenceName);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterReferenceStatement(AlterReferenceStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Reference, AStatement.ReferenceName);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropReferenceDefinition(DropReferenceDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Reference, AStatement.ReferenceName);
		}
		
		protected virtual void EmitDropReferenceStatement(DropReferenceStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Reference, AStatement.ReferenceName);
		}
		
		protected virtual void EmitReferencesDefinition(ReferencesDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.References, AStatement.TableVarName, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitReferenceColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0} ", Keywords.EndList);

			switch (AStatement.UpdateReferenceAction)
			{
				case ReferenceAction.Require: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Require); break;
				case ReferenceAction.Cascade: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Cascade); break;
				case ReferenceAction.Clear: AppendFormat(" {0} {1}", Keywords.Update, Keywords.Clear); break;
				case ReferenceAction.Set:
					AppendFormat(" {0} {1} {2} ", Keywords.Update, Keywords.Set, Keywords.BeginList);
					for (int LIndex = 0; LIndex < AStatement.UpdateReferenceExpressions.Count; LIndex++)
					{
						if (LIndex > 0)
							EmitListSeparator();
						EmitExpression(AStatement.UpdateReferenceExpressions[LIndex]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}

			switch (AStatement.DeleteReferenceAction)
			{
				case ReferenceAction.Require: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Require); break;
				case ReferenceAction.Cascade: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Cascade); break;
				case ReferenceAction.Clear: AppendFormat(" {0} {1}", Keywords.Delete, Keywords.Clear); break;
				case ReferenceAction.Set:
					AppendFormat(" {0} {1} {2} ", Keywords.Delete, Keywords.Set, Keywords.BeginList);
					for (int LIndex = 0; LIndex < AStatement.DeleteReferenceExpressions.Count; LIndex++)
					{
						if (LIndex > 0)
							EmitListSeparator();
						EmitExpression(AStatement.DeleteReferenceExpressions[LIndex]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}
		}
		
		protected virtual void EmitOrderDefinitionBase(OrderDefinitionBase AStatement)
		{
			if (AStatement is OrderDefinition)
				EmitOrderDefinition((OrderDefinition)AStatement);
			else if (AStatement is AlterOrderDefinition)
				EmitAlterOrderDefinition((AlterOrderDefinition)AStatement);
			else if (AStatement is DropOrderDefinition)
				EmitDropOrderDefinition((DropOrderDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitOrderDefinition(OrderDefinition AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Order, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateOrderDefinition(OrderDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitOrderDefinition(AStatement);
		}
		
		protected virtual void EmitAlterOrderDefinition(AlterOrderDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Order, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropOrderDefinition(DropOrderDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Drop, Keywords.Order, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitOrderColumnDefinition(AStatement.Columns[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitConstraintDefinitionBase(ConstraintDefinitionBase AStatement)
		{
			if (AStatement is ConstraintDefinition)
				EmitConstraintDefinition((ConstraintDefinition)AStatement);
			else if (AStatement is AlterConstraintDefinition)
				EmitAlterConstraintDefinition((AlterConstraintDefinition)AStatement);
			else if (AStatement is DropConstraintDefinition)
				EmitDropConstraintDefinition((DropConstraintDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitConstraintDefinition(ConstraintDefinition AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Constraint, AStatement.ConstraintName, Keywords.BeginGroup);
			EmitExpression(AStatement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateConstraintDefinition(CreateConstraintDefinition AStatement)
		{
			if (AStatement is ConstraintDefinition)
				EmitConstraintDefinition((ConstraintDefinition)AStatement);
			else
				EmitTransitionConstraintDefinition((TransitionConstraintDefinition)AStatement);
		}
		
		protected virtual void EmitTransitionConstraintDefinition(TransitionConstraintDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Transition, Keywords.Constraint, AStatement.ConstraintName);
			IncreaseIndent();
			if (AStatement.OnInsertExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Insert);
				EmitExpression(AStatement.OnInsertExpression);
			}
			if (AStatement.OnUpdateExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Update);
				EmitExpression(AStatement.OnUpdateExpression);
			}
			if (AStatement.OnDeleteExpression != null)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} ", Keywords.On, Keywords.Delete);
				EmitExpression(AStatement.OnDeleteExpression);
			}
			NewLine();
			Indent();
			EmitMetaData(AStatement.MetaData);
			DecreaseIndent();
		}
		
		protected virtual void EmitCreateConstraintDefinition(ConstraintDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitConstraintDefinition(AStatement);
		}
		
		protected virtual void EmitCreateCreateConstraintDefinition(CreateConstraintDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitCreateConstraintDefinition(AStatement);
		}
		
		protected virtual void EmitCreateConstraintStatement(CreateConstraintStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			if (AStatement.IsSession)
				AppendFormat("{0} ", Keywords.Session);
			AppendFormat("{0} {1} {2}", Keywords.Constraint, AStatement.ConstraintName, Keywords.BeginGroup);
			EmitExpression(AStatement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitAlterConstraintDefinitionBase(AlterConstraintDefinitionBase AStatement)
		{
			if (AStatement is AlterConstraintDefinition)
				EmitAlterConstraintDefinition((AlterConstraintDefinition)AStatement);
			else
				EmitAlterTransitionConstraintDefinition((AlterTransitionConstraintDefinition)AStatement);
		}
		
		protected virtual void EmitAlterConstraintDefinition(AlterConstraintDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Alter);
			AppendFormat("{0} {1}", Keywords.Constraint, AStatement.ConstraintName);
			if (AStatement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(AStatement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitAlterTransitionConstraintDefinitionItem(string ATransition, AlterTransitionConstraintDefinitionItemBase AItem)
		{
			if (AItem is AlterTransitionConstraintDefinitionCreateItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2} ", Keywords.Create, Keywords.On, ATransition);
				EmitExpression(((AlterTransitionConstraintDefinitionCreateItem)AItem).Expression);
			}
			else if (AItem is AlterTransitionConstraintDefinitionAlterItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.On, ATransition);
				EmitExpression(((AlterTransitionConstraintDefinitionAlterItem)AItem).Expression);
			}
			else if (AItem is AlterTransitionConstraintDefinitionDropItem)
			{
				NewLine();
				Indent();
				AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.On, ATransition);
			}
		}
		
		protected virtual void EmitAlterTransitionConstraintDefinition(AlterTransitionConstraintDefinition AStatement)
		{
			AppendFormat("{0} {1} {2} {3}", Keywords.Alter, Keywords.Transition, Keywords.Constraint, AStatement.ConstraintName);
			IncreaseIndent();
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Insert, AStatement.OnInsert);
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Update, AStatement.OnUpdate);
			EmitAlterTransitionConstraintDefinitionItem(Keywords.Delete, AStatement.OnDelete);
			if (AStatement.AlterMetaData != null)
			{
				NewLine();
				Indent();
				EmitAlterMetaData(AStatement.AlterMetaData);
			}
			DecreaseIndent();
			NewLine();
			Indent();
		}
		
		protected virtual void EmitAlterConstraintStatement(AlterConstraintStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Constraint, AStatement.ConstraintName);
			if (AStatement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(AStatement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropConstraintDefinition(DropConstraintDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Drop);
			if (AStatement.IsTransition)
				AppendFormat("{0} ", Keywords.Transition);
			AppendFormat("{0} {1}", Keywords.Constraint, AStatement.ConstraintName);
		}
		
		protected virtual void EmitDropConstraintStatement(DropConstraintStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Constraint, AStatement.ConstraintName);
		}
		
		protected virtual void EmitAttachStatementBase(AttachStatementBase AStatement)
		{
			if (AStatement is AttachStatement)
				EmitAttachStatement((AttachStatement)AStatement);
			else if (AStatement is InvokeStatement)
				EmitInvokeStatement((InvokeStatement)AStatement);
			else if (AStatement is DetachStatement)
				EmitDetachStatement((DetachStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitAttachStatement(AttachStatement AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Attach, AStatement.OperatorName, Keywords.To);
			EmitEventSourceSpecifier(AStatement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(AStatement.EventSpecifier);
			Append(" ");
			if (AStatement.BeforeOperatorNames.Count > 0)
			{
				AppendFormat("{0} {1} ", Keywords.Before, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AStatement.BeforeOperatorNames.Count; LIndex++)
				{
					if (LIndex > 1)
						EmitListSeparator();
					Append(AStatement.BeforeOperatorNames[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitInvokeStatement(InvokeStatement AStatement)
		{	
			AppendFormat("{0} {1} {2} ", Keywords.Invoke, AStatement.OperatorName, Keywords.On);
			EmitEventSourceSpecifier(AStatement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(AStatement.EventSpecifier);
			AppendFormat("{0} {1} ", Keywords.Before, Keywords.BeginList);
			for (int LIndex = 0; LIndex < AStatement.BeforeOperatorNames.Count; LIndex++)
			{
				if (LIndex > 1)
					EmitListSeparator();
				Append(AStatement.BeforeOperatorNames[LIndex]);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitDetachStatement(DetachStatement AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Detach, AStatement.OperatorName, Keywords.From);
			EmitEventSourceSpecifier(AStatement.EventSourceSpecifier);
			Append(" ");
			EmitEventSpecifier(AStatement.EventSpecifier);
		}
		
		protected virtual void EmitEventSourceSpecifier(EventSourceSpecifier AStatement)
		{
			if (AStatement is ObjectEventSourceSpecifier)
				EmitObjectEventSourceSpecifier((ObjectEventSourceSpecifier)AStatement);
			else if (AStatement is ColumnEventSourceSpecifier)
				EmitColumnEventSourceSpecifier((ColumnEventSourceSpecifier)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitObjectEventSourceSpecifier(ObjectEventSourceSpecifier AStatement)
		{
			Append(AStatement.ObjectName);
		}
		
		protected virtual void EmitColumnEventSourceSpecifier(ColumnEventSourceSpecifier AStatement)
		{
			AppendFormat("{0} {1} {2}", AStatement.ColumnName, Keywords.In, AStatement.TableVarName);
		}
		
		protected virtual void EmitEventSpecifier(EventSpecifier AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.On, Keywords.BeginList);
			bool LFirst = true;
			if ((AStatement.EventType & EventType.BeforeInsert) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Insert);
			}

			if ((AStatement.EventType & EventType.BeforeUpdate) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Update);
			}

			if ((AStatement.EventType & EventType.BeforeDelete) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.Before, Keywords.Delete);
			}

			if ((AStatement.EventType & EventType.AfterInsert) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Insert);
			}

			if ((AStatement.EventType & EventType.AfterUpdate) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Update);
			}

			if ((AStatement.EventType & EventType.AfterDelete) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				AppendFormat("{0} {1}", Keywords.After, Keywords.Delete);
			}

			if ((AStatement.EventType & EventType.Default) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				Append(Keywords.Default);
			}

			if ((AStatement.EventType & EventType.Change) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				Append(Keywords.Change);
			}

			if ((AStatement.EventType & EventType.Validate) != 0)
			{
				if (!LFirst)
					EmitListSeparator();
				else
					LFirst = false;
				Append(Keywords.Validate);
			}
			AppendFormat(" {0}", Keywords.EndList);
		}
		
		protected virtual void EmitRightStatementBase(RightStatementBase AStatement)
		{
			if (AStatement is GrantStatement)
				EmitGrantStatement((GrantStatement)AStatement);
			else if (AStatement is RevokeStatement)
				EmitRevokeStatement((RevokeStatement)AStatement);
			else if (AStatement is RevertStatement)
				EmitRevertStatement((RevertStatement)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitGrantStatement(GrantStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Grant);
			EmitRightSpecifier(AStatement);
			if (AStatement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(AStatement.Target);
			}
			AppendFormat(" {0} ", Keywords.To);
			EmitSecuritySpecifier(AStatement);
		}
		
		protected virtual void EmitRevokeStatement(RevokeStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Revoke);
			EmitRightSpecifier(AStatement);
			if (AStatement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(AStatement.Target);
			}
			AppendFormat(" {0} ", Keywords.From);
			EmitSecuritySpecifier(AStatement);
		}
		
		protected virtual void EmitRevertStatement(RevertStatement AStatement)
		{
			AppendFormat("{0} ", Keywords.Revert);
			EmitRightSpecifier(AStatement);
			if (AStatement.Target != null)
			{
				AppendFormat(" {0} ", Keywords.On);
				EmitCatalogObjectSpecifier(AStatement.Target);
			}
			AppendFormat(" {0} ", Keywords.For);
			EmitSecuritySpecifier(AStatement);
		}
		
		protected virtual void EmitRightSpecifier(RightStatementBase AStatement)
		{
			switch (AStatement.RightType)
			{
				case RightSpecifierType.All : Append(Keywords.All); break;
				case RightSpecifierType.Usage : Append(Keywords.Usage); break;
				default:
					AppendFormat("{0} ", Keywords.BeginList);
					for (int LIndex = 0; LIndex < AStatement.Rights.Count; LIndex++)
					{
						if (LIndex > 0)
							AppendFormat("{0} ", Keywords.ListSeparator);
						Append(AStatement.Rights[LIndex].RightName);
					}
					AppendFormat(" {0}", Keywords.EndList);
				break;
			}
		}
		
		protected virtual void EmitCatalogObjectSpecifier(CatalogObjectSpecifier ASpecifier)
		{
			if (ASpecifier.IsOperator)
				EmitOperatorSpecifier(ASpecifier.ObjectName, ASpecifier.FormalParameterSpecifiers);
			else
				Append(ASpecifier.ObjectName);
		}
		
		protected virtual void EmitSecuritySpecifier(RightStatementBase AStatement)
		{
			switch (AStatement.GranteeType)
			{
				case GranteeType.User :
					AppendFormat(@"{0} ""{1}""i", Keywords.User, AStatement.Grantee.Replace(@"""", @""""""));
				break;
				
				case GranteeType.Role :
					AppendFormat(@"{0} {1}", Keywords.Role, AStatement.Grantee);
					if (AStatement.IsInherited)
						AppendFormat(" {0}", Keywords.Inherited);
				break;
				
				case GranteeType.Group :
					AppendFormat(@"{0} ""{1}""i", Keywords.Group, AStatement.Grantee.Replace(@"""", @""""""));
					if (AStatement.IsInherited)
						AppendFormat(" {0}", Keywords.Inherited);
					if (AStatement.ApplyRecursively)
						AppendFormat(" {0} {1}", Keywords.Apply, Keywords.Recursively);
					if (AStatement.IncludeUsers)
						AppendFormat(" {0} {1}", Keywords.Include, Keywords.Users);
				break;
			}
		}
		
		protected virtual void EmitDefaultDefinitionBase(DefaultDefinitionBase AStatement)
		{
			if (AStatement is DefaultDefinition)
				EmitDefaultDefinition((DefaultDefinition)AStatement);
			else if (AStatement is AlterDefaultDefinition)
				EmitAlterDefaultDefinition((AlterDefaultDefinition)AStatement);
			else if (AStatement is DropDefaultDefinition)
				EmitDropDefaultDefinition((DropDefaultDefinition)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitSortDefinition(SortDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Sort);
			EmitExpression(AStatement.Expression);
		}
		
		protected virtual void EmitCreateSortStatement(CreateSortStatement AStatement)
		{
			AppendFormat("{0} {1} {2} {3} ", Keywords.Create, Keywords.Sort, AStatement.ScalarTypeName, Keywords.Using);
			EmitExpression(AStatement.Expression);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitAlterSortStatement(AlterSortStatement AStatement)
		{
			AppendFormat("{0} {1} {2} ", Keywords.Alter, Keywords.Sort, AStatement.ScalarTypeName);
			if (AStatement.Expression != null)
			{
				AppendFormat("{0} ", Keywords.Using);
				EmitExpression(AStatement.Expression);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropSortStatement(DropSortStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Sort, AStatement.ScalarTypeName);
		}
		
		protected virtual void EmitCreateRoleStatement(CreateRoleStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Role, AStatement.RoleName);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitAlterRoleStatement(AlterRoleStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Role, AStatement.RoleName);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropRoleStatement(DropRoleStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Role, AStatement.RoleName);
		}
		
		protected virtual void EmitCreateRightStatement(CreateRightStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Create, Keywords.Right, AStatement.RightName);
		}
		
		protected virtual void EmitDropRightStatement(DropRightStatement AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Right, AStatement.RightName);
		}
		
		protected virtual void EmitCreateConversionStatement(CreateConversionStatement AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Create, Keywords.Conversion);
			EmitTypeSpecifier(AStatement.SourceScalarTypeName);
			AppendFormat(" {0} ", Keywords.To);
			EmitTypeSpecifier(AStatement.TargetScalarTypeName);
			AppendFormat(" {0} ", Keywords.Using);
			EmitIdentifierExpression(AStatement.OperatorName);
			AppendFormat(" {0}", AStatement.IsNarrowing ? Keywords.Narrowing : Keywords.Widening);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitDropConversionStatement(DropConversionStatement AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Conversion);
			EmitTypeSpecifier(AStatement.SourceScalarTypeName);
			AppendFormat(" {0} ", Keywords.To);
			EmitTypeSpecifier(AStatement.TargetScalarTypeName);
		}
		
		protected virtual void EmitDefaultDefinition(DefaultDefinition AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Default, Keywords.BeginGroup);
			EmitExpression(AStatement.Expression);
			AppendFormat("{0}", Keywords.EndGroup);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateDefaultDefinition(DefaultDefinition AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDefaultDefinition(AStatement);
		}
		
		protected virtual void EmitAlterDefaultDefinition(AlterDefaultDefinition AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Default);
			if (AStatement.Expression != null)
			{
				AppendFormat(" {0}", Keywords.BeginGroup);
				EmitExpression(AStatement.Expression);
				AppendFormat("{0}", Keywords.EndGroup);
			}
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropDefaultDefinition(DropDefaultDefinition AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Drop, Keywords.Default);
		}
		
		protected virtual void EmitClassDefinition(ClassDefinition AStatement)
		{
			if (AStatement != null)
			{
				AppendFormat(@" {0} ""{1}""", Keywords.Class, AStatement.ClassName.Replace(@"""", @""""""));
				
				if (AStatement.Attributes.Count > 0)
				{
					AppendFormat(" {0} {1} ", Keywords.Attributes, Keywords.BeginList);
					for (int LIndex = 0; LIndex < AStatement.Attributes.Count; LIndex++)
					{
						if (LIndex > 0)
							EmitListSeparator();
						EmitClassAttributeDefinition(AStatement.Attributes[LIndex]);
					}
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitAlterClassDefinition(AlterClassDefinition AStatement)
		{
			if (AStatement != null)
			{
				AppendFormat(" {0} {1}", Keywords.Alter, Keywords.Class);
				if (AStatement.ClassName != String.Empty)
					AppendFormat(@" ""{0}""", AStatement.ClassName.Replace(@"""", @""""""));
				if 
					(
						(AStatement.CreateAttributes.Count > 0) || 
						(AStatement.AlterAttributes.Count > 0) || 
						(AStatement.DropAttributes.Count > 0)
					)
				{
					AppendFormat(" {0} ", Keywords.BeginList);
					bool LFirst = true;
					foreach (ClassAttributeDefinition LAttribute in AStatement.CreateAttributes)
					{
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat("{0} ", Keywords.Create);
						EmitClassAttributeDefinition(LAttribute);
					}
					
					foreach (ClassAttributeDefinition LAttribute in AStatement.AlterAttributes)
					{
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat("{0} ", Keywords.Alter);
						EmitClassAttributeDefinition(LAttribute);
					}
					
					foreach (ClassAttributeDefinition LAttribute in AStatement.DropAttributes)
					{
						if (!LFirst)
							EmitListSeparator();
						else
							LFirst = false;
						AppendFormat(@"{0} ""{1}""", new object[]{Keywords.Drop, LAttribute.AttributeName.Replace(@"""", @"""""")}); 
					}
					
					AppendFormat(" {0}", Keywords.EndList);
				}
			}
		}
		
		protected virtual void EmitClassAttributeDefinition(ClassAttributeDefinition AStatement)
		{
			AppendFormat(@"""{0}"" {1} ""{2}""", AStatement.AttributeName.Replace(@"""", @""""""), Keywords.Equal, AStatement.AttributeValue.Replace(@"""", @""""""));
		}
		
		protected virtual void EmitNamedTypeSpecifier(NamedTypeSpecifier AStatement)
		{
			if (AStatement is FormalParameter)
				EmitFormalParameter((FormalParameter)AStatement);
			else
			{
				AppendFormat("{0} {1} ", AStatement.Identifier, Keywords.TypeSpecifier);
				EmitTypeSpecifier(AStatement.TypeSpecifier);
			};
		}
		
		protected virtual void EmitFormalParameter(FormalParameter AStatement)
		{
			switch (AStatement.Modifier)
			{
				case Modifier.Var: AppendFormat("{0} ", Keywords.Var); break;
				case Modifier.Const: AppendFormat("{0} ", Keywords.Const); break;
			}
			AppendFormat("{0} {1} ", AStatement.Identifier, Keywords.TypeSpecifier);
			EmitTypeSpecifier(AStatement.TypeSpecifier);
		}
		
		protected virtual void EmitFormalParameterSpecifier(FormalParameterSpecifier AStatement)
		{
			switch (AStatement.Modifier)
			{
				case Modifier.Var: AppendFormat("{0} ", Keywords.Var); break;
				case Modifier.Const: AppendFormat("{0} ", Keywords.Const); break;
			}
			EmitTypeSpecifier(AStatement.TypeSpecifier);
		}
		
		protected void EmitOperatorSpecifier(OperatorSpecifier ASpecifier)
		{
			EmitOperatorSpecifier(ASpecifier.OperatorName, ASpecifier.FormalParameterSpecifiers);
		}
		
		protected virtual void EmitOperatorSpecifier(string AOperatorName, FormalParameterSpecifiers AFormalParameterSpecifiers)
		{
			AppendFormat("{0}{1}", AOperatorName, Keywords.BeginGroup);
			for (int LIndex = 0; LIndex < AFormalParameterSpecifiers.Count; LIndex++)
			{
				if (LIndex > 0)
					EmitListSeparator();
				EmitFormalParameterSpecifier(AFormalParameterSpecifiers[LIndex]);
			}
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitTypeSpecifier(TypeSpecifier AStatement)
		{
			if (AStatement is GenericTypeSpecifier)
				EmitGenericTypeSpecifier((GenericTypeSpecifier)AStatement);
			else if (AStatement is ScalarTypeSpecifier)
				EmitScalarTypeSpecifier((ScalarTypeSpecifier)AStatement);
			else if (AStatement is RowTypeSpecifier)
				EmitRowTypeSpecifier((RowTypeSpecifier)AStatement);
			else if (AStatement is TableTypeSpecifier)
				EmitTableTypeSpecifier((TableTypeSpecifier)AStatement);
			else if (AStatement is ListTypeSpecifier)
				EmitListTypeSpecifier((ListTypeSpecifier)AStatement);
			else if (AStatement is CursorTypeSpecifier)
				EmitCursorTypeSpecifier((CursorTypeSpecifier)AStatement);
			else if (AStatement is TypeOfTypeSpecifier)
				EmitTypeOfTypeSpecifier((TypeOfTypeSpecifier)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitGenericTypeSpecifier(GenericTypeSpecifier AStatement)
		{
			Append(Keywords.Generic);
		}
		
		protected virtual void EmitScalarTypeSpecifier(ScalarTypeSpecifier AStatement)
		{
			if (AStatement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Scalar);
			else
				Append(AStatement.ScalarTypeName);
		}
		
		protected virtual void EmitRowTypeSpecifier(RowTypeSpecifier AStatement)
		{
			if (AStatement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Row);
			else
			{
				AppendFormat("{0} {1} ", Keywords.Row, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
				{
					if (LIndex > 0)
						EmitListSeparator();
					EmitNamedTypeSpecifier(AStatement.Columns[LIndex]);
				}
				AppendFormat(" {0}", Keywords.EndList);
			}
		}

		protected virtual void EmitTableTypeSpecifier(TableTypeSpecifier AStatement)
		{
			if (AStatement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Table);
			else
			{
				bool LFirst = true;
				AppendFormat("{0} {1} ", Keywords.Table, Keywords.BeginList);
				for (int LIndex = 0; LIndex < AStatement.Columns.Count; LIndex++)
				{
					if (!LFirst)
						EmitListSeparator();
					else
						LFirst = false;
					EmitNamedTypeSpecifier(AStatement.Columns[LIndex]);
				}

				AppendFormat(" {0}", Keywords.EndList);
			}
		}

		protected virtual void EmitListTypeSpecifier(ListTypeSpecifier AStatement)
		{
			if (AStatement.IsGeneric)
				AppendFormat("{0}", Keywords.List);
			else
			{
				AppendFormat("{0}{1}", Keywords.List, Keywords.BeginGroup);
				EmitTypeSpecifier(AStatement.TypeSpecifier);
				Append(Keywords.EndGroup);
			}
		}
		
		protected virtual void EmitCursorTypeSpecifier(CursorTypeSpecifier AStatement)
		{
			if (AStatement.IsGeneric)
				AppendFormat("{0} {1}", Keywords.Generic, Keywords.Cursor);
			else
			{
				AppendFormat("{0}{1}", Keywords.Cursor, Keywords.BeginGroup);
				EmitTypeSpecifier(AStatement.TypeSpecifier);
				Append(Keywords.EndGroup);
			}
		}
		
		protected virtual void EmitTypeOfTypeSpecifier(TypeOfTypeSpecifier AStatement)
		{
			AppendFormat("{0}{1}", Keywords.TypeOf, Keywords.BeginGroup);
			EmitExpression(AStatement.Expression);
			Append(Keywords.EndGroup);
		}
		
		protected virtual void EmitDeviceMapItem(DeviceMapItem AStatement)
		{
			if (AStatement is DeviceScalarTypeMapBase)
				EmitDeviceScalarTypeMapBase((DeviceScalarTypeMapBase)AStatement);
			else if (AStatement is DeviceOperatorMapBase)
				EmitDeviceOperatorMapBase((DeviceOperatorMapBase)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitDeviceScalarTypeMapBase(DeviceScalarTypeMapBase AStatement)
		{
			if (AStatement is DeviceScalarTypeMap)
				EmitDeviceScalarTypeMap((DeviceScalarTypeMap)AStatement);
			else if (AStatement is AlterDeviceScalarTypeMap)
				EmitAlterDeviceScalarTypeMap((AlterDeviceScalarTypeMap)AStatement);
			else if (AStatement is DropDeviceScalarTypeMap)
				EmitDropDeviceScalarTypeMap((DropDeviceScalarTypeMap)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitDeviceScalarTypeMap(DeviceScalarTypeMap AStatement)
		{
			AppendFormat("{0} {1}", Keywords.Type, AStatement.ScalarTypeName);
			EmitClassDefinition(AStatement.ClassDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceScalarTypeMap(DeviceScalarTypeMap AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDeviceScalarTypeMap(AStatement);
		}
		
		protected virtual void EmitAlterDeviceScalarTypeMap(AlterDeviceScalarTypeMap AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Alter, Keywords.Type, AStatement.ScalarTypeName);
			EmitAlterClassDefinition(AStatement.AlterClassDefinition);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropDeviceScalarTypeMap(DropDeviceScalarTypeMap AStatement)
		{
			AppendFormat("{0} {1} {2}", Keywords.Drop, Keywords.Type, AStatement.ScalarTypeName);
		}
		
		protected virtual void EmitDeviceOperatorMapBase(DeviceOperatorMapBase AStatement)
		{
			if (AStatement is DeviceOperatorMap)
				EmitDeviceOperatorMap((DeviceOperatorMap)AStatement);
			else if (AStatement is AlterDeviceOperatorMap)
				EmitAlterDeviceOperatorMap((AlterDeviceOperatorMap)AStatement);
			else if (AStatement is DropDeviceOperatorMap)
				EmitDropDeviceOperatorMap((DropDeviceOperatorMap)AStatement);
			else
				throw new LanguageException(LanguageException.Codes.UnknownStatementClass, AStatement.GetType().Name);
		}
		
		protected virtual void EmitDeviceOperatorMap(DeviceOperatorMap AStatement)
		{
			AppendFormat("{0} ", Keywords.Operator);
			EmitOperatorSpecifier(AStatement.OperatorSpecifier);
			if (AStatement.ClassDefinition != null)
				EmitClassDefinition(AStatement.ClassDefinition);
			EmitMetaData(AStatement.MetaData);
		}
		
		protected virtual void EmitCreateDeviceOperatorMap(DeviceOperatorMap AStatement)
		{
			AppendFormat("{0} ", Keywords.Create);
			EmitDeviceOperatorMap(AStatement);
		}
		
		protected virtual void EmitAlterDeviceOperatorMap(AlterDeviceOperatorMap AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Alter, Keywords.Operator);
			EmitOperatorSpecifier(AStatement.OperatorSpecifier);
			EmitAlterClassDefinition(AStatement.AlterClassDefinition);
			EmitAlterMetaData(AStatement.AlterMetaData);
		}
		
		protected virtual void EmitDropDeviceOperatorMap(DropDeviceOperatorMap AStatement)
		{
			AppendFormat("{0} {1} ", Keywords.Drop, Keywords.Operator);
			EmitOperatorSpecifier(AStatement.OperatorSpecifier);
		}
	}
}

