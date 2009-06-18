/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator System.ObjectName(const AName : System.Name) : System.Name
	// operator System.ObjectName(const ASpecifier : System.String) : System.Name
	public class FullObjectNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).Name));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).Name));
		}
	}

	// operator ObjectExists(const AName : Name) : Boolean
	// operator ObjectExists(const ASpecifier : String) : Boolean
	public class ObjectExistsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString, false) != null));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, false) != null));
		}
	}
	
	// operator System.ObjectID(System.Name) : System.Integer
	// operator System.ObjectID(System.String) : System.Integer
	public class SystemObjectIDNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).ID));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).ID));
		}
	}
	
	// operator System.ObjectName(System.Integer) : System.String
	public class SystemObjectNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.GetObjectHeader(AArguments[0].Value.AsInt32).Name));
		}
	}
	
	// operator System.ObjectDescription(const AName : System.Name) : System.String
	// operator System.ObjectDescription(const ASpecifier : System.String) : System.String;
	// operator System.ObjectDescription(const AObjectID : System.Integer) : System.String;
	public class ObjectDescriptionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).Description));
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).Description));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.ResolveObject(AArguments[0].Value.AsInt32).Description));
		}
	}
	
	// operator System.ObjectDisplayName(const AName : System.Name) : System.String
	// operator System.ObjectDisplayName(const ASpecifier : System.String) : System.String
	// operator System.ObjectDisplayName(const AObjectID : System.Integer) : System.String
	public class ObjectDisplayNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).DisplayName));
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).DisplayName));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.GetObjectHeader(AArguments[0].Value.AsInt32).DisplayName));
		}
	}
	
	// operator System.OperatorSignature(const AName : System.Name) : System.String
	// operator System.OperatorSignature(const ASpecifier : System.String) : System.String
	// operator System.OperatorSignature(const AObjectID : System.Integer) : System.String
	public class OperatorSignatureNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Schema.Operator)Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString)).Signature.ToString()));
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Schema.Operator)Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString)).Signature.ToString()));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((Schema.Operator)AProcess.CatalogDeviceSession.ResolveCatalogObject(AArguments[0].Value.AsInt32)).Signature.ToString()));
		}
	}
	
	// overloads supported
	// operator System.ObjectMetaData(const AName : System.Name, const ATagName : System.String, ADefaultValue : System.String) : System.String
	// operator System.ObjectMetaData(const ASpecifier : System.String, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// operator System.ObjectMetaData(const AObjectID : System.Integer, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// similar to ObjectDescriptionNode
	public class ObjectMetaDataNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Schema.Object LObject = null;

			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString);
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
					LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString);
				else
					LObject = AProcess.CatalogDeviceSession.ResolveObject(AArguments[0].Value.AsInt32);
				return new DataVar
				(
					FDataType, 
					new Scalar
					(
						AProcess, 
						(Schema.ScalarType)FDataType, 
						MetaData.GetTag(LObject.MetaData, AArguments[1].Value.AsString, AArguments[2].Value.AsString)
					)
				);
			}
		}
	}
	
	// operator System.IsSystem(const AName : System.Name) : System.Boolean
	// operator System.IsSystem(const ASpecifier : System.String) : System.Boolean
	// operator System.IsSystem(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsSystemNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).IsSystem));
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.GetObjectHeader(AArguments[0].Value.AsInt32).IsSystem));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).IsSystem));
		}
	}
	
	// operator System.IsGenerated(const AName : System.Name) : System.Boolean
	// operator System.IsGenerated(const ASpecifier : String) : System.Boolean
	// operator System.IsGenerated(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsGeneratedNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.GetObjectHeader(AArguments[0].Value.AsInt32).IsGenerated));
				else if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString).IsGenerated));
				else
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString).IsGenerated));
		}
	}

	// operator System.LibraryName() : System.Name
	// operator System.LibraryName(const AName : System.Name) : System.Name
	// operator System.LibraryName(const AObjectID : System.Integer) : System.Name	
	public class SystemLibraryNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				string LLibraryName;
				if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					LLibraryName = AProcess.CatalogDeviceSession.GetObjectHeader(AArguments[0].Value.AsInt32).LibraryName;
				else
				{
					Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true);
					LLibraryName = LObject.Library == null ? String.Empty : LObject.Library.Name;
				}
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LLibraryName));
			}
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.CurrentLibrary.Name));
		}
	}
	
	// operator System.NameFromGuid(const AID : System.Guid) : System.Name
	public class SystemNameFromGuidNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Schema.Object.NameFromGuid(AArguments[0].Value.AsGuid)));
		}
	}
	
    public class SystemNameSelectorNode : InstructionNode
    {
		public static void CheckValidName(string AValue)
		{
			if (!Parser.IsValidQualifiedIdentifier(AValue))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, AValue);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LArgument = AArguments[0].Value.AsString;
				CheckValidName(LArgument);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LArgument));
			}
		}
    }
    
    public class SystemNameReadAccessorNode : InstructionNode
    {
		public SystemNameReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
				return new DataVar(FDataType, AArguments[0].Value.Copy());
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    public class SystemNameWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				string LArgument = AArguments[1].Value.AsString;
				SystemNameSelectorNode.CheckValidName(LArgument);
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LArgument));
			}
		}
    }
    
    /// <remarks> operator System.CatalogTimeStamp() : Long; </remarks>
    public class SystemCatalogTimeStampNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.Catalog.TimeStamp));
		}
	}
    
    /// <remarks> operator System.CacheTimeStamp() : Long; </remarks>
    public class SystemCacheTimeStampNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.Catalog.CacheTimeStamp));
		}
    }
    
    /// <remarks> operator System.DerivationTimeStamp() : Long; </remarks>
    public class SystemDerivationTimeStampNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.Catalog.DerivationTimeStamp));
		}
    }
    
	// operator UpdateTimeStamps();
	public class SystemUpdateTimeStampsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// This call is no longer necessary because the catalog timestamp only controls library information now
			//AProcess.ServerSession.Server.Catalog.UpdateTimeStamp(); 
			AProcess.ServerSession.Server.Catalog.UpdateCacheTimeStamp();
			AProcess.ServerSession.Server.Catalog.UpdatePlanCacheTimeStamp();
			AProcess.ServerSession.Server.Catalog.UpdateDerivationTimeStamp();
			return null;
		}
	}

    // operator Script(const AName : Name) : String
    // operator Script(const ASpecifier : String) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator Script(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();

			Schema.Object LObject;		
			if (AArguments[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemName))
				LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true);
			else
				LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString, true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? AArguments[1].Value.AsBoolean : false;
			bool LIncludeObject = AArguments.Length > 2 ? AArguments[2].Value.AsBoolean : true;
				
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LEmitter.Emit(AProcess.Plan.Catalog.EmitStatement(AProcess, EmitMode.ForCopy, new string[]{LObject.Name}, String.Empty, true, true, LIncludeDependents, LIncludeObject))));
		}
    }
    
    // operator ScriptExpression(const AExpression : String) : String
    public class SystemScriptExpressionNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			AProcess.Context.PushWindow(0);
			try
			{
				IServerExpressionPlan LPlan = ((IServerProcess)AProcess).PrepareExpression(AArguments[0].Value.AsString, null);
				try
				{
					LPlan.CheckCompiled();
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LEmitter.Emit(LPlan.Catalog.EmitStatement(AProcess, EmitMode.ForCopy, new string[]{LPlan.TableVar.Name}))));
				}
				finally
				{
					((IServerProcess)AProcess).UnprepareExpression(LPlan);
				}
			}
			finally
			{
				AProcess.Context.PopWindow();
			}
		}
    }
    
    /// <remarks>operator ScriptData(AExpression : System.String) : System.String;</remarks>
    public class SystemScriptDataNode : InstructionNode
    {
		// TODO: Update this to work with non-scalar-valued attributes

		private bool IsParserLiteral(ServerProcess AProcess, Schema.ScalarType AType)
		{
			switch (AType.Name)
			{
				case "System.Boolean" :
				case "System.Byte" :
				case "System.Short" :
				case "System.Integer" :
				case "System.Long" :
				case "System.Decimal" :
				case "System.Money" :
				case "System.String" :
				#if USEISTRING
				case "System.IString" : 
				#endif
					return true;
				default : return false;
			}
		}
		
		// returns true if all the properties of the representation are parser literals, or can be specified in terms of parser literals, recursively
		private bool IsRepresentationLiteral(ServerProcess AProcess, Schema.Representation ARepresentation)
		{
			foreach (Schema.Property LProperty in ARepresentation.Properties)
				if ((!(LProperty.DataType is Schema.ScalarType)) || !IsParserLiteral(AProcess, (Schema.ScalarType)LProperty.DataType))
					return false;
			return true;
		}
		
		private Expression EmitScalarRepresentationSelector(ServerProcess AProcess, Schema.Representation ARepresentation, Scalar AValue)
		{
			CallExpression LSelector = new CallExpression();
			LSelector.Identifier = ARepresentation.Selector.OperatorName;
			foreach (Schema.Property LProperty in ARepresentation.Properties)
				LSelector.Expressions.Add(EmitScalarSelector(AProcess, (Scalar)Compiler.EmitCallNode(AProcess.Plan, LProperty.ReadAccessor.OperatorName, new PlanNode[]{new ValueNode(AValue)}).Execute(AProcess).Value, (Schema.ScalarType)LProperty.DataType));
			return LSelector;
		}
		
		private Expression EmitScalarSelector(ServerProcess AProcess, Scalar AValue, Schema.ScalarType ADataType)
		{
			// if the value is a parser literal, emit the value expression for it,
			// search for a selector in terms of parser literals, recursively
			// if a parser literal can be converted to the value, emit the expression to convert it
			// Compile the template to use for each row so the search does not have to take place for each row.
			switch (ADataType.Name)
			{
				case "System.Boolean" : return new ValueExpression(AValue.AsBoolean);
				case "System.Byte" : return new ValueExpression((int)AValue.AsByte);
				case "System.Short" : return new ValueExpression((int)AValue.AsInt16);
				case "System.Integer" : return new ValueExpression((int)AValue.AsInt32);
				case "System.Long" : return new ValueExpression(AValue.AsInt64, TokenType.Integer);
				case "System.Decimal" : return new ValueExpression(AValue.AsDecimal);
				case "System.Money" : 
					if (AValue.AsDecimal < 0)
						return new UnaryExpression(Alphora.Dataphor.DAE.Language.D4.Instructions.Negate, new ValueExpression(-(AValue.AsDecimal), TokenType.Money));
					else
						return new ValueExpression(AValue.AsDecimal, TokenType.Money);
				case "System.String" : return new ValueExpression(AValue.AsString);
				#if USEISTRING
				case "System.IString" : return new ValueExpression(AValue.AsString, LexerToken.IString);
				#endif
				default :
				{
					foreach (Schema.Representation LRepresentation in AValue.DataType.Representations)
						if (IsRepresentationLiteral(AProcess, LRepresentation))
							return EmitScalarRepresentationSelector(AProcess, LRepresentation, AValue);
					break;
				}
			}
			
			Error.Fail("Unable to construct a literal selector for values of type {0}.", AValue.DataType.Name);
			return null;
		}
		
		private RowSelectorExpressionBase EmitRowSelector(ServerProcess AProcess, Row ARow)
		{
			DAE.Language.D4.RowSelectorExpressionBase LSelector = ARow.DataType is Schema.RowType ? (RowSelectorExpressionBase)new RowSelectorExpression() : new EntrySelectorExpression();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				if (ARow.HasValue(LIndex))
					LSelector.Expressions.Add(new NamedColumnExpression(EmitScalarSelector(AProcess, (Scalar)ARow[LIndex], (Schema.ScalarType)ARow.DataType.Columns[LIndex].DataType), ARow.DataType.Columns[LIndex].Name));
				else
					LSelector.Expressions.Add(new NamedColumnExpression(new AsExpression(new ValueExpression(null, TokenType.Nil), ARow.DataType.Columns[LIndex].DataType.EmitSpecifier(EmitMode.ForCopy)), ARow.DataType.Columns[LIndex].Name));
			return LSelector;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			string LExpression = AArguments[0].Value.AsString;
			CursorNode LNode = (CursorNode)Compiler.BindNode(AProcess.Plan, Compiler.OptimizeNode(AProcess.Plan, Compiler.CompileCursor(AProcess.Plan, new Parser().ParseCursorDefinition(LExpression))));
			TableSelectorExpressionBase LSelector = LNode.SourceNode.DataType is Schema.TableType ? (TableSelectorExpressionBase)new TableSelectorExpression() : new PresentationSelectorExpression();
			LSelector.TypeSpecifier = LNode.SourceNode.DataType.EmitSpecifier(EmitMode.ForCopy);
			LSelector.Keys.Add(LNode.SourceNode.TableVar.FindClusteringKey().EmitStatement(EmitMode.ForCopy));
			Table LTable = (Table)LNode.SourceNode.Execute(AProcess).Value;
			try
			{
				Row LRow = new Row(AProcess, LTable.DataType.RowType);
				try
				{
					while (LTable.Next())
					{
						LTable.Select(LRow);
						
						LSelector.Expressions.Add(EmitRowSelector(AProcess, LRow));
					}
					
					return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, LEmitter.Emit(LSelector)));
				}
				finally
				{
					LRow.Dispose();
				}
			}
			finally
			{
				LTable.Dispose();
			}
		}
    }
    
    /// <remarks>operator ScriptLibrary(ALibraryName : System.Name) : System.String;</remarks>
    public class SystemScriptLibraryNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, AProcess.ServerSession.Server.ScriptLibrary(AProcess, AArguments[0].Value.AsString)));
		}
    }
    
    /// <remarks>operator ScriptCatalog() : System.String;</remarks>
	public class SystemScriptCatalogNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, AProcess.ServerSession.Server.ScriptCatalog(AProcess)));
		}
	}
	
	/// <remarks>operator ScriptServerState() : System.String;</remarks>
	public class SystemScriptServerStateNode : InstructionNode
	{
		public override DataVar  InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			 return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)DataType, AProcess.ServerSession.Server.ScriptServerState(AProcess)));
		}
	}

    // operator ScriptDrop(const AName : Name) : String
    // operator ScriptDrop(const ASpecifier : String) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptDropNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			
			Schema.Object LObject;
			
			if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemName))
				LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true);
			else
				LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, AArguments[0].Value.AsString, true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? AArguments[1].Value.AsBoolean : true;
			bool LIncludeObject = AArguments.Length > 2 ? AArguments[2].Value.AsBoolean : true;
		
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, LEmitter.Emit(AProcess.Plan.Catalog.EmitDropStatement(AProcess, new string[]{LObject.Name}, String.Empty, true, true, LIncludeDependents, LIncludeObject))));
		}
    }
    
    /// <remarks>operator ScriptDropLibrary(AName : System.Name) : System.String;</remarks>
    public class SystemScriptDropLibraryNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, AProcess.ServerSession.Server.ScriptDropLibrary(AProcess, AArguments[0].Value.AsString)));
		}
    }
    
    /// <remarks>operator ScriptDropCatalog() : System.String;</remarks>
    public class SystemScriptDropCatalogNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, AProcess.ServerSession.Server.ScriptDropCatalog(AProcess)));
		}
    }

	/// <remarks>operator ScriptLibraryChanges(const AOldCatalogDirectory : String, const ALibraryName : Name) : String;</remarks>
	public class SystemScriptLibraryChangesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, null);
			//return new DataVar(FDataType, new Scalar(AProcess, AProcess.DataTypes.SystemString, AProcess.ServerSession.Server.ScriptLibraryChanges(AArguments[0].Value.AsString, AArguments[1].Value.AsString)));
		}
	}

	// operator Diagnostics.IsCatalogObjectLoaded(const AObjectID : Integer) : Boolean;
	public class SystemIsCatalogObjectLoadedNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			else
			#endif
			{
				if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
					
				if (Nodes[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemInteger))
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.ResolveCachedCatalogObject(AArguments[0].Value.AsInt32, false) != null));

				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.CatalogDeviceSession.ResolveCachedCatalogObject(AArguments[0].Value.AsString, false) != null));
			}
		}
	}

	// operator ClearCatalogObject(const AObjectID : Integer);
	// operator ClearCatalogObject(const AObjectName : Name);
    public class SystemClearCatalogObjectNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);

			Schema.CatalogObject LCatalogObject;
			if (Nodes[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemInteger))
				LCatalogObject = AProcess.CatalogDeviceSession.ResolveCachedCatalogObject(AArguments[0].Value.AsInt32);
			else
				LCatalogObject = AProcess.CatalogDeviceSession.ResolveCachedCatalogObject(AArguments[0].Value.AsString);
				
			AProcess.CatalogDeviceSession.ClearCachedCatalogObject(LCatalogObject);
				
			return null;
		}
    }
    
	// operator ClearLibrary(string ALibraryName);
    public class SystemClearLibraryNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
			string LLibraryName = AProcess.Plan.Catalog.Libraries[AArguments[0].Value.AsString].Name;
			Schema.Objects LObjects = new Schema.Objects();
			lock (AProcess.Plan.Catalog)
			{
				for (int LIndex = 0; LIndex < AProcess.Plan.Catalog.Count; LIndex++)
					if ((AProcess.Plan.Catalog[LIndex].Library != null) && (AProcess.Plan.Catalog[LIndex].Library.Name == LLibraryName))
						LObjects.Add(AProcess.Plan.Catalog[LIndex]);
			}
			
			AProcess.CatalogDeviceSession.ClearCachedCatalogObjects(LObjects);
				
			return null;
		}
    }
    
	// operator ClearCatalog();
    public class SystemClearCatalogNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
			AProcess.ServerSession.Server.ClearCatalog();
			AProcess.ServerSession.SetUser(AProcess.ServerSession.Server.AdminUser);
			AProcess.ExecutingPlan.Plan.UpdateSecurityContexts(AProcess.ServerSession.User);
			return null;
		}
    }
    
    // operator DependentObjects(const AObjectID : Integer) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectID : Integer, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator DependentObjects(const AObjectName : Name, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    public class SystemDependentObjectsNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Object_ID", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.Catalog.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					int LID;
					if (Nodes[0].DataType.Is(AProcess.DataTypes.SystemName))
						LID = Compiler.ResolveCatalogIdentifier(AProcess.Plan, Nodes[0].Execute(AProcess).Value.AsString, true).ID;
					else
						LID = Nodes[0].Execute(AProcess).Value.AsInt32;
						
					List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(LID, Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsBoolean : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0].AsInt32 = LHeaders[LIndex].ID;
						LRow[1].AsString = LHeaders[LIndex].Name;
						LRow[2].AsString = LHeaders[LIndex].Description;
						LRow[3].AsInt32 = LHeaders[LIndex].Sequence;
						LRow[4].AsInt32 = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    // operator RequiredObjects(const AObjectID : Integer) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectID : Integer, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectName : Name) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    // operator RequiredObjects(const AObjectName : Name, const ARecursive : Boolean) : table { Object_ID : Integer, Object_Name : Name, Object_Description : String, Sequence : Integer, Level : Integer };
    public class SystemRequiredObjectsNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Object_ID", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Object_Name", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Object_Description", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.Catalog.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredObject(ServerProcess AProcess, Table ATable, Row ARow, Schema.Object AObject, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0].AsInt32 = AObject.ID;
			ARow[1].AsString = AObject.Name;
			ARow[2].AsString = AObject.Description;
			ARow[3].AsInt32 = ASequence;
			ARow[4].AsInt32 = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateRequiredObjects(AProcess, ATable, ARow, AObject, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateRequiredObjects(ServerProcess AProcess, Table ATable, Row ARow, Schema.Object AObject, bool ARecursive, ref int ASequence, int ALevel)
		{
			if (AObject.HasDependencies())
				for (int LIndex = 0; LIndex < AObject.Dependencies.Count; LIndex++)
				{
					ASequence += 1;
					PopulateRequiredObject(AProcess, ATable, ARow, AObject.Dependencies.ResolveObject(AProcess, LIndex), ARecursive, ref ASequence, ALevel);
				}
		}

		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					
					int LID;
					if (Nodes[0].DataType.Is(AProcess.DataTypes.SystemName))
						LID = Compiler.ResolveCatalogIdentifier(AProcess.Plan, Nodes[0].Execute(AProcess).Value.AsString, true).ID;
					else
						LID = Nodes[0].Execute(AProcess).Value.AsInt32;
						
					List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependencies(LID, Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsBoolean : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0].AsInt32 = LHeaders[LIndex].ID;
						LRow[1].AsString = LHeaders[LIndex].Name;
						LRow[2].AsString = LHeaders[LIndex].Description;
						LRow[3].AsInt32 = LHeaders[LIndex].Sequence;
						LRow[4].AsInt32 = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    // operator DependentLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator DependentLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemDependentLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.Catalog.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], false));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateLibrary(ServerProcess AProcess, Table ATable, Row ARow, Schema.LoadedLibrary ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0].AsString = ALibrary.Name;
			ARow[1].AsInt32 = ASequence;
			ARow[2].AsInt32 = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateDependentLibraries(AProcess, ATable, ARow, ALibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateDependentLibraries(ServerProcess AProcess, Table ATable, Row ARow, Schema.LoadedLibrary ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			foreach (Schema.LoadedLibrary LLibrary in ALibrary.RequiredByLibraries)
			{
				ASequence += 1;
				PopulateLibrary(AProcess, ATable, ARow, LLibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					int LSequence = 0;
					PopulateDependentLibraries
					(
						AProcess, 
						LResult, 
						LRow,
						AProcess.CatalogDeviceSession.ResolveLoadedLibrary(Nodes[0].Execute(AProcess).Value.AsString), 
						Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsBoolean : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }

    // operator RequiredLibraries(const ALibraryName : Name) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    // operator RequiredLibraries(const ALibraryName : Name, const ARecursive : Boolean) : table { Library_Name : Name, Sequence : Integer, Level : Integer };
    public class SystemRequiredLibrariesNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Library_Name", APlan.Catalog.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Level", APlan.Catalog.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));
			Schema.Order LOrder = new Schema.Order();
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Level"], true));
			LOrder.Columns.Add(new Schema.OrderColumn(TableVar.Columns["Sequence"], true));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredLibrary(ServerProcess AProcess, Table ATable, Row ARow, Schema.Library ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0].AsString = ALibrary.Name;
			ARow[1].AsInt32 = ASequence;
			ARow[2].AsInt32 = ALevel;
			ATable.Insert(ARow);
			if (ARecursive)
			{
				ALevel += 1;
				PopulateRequiredLibraries(AProcess, ATable, ARow, ALibrary, ARecursive, ref ASequence, ALevel);
			}
		}
		
		private void PopulateRequiredLibraries(ServerProcess AProcess, Table ATable, Row ARow, Schema.Library ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
			{
				ASequence += 1;
				PopulateRequiredLibrary(AProcess, ATable, ARow, AProcess.Plan.Catalog.Libraries[LLibrary.Name], ARecursive, ref ASequence, ALevel);
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					int LSequence = 0;
					PopulateRequiredLibraries
					(
						AProcess, 
						LResult, 
						LRow, 
						AProcess.Plan.Catalog.Libraries[Nodes[0].Execute(AProcess).Value.AsString],
						Nodes.Count == 2 ? Nodes[1].Execute(AProcess).Value.AsBoolean : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
    
    public class SystemClearStoreCountersNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.CatalogDeviceSession.ClearStoreCounters();
			return null;
		}
    }
    
    // operator GetStoreCounters() : table { Sequence : Integer, Operation : String, TableName : String, IndexName : String, IsMatched : Boolean, IsRanged : Boolean, Duration : TimeSpan };
    public class SystemGetStoreCountersNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Operation", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TableName", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IndexName", APlan.Catalog.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsMatched", APlan.Catalog.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRanged", APlan.Catalog.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsUpdatable", APlan.Catalog.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Duration", APlan.Catalog.DataTypes.SystemTimeSpan));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

			TableVar.DetermineRemotable(APlan.ServerProcess);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			LocalTable LResult = new LocalTable(this, AProcess);
			try
			{
				LResult.Open();
				
				// Populate the result
				Row LRow = new Row(AProcess, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;
					AProcess.CatalogDeviceSession.PopulateStoreCounters(LResult, LRow);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return new DataVar(LResult.DataType, LResult);
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
}
