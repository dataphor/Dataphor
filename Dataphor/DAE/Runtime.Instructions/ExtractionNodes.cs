/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define NILPROPOGATION
	
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.Text;
	using System.Threading;
	using System.Collections;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
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
			
			if (!LHasEmptyKey && !APlan.ServerProcess.SuppressWarnings && !APlan.InTypeOfContext)
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
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
			{
				FIsLiteral = FIsLiteral && Nodes[LIndex].IsLiteral;
				FIsFunctional = FIsFunctional && Nodes[LIndex].IsFunctional;
				FIsDeterministic = FIsDeterministic && Nodes[LIndex].IsDeterministic;
				FIsRepeatable = FIsRepeatable && Nodes[LIndex].IsRepeatable;
			} 
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LVar = Nodes[0].Execute(AProcess);
			#if NILPROPOGATION
			if ((LVar.Value == null) || LVar.Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			using (Table LTable = (Table)LVar.Value)
			{
				LTable.Open();
				if (LTable.Next())
				{
					Row LRow = LTable.Select();
					if (LTable.Next())
						throw new CompilerException(CompilerException.Codes.InvalidRowExtractorExpression);
					return new DataVar(String.Empty, FDataType, LRow);
				}
				else
					#if NILPROPOGATION
					return new DataVar(FDataType, null);
					#else
					throw new RuntimeException(RuntimeException.Codes.RowTableEmpty);
					#endif
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
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LVar = Nodes[0].Execute(AProcess);
			#if NILPROPOGATION
			if ((LVar.Value == null) || LVar.Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			try
			{
				if (LVar.DataType is Schema.TableType)
				{
					Table LTable = (Table)LVar.Value;
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
								return new DataVar(FDataType, LRow[Location]);
							else
								return new DataVar(FDataType, null);
							#else
							int LColumnIndex = LRow.DataType.Columns.IndexOf(Identifier);
							if (LRow.HasValue(LColumnIndex))
								return new DataVar(FDataType, LRow[LColumnIndex].Copy());
							else
								return new DataVar(FDataType, null);
							#endif
						}
						finally
						{
							LRow.Dispose();
						}
					}
					else
						#if NILPROPOGATION
						return new DataVar(FDataType, null);
						#else
						throw new RuntimeException(RuntimeException.Codes.ColumnTableEmpty);
						#endif
				}
				else
				{
					Row LRow = (Row)LVar.Value;
					#if USECOLUMNLOCATIONBINDING
					if (LRow.HasValue(Location))
						return new DataVar(FDataType, ((Row)LVar.Value)[Location]);
					else
						return new DataVar(FDataType, null);
					#else
					int LColumnIndex = LRow.DataType.Columns.IndexOf(Identifier);
					if (LRow.HasValue(LColumnIndex))
						return new DataVar(FDataType, LRow[LColumnIndex].Copy());
					else
						return new DataVar(FDataType, null);
					#endif
				}
			}
			finally
			{
				if (FShouldDisposeSource)
					LVar.Value.Dispose();
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new QualifierExpression((Expression)Nodes[0].EmitStatement(AMode), new IdentifierExpression(Schema.Object.EnsureUnrooted(Identifier)));
		}
	}
	
    // operator iExists(table) : bool
    public class ExistsNode : InstructionNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.Catalog.DataTypes.SystemBoolean;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments) { return null; }		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LVar = Nodes[0].Execute(AProcess);
			#if NILPROPOGATION
			if ((LVar.Value == null) || LVar.Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			using (Table LTable = (Table)LVar.Value)
			{
				LTable.Open();
				return new DataVar(String.Empty, FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, !LTable.IsEmpty()));
			}
		}
    }
}
