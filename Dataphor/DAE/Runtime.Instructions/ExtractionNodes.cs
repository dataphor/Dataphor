/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define NILPROPOGATION
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	/*
		Extraction Operators
		    Extract(table{}): row{}
			Extract(table{}, ColumnName): scalar
			Extract(row{}, ColumnName): scalar
			
		// These operators cannot be overloaded, nor can they be invoked by call
	*/
	
	// operator Extract(table{}): row{}
	public class ExtractRowNode : PlanNode
	{		
		public ExtractRowNode() : base()
		{
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			TableNode LSourceNode = (TableNode)Nodes[0];
			bool LHasEmptyKey = false;
			foreach (Schema.Key LKey in LSourceNode.TableVar.Keys)
				if (LKey.Columns.Count == 0)
				{
					LHasEmptyKey = true;
					break;
				}
			
			if (!LHasEmptyKey && !APlan.SuppressWarnings && !APlan.InTypeOfContext)
				APlan.Messages.Add(new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression, CompilerErrorLevel.Warning, APlan.CurrentStatement()));
			FDataType = LSourceNode.TableVar.DataType.RowType;
		}
		
		public override Schema.IDataType DataType { get { return base.DataType; } set { base.DataType = value; } }
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = true;
			for (int LIndex = 0; LIndex < NodeCount; LIndex++)
			{
				FIsLiteral = FIsLiteral && Nodes[LIndex].IsLiteral;
				FIsFunctional = FIsFunctional && Nodes[LIndex].IsFunctional;
				FIsDeterministic = FIsDeterministic && Nodes[LIndex].IsDeterministic;
				FIsRepeatable = FIsRepeatable && Nodes[LIndex].IsRepeatable;
			} 
		}
		
		public override object InternalExecute(Program AProgram)
		{
			Table LTable = Nodes[0].Execute(AProgram) as Table;
			#if NILPROPOGATION
			if ((LTable == null))
				return null;
			#endif
			try
			{
				LTable.Open();
				if (LTable.Next())
				{
					Row LRow = LTable.Select();
					try
					{
						if (LTable.Next())
							throw new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression);
						return LRow;
					}
					catch
					{
						LRow.Dispose();
						throw;
					}
				}
				else
					#if NILPROPOGATION
					return null;
					#else
					throw new RuntimeException(RuntimeException.Codes.RowTableEmpty);
					#endif
			}
			finally
			{
				LTable.Dispose();
			}
		}
		
		private D4IndexerExpression FIndexerExpression;
		public D4IndexerExpression IndexerExpression
		{
			get { return FIndexerExpression; }
			set { FIndexerExpression = value; }
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			if (FIndexerExpression != null)
				return FIndexerExpression;
			else
			{
				D4IndexerExpression LIndexerExpression = new D4IndexerExpression();
				LIndexerExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
				return LIndexerExpression;
			}
		}
	}

	//	Extract(row{}, ColumnName): scalar
	//  Extract(table{}, ColumnName): scalar
	public class ExtractColumnNode : PlanNode
	{		
		public override void DetermineCharacteristics(Plan APlan)
		{
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = true;
			for (int LIndex = 0; LIndex < NodeCount; LIndex++)
			{
				FIsLiteral = FIsLiteral && Nodes[LIndex].IsLiteral;
				FIsFunctional = FIsFunctional && Nodes[LIndex].IsFunctional;
				FIsDeterministic = FIsDeterministic && Nodes[LIndex].IsDeterministic;
				FIsRepeatable = FIsRepeatable && Nodes[LIndex].IsRepeatable;
			} 
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			#if USECOLUMNLOCATIONBINDING
			if (Nodes[0].DataType is Schema.RowType)
				FDataType = ((Schema.RowType)Nodes[0].DataType).Columns[Location].DataType;
			else
				FDataType = ((Schema.TableType)Nodes[0].DataType).Columns[Location].DataType;
			#else
			if (Nodes[0].DataType is Schema.RowType)
			{
				FDataType = ((Schema.RowType)Nodes[0].DataType).Columns[Identifier].DataType;
				if (Nodes[0] is StackReferenceNode)
				{
					FShouldDisposeSource = false;
					((StackReferenceNode)Nodes[0]).ByReference = true;
				}
			}
			else
				FDataType = ((Schema.TableType)Nodes[0].DataType).Columns[Identifier].DataType;
			#endif
		}
		
		#if USECOLUMNLOCATIONBINDING
		public int Location = -1;
		#endif

		public string Identifier = String.Empty;		
		
		private bool FShouldDisposeSource = true;
		
		public override object InternalExecute(Program AProgram)
		{
			object LObject = Nodes[0].Execute(AProgram);
			#if NILPROPOGATION
			if (LObject == null)
				return null;
			#endif

			Table LTable = LObject as Table;
			if (LTable != null)
			{
				try
				{
					LTable.Open();
					if (LTable.Next())
					{
						Row LRow = LTable.Select();
						try
						{
							if (LTable.Next())
								throw new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression);
							#if USECOLUMNLOCATIONBINDING
							if (LRow.HasValue(Location))
								return LRow[Location].AsNative;
							else
								return null;
							#else
							int LColumnIndex = LRow.DataType.Columns.IndexOf(Identifier);
							if (LRow.HasValue(LColumnIndex))
								return LRow[LColumnIndex];
							else
								return null;
							#endif
						}
						finally
						{
							LRow.Dispose();
						}
					}
					else
						#if NILPROPOGATION
						return null;
						#else
						throw new RuntimeException(RuntimeException.Codes.ColumnTableEmpty);
						#endif
				}
				finally
				{
					if (FShouldDisposeSource)
						LTable.Dispose();
				}
			}
			else
			{
				Row LRow = (Row)LObject;
				try
				{
					#if USECOLUMNLOCATIONBINDING
					if (LRow.HasValue(Location))
						return LRow[Location].AsNative;
					else
						return null;
					#else
					int LColumnIndex = LRow.DataType.Columns.IndexOf(Identifier);
					if (!LRow.IsNil && LRow.HasValue(LColumnIndex))
						return LRow[LColumnIndex];
					else
						return null;
					#endif
				}
				finally
				{
					if (FShouldDisposeSource)
						LRow.Dispose();
				}
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new QualifierExpression((Expression)Nodes[0].EmitStatement(AMode), new IdentifierExpression(Schema.Object.EnsureUnrooted(Identifier)));
		}
	}
	
    // operator iExists(table) : bool
    public class ExistsNode : InstructionNodeBase
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.DataTypes.SystemBoolean;
		}
		
		public override object InternalExecute(Program AProgram)
		{
			Table LTable = Nodes[0].Execute(AProgram) as Table;
			#if NILPROPOGATION
			if (LTable == null)
				return null;
			#endif
			try
			{
				LTable.Open();
				return !LTable.IsEmpty();
			}
			finally
			{
				LTable.Dispose();
			}
		}
    }
}
