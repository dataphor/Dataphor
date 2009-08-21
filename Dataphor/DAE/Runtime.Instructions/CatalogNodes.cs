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

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator System.ObjectName(const AName : System.Name) : System.Name
	// operator System.ObjectName(const ASpecifier : System.String) : System.Name
	public class FullObjectNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).Name;
				else
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).Name;
		}
	}

	// operator ObjectExists(const AName : Name) : Boolean
	// operator ObjectExists(const ASpecifier : String) : Boolean
	public class ObjectExistsNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1, false) != null;
				else
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1, false) != null;
		}
	}
	
	// operator System.ObjectID(System.Name) : System.Integer
	// operator System.ObjectID(System.String) : System.Integer
	public class SystemObjectIDNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).ID;
				else
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).ID;
		}
	}
	
	// operator System.ObjectName(System.Integer) : System.String
	public class SystemObjectNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return AProcess.CatalogDeviceSession.GetObjectHeader((int)AArgument1).Name;
		}
	}
	
	// operator System.ObjectDescription(const AName : System.Name) : System.String
	// operator System.ObjectDescription(const ASpecifier : System.String) : System.String;
	// operator System.ObjectDescription(const AObjectID : System.Integer) : System.String;
	public class ObjectDescriptionNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).Description;
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemName))
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).Description;
				else
					return AProcess.CatalogDeviceSession.ResolveObject((int)AArgument1).Description;
		}
	}
	
	// operator System.ObjectDisplayName(const AName : System.Name) : System.String
	// operator System.ObjectDisplayName(const ASpecifier : System.String) : System.String
	// operator System.ObjectDisplayName(const AObjectID : System.Integer) : System.String
	public class ObjectDisplayNameNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).DisplayName;
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemName))
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).DisplayName;
				else
					return AProcess.CatalogDeviceSession.GetObjectHeader((int)AArgument1).DisplayName;
		}
	}
	
	// operator System.OperatorSignature(const AName : System.Name) : System.String
	// operator System.OperatorSignature(const ASpecifier : System.String) : System.String
	// operator System.OperatorSignature(const AObjectID : System.Integer) : System.String
	public class OperatorSignatureNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return ((Schema.Operator)Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1)).Signature.ToString();
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemName))
					return ((Schema.Operator)Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1)).Signature.ToString();
				else
					return ((Schema.Operator)AProcess.CatalogDeviceSession.ResolveCatalogObject((int)AArgument1)).Signature.ToString();
		}
	}
	
	// overloads supported
	// operator System.ObjectMetaData(const AName : System.Name, const ATagName : System.String, ADefaultValue : System.String) : System.String
	// operator System.ObjectMetaData(const ASpecifier : System.String, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// operator System.ObjectMetaData(const AObjectID : System.Integer, const ATagName : System.String, ADefaultValue : System.String) : System.String;
	// similar to ObjectDescriptionNode
	public class ObjectMetaDataNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			Schema.Object LObject = null;

			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
				return null;
			else
			#endif
			{
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArguments[0]);
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemName))
					LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArguments[0]);
				else
					LObject = AProcess.CatalogDeviceSession.ResolveObject((int)AArguments[0]);
				return MetaData.GetTag(LObject.MetaData, (string)AArguments[1], (string)AArguments[2]);
			}
		}
	}
	
	// operator System.IsSystem(const AName : System.Name) : System.Boolean
	// operator System.IsSystem(const ASpecifier : System.String) : System.Boolean
	// operator System.IsSystem(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsSystemNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).IsSystem;
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					return AProcess.CatalogDeviceSession.GetObjectHeader((int)AArgument1).IsSystem;
				else
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).IsSystem;
		}
	}
	
	// operator System.IsGenerated(const AName : System.Name) : System.Boolean
	// operator System.IsGenerated(const ASpecifier : String) : System.Boolean
	// operator System.IsGenerated(const AObjectID : System.Integer) : System.Boolean
	public class ObjectIsGeneratedNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					return AProcess.CatalogDeviceSession.GetObjectHeader((int)AArgument1).IsGenerated;
				else if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemString))
					return Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArgument1).IsGenerated;
				else
					return Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArgument1).IsGenerated;
		}
	}

	// operator System.LibraryName() : System.Name
	// operator System.LibraryName(const AName : System.Name) : System.Name
	// operator System.LibraryName(const AObjectID : System.Integer) : System.Name	
	public class SystemLibraryNameNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AArguments.Length == 1)
			{
				string LLibraryName;
				if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemInteger))
					LLibraryName = AProcess.CatalogDeviceSession.GetObjectHeader((int)AArguments[0]).LibraryName;
				else
				{
					Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArguments[0], true);
					LLibraryName = LObject.Library == null ? String.Empty : LObject.Library.Name;
				}
				return LLibraryName;
			}
			else
				return AProcess.Plan.CurrentLibrary.Name;
		}
	}
	
	// operator System.NameFromGuid(const AID : System.Guid) : System.Name
	public class SystemNameFromGuidNode : UnaryInstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return Schema.Object.NameFromGuid((Guid)AArgument1);
		}
	}
	
    public class SystemNameSelectorNode : UnaryInstructionNode
    {
		public static void CheckValidName(string AValue)
		{
			if (!Parser.IsValidQualifiedIdentifier(AValue))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, AValue);
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
			{
				string LArgument = (string)AArgument1;
				CheckValidName(LArgument);
				return LArgument;
			}
		}
    }
    
    public class SystemNameReadAccessorNode : UnaryInstructionNode
    {
		public SystemNameReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			else
			#endif
				return AArgument1;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    public class SystemNameWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			else
			#endif
			{
				string LArgument = (string)AArgument2;
				SystemNameSelectorNode.CheckValidName(LArgument);
				return LArgument;
			}
		}
    }
    
    /// <remarks> operator System.CatalogTimeStamp() : Long; </remarks>
    public class SystemCatalogTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			return AProcess.Plan.Catalog.TimeStamp;
		}
	}
    
    /// <remarks> operator System.CacheTimeStamp() : Long; </remarks>
    public class SystemCacheTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			return AProcess.Plan.Catalog.CacheTimeStamp;
		}
    }
    
    /// <remarks> operator System.DerivationTimeStamp() : Long; </remarks>
    public class SystemDerivationTimeStampNode : NilaryInstructionNode
    {
		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			return AProcess.Plan.Catalog.DerivationTimeStamp;
		}
    }
    
	// operator UpdateTimeStamps();
	public class SystemUpdateTimeStampsNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(ServerProcess AProcess)
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();

			Schema.Object LObject;		
			if (Operator.Operands[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemName))
				LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArguments[0], true);
			else
				LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArguments[0], true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? (bool)AArguments[1] : false;
			bool LIncludeObject = AArguments.Length > 2 ? (bool)AArguments[2] : true;
				
			return 
				LEmitter.Emit
				(
					AProcess.Plan.Catalog.EmitStatement
					(
						AProcess.CatalogDeviceSession, 
						EmitMode.ForCopy, 
						new string[] { LObject.Name }, 
						String.Empty, 
						true, 
						true, 
						LIncludeDependents, 
						LIncludeObject
					)
				);
		}
    }
    
    // operator ScriptExpression(const AExpression : String) : String
    public class SystemScriptExpressionNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			AProcess.Context.PushWindow(0);
			try
			{
				IServerExpressionPlan LPlan = ((IServerProcess)AProcess).PrepareExpression((string)AArguments[0], null);
				try
				{
					LPlan.CheckCompiled();
					return LEmitter.Emit(LPlan.Catalog.EmitStatement(AProcess.CatalogDeviceSession, EmitMode.ForCopy, new string[] { LPlan.TableVar.Name } ));
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
		
		private Expression EmitScalarRepresentationSelector(ServerProcess AProcess, Schema.Representation ARepresentation, object AValue)
		{
			CallExpression LSelector = new CallExpression();
			LSelector.Identifier = ARepresentation.Selector.OperatorName;
			foreach (Schema.Property LProperty in ARepresentation.Properties)
				LSelector.Expressions.Add
				(
					EmitScalarSelector
					(
						AProcess, 
						Compiler.EmitCallNode
						(
							AProcess.Plan, 
							LProperty.ReadAccessor.OperatorName, 
							new PlanNode[] { new ValueNode(ARepresentation.ScalarType, AValue) }
						).Execute(AProcess), 
						(Schema.ScalarType)LProperty.DataType
					)
				);
			return LSelector;
		}
		
		private Expression EmitScalarSelector(ServerProcess AProcess, object AValue, Schema.ScalarType ADataType)
		{
			// if the value is a parser literal, emit the value expression for it,
			// search for a selector in terms of parser literals, recursively
			// if a parser literal can be converted to the value, emit the expression to convert it
			// Compile the template to use for each row so the search does not have to take place for each row.
			switch (ADataType.Name)
			{
				case "System.Boolean" : return new ValueExpression((bool)AValue);
				case "System.Byte" : return new ValueExpression((int)(byte)AValue);
				case "System.Short" : return new ValueExpression((int)(short)AValue);
				case "System.Integer" : return new ValueExpression((int)AValue);
				case "System.Long" : return new ValueExpression((long)AValue, TokenType.Integer);
				case "System.Decimal" : return new ValueExpression((decimal)AValue);
				case "System.Money" : 
					if ((decimal)AValue < 0)
						return new UnaryExpression(Alphora.Dataphor.DAE.Language.D4.Instructions.Negate, new ValueExpression(-((decimal)AValue), TokenType.Money));
					else
						return new ValueExpression((decimal)AValue, TokenType.Money);
				case "System.String" : return new ValueExpression((string)AValue);
				#if USEISTRING
				case "System.IString" : return new ValueExpression((string)AValue, LexerToken.IString);
				#endif
				default :
				{
					foreach (Schema.Representation LRepresentation in ADataType.Representations)
						if (IsRepresentationLiteral(AProcess, LRepresentation))
							return EmitScalarRepresentationSelector(AProcess, LRepresentation, AValue);
					break;
				}
			}
			
			Error.Fail("Unable to construct a literal selector for values of type {0}.", ADataType.Name);
			return null;
		}
		
		private RowSelectorExpressionBase EmitRowSelector(ServerProcess AProcess, Row ARow)
		{
			DAE.Language.D4.RowSelectorExpressionBase LSelector = ARow.DataType is Schema.RowType ? (RowSelectorExpressionBase)new RowSelectorExpression() : new EntrySelectorExpression();
			for (int LIndex = 0; LIndex < ARow.DataType.Columns.Count; LIndex++)
				if (ARow.HasValue(LIndex))
					LSelector.Expressions.Add(new NamedColumnExpression(EmitScalarSelector(AProcess, ARow[LIndex], (Schema.ScalarType)ARow.DataType.Columns[LIndex].DataType), ARow.DataType.Columns[LIndex].Name));
				else
					LSelector.Expressions.Add(new NamedColumnExpression(new AsExpression(new ValueExpression(null, TokenType.Nil), ARow.DataType.Columns[LIndex].DataType.EmitSpecifier(EmitMode.ForCopy)), ARow.DataType.Columns[LIndex].Name));
			return LSelector;
		}
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
		
			string LExpression = (string)AArguments[0];
			CursorNode LNode = (CursorNode)Compiler.BindNode(AProcess.Plan, Compiler.OptimizeNode(AProcess.Plan, Compiler.CompileCursor(AProcess.Plan, new Parser().ParseCursorDefinition(LExpression))));
			TableSelectorExpressionBase LSelector = LNode.SourceNode.DataType is Schema.TableType ? (TableSelectorExpressionBase)new TableSelectorExpression() : new PresentationSelectorExpression();
			LSelector.TypeSpecifier = LNode.SourceNode.DataType.EmitSpecifier(EmitMode.ForCopy);
			LSelector.Keys.Add(LNode.SourceNode.TableVar.FindClusteringKey().EmitStatement(EmitMode.ForCopy));
			Table LTable = (Table)LNode.SourceNode.Execute(AProcess);
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
					
					return LEmitter.Emit(LSelector);
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return AProcess.ServerSession.Server.ScriptLibrary(AProcess.CatalogDeviceSession, (string)AArguments[0]);
		}
    }
    
    /// <remarks>operator ScriptCatalog() : System.String;</remarks>
	public class SystemScriptCatalogNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return AProcess.ServerSession.Server.ScriptCatalog(AProcess.CatalogDeviceSession);
		}
	}
	
	/// <remarks>operator ScriptServerState() : System.String;</remarks>
	public class SystemScriptServerStateNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return AProcess.ServerSession.Server.ScriptServerState(AProcess);
		}
	}

    // operator ScriptDrop(const AName : Name) : String
    // operator ScriptDrop(const ASpecifier : String) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean) : String
    // operator ScriptDrop(const ASpecifier : String, const AIncludeDependents : Boolean, const AIncludeObject : Boolean) : String
    public class SystemScriptDropNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			D4TextEmitter LEmitter = new D4TextEmitter();
			
			Schema.Object LObject;
			
			if (Operator.Operands[0].DataType.Is(AProcess.DataTypes.SystemName))
				LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)AArguments[0], true);
			else
				LObject = Compiler.ResolveCatalogObjectSpecifier(AProcess.Plan, (string)AArguments[0], true);
				
			bool LIncludeDependents = AArguments.Length > 1 ? (bool)AArguments[1] : true;
			bool LIncludeObject = AArguments.Length > 2 ? (bool)AArguments[2] : true;
		
			return LEmitter.Emit(AProcess.Plan.Catalog.EmitDropStatement(AProcess.CatalogDeviceSession, new string[] { LObject.Name }, String.Empty, true, true, LIncludeDependents, LIncludeObject));
		}
    }
    
    /// <remarks>operator ScriptDropLibrary(AName : System.Name) : System.String;</remarks>
    public class SystemScriptDropLibraryNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return AProcess.ServerSession.Server.ScriptDropLibrary(AProcess.CatalogDeviceSession, (string)AArguments[0]);
		}
    }
    
    /// <remarks>operator ScriptDropCatalog() : System.String;</remarks>
    public class SystemScriptDropCatalogNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return AProcess.ServerSession.Server.ScriptDropCatalog(AProcess.CatalogDeviceSession);
		}
    }

	/// <remarks>operator ScriptLibraryChanges(const AOldCatalogDirectory : String, const ALibraryName : Name) : String;</remarks>
	public class SystemScriptLibraryChangesNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			return null;
			//return AProcess.ServerSession.Server.ScriptLibraryChanges((string)AArguments[0], (string)AArguments[1]);
		}
	}

	// operator Diagnostics.IsCatalogObjectLoaded(const AObjectID : Integer) : Boolean;
	public class SystemIsCatalogObjectLoadedNode : InstructionNode
	{
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			else
			#endif
			{
				if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
					throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
					
				if (Nodes[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemInteger))
					return AProcess.CatalogDeviceSession.ResolveCachedCatalogObject((int)AArguments[0], false) != null;

				return AProcess.CatalogDeviceSession.ResolveCachedCatalogObject((string)AArguments[0], false) != null;
			}
		}
	}

	// operator ClearCatalogObject(const AObjectID : Integer);
	// operator ClearCatalogObject(const AObjectName : Name);
    public class SystemClearCatalogObjectNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);

			Schema.CatalogObject LCatalogObject;
			if (Nodes[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemInteger))
				LCatalogObject = AProcess.CatalogDeviceSession.ResolveCachedCatalogObject((int)AArguments[0]);
			else
				LCatalogObject = AProcess.CatalogDeviceSession.ResolveCachedCatalogObject((string)AArguments[0]);
				
			AProcess.CatalogDeviceSession.ClearCachedCatalogObject(LCatalogObject);
				
			return null;
		}
    }
    
	// operator ClearLibrary(string ALibraryName);
    public class SystemClearLibraryNode : InstructionNode
    {
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
			string LLibraryName = AProcess.Plan.Catalog.Libraries[(string)AArguments[0]].Name;
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
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

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
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
						LID = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)Nodes[0].Execute(AProcess), true).ID;
					else
						LID = (int)Nodes[0].Execute(AProcess);
						
					List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(LID, Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProcess) : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0] = LHeaders[LIndex].ID;
						LRow[1] = LHeaders[LIndex].Name;
						LRow[2] = LHeaders[LIndex].Description;
						LRow[3] = LHeaders[LIndex].Sequence;
						LRow[4] = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
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

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredObject(ServerProcess AProcess, Table ATable, Row ARow, Schema.Object AObject, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = AObject.ID;
			ARow[1] = AObject.Name;
			ARow[2] = AObject.Description;
			ARow[3] = ASequence;
			ARow[4] = ALevel;
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
					PopulateRequiredObject(AProcess, ATable, ARow, AObject.Dependencies.ResolveObject(AProcess.CatalogDeviceSession, LIndex), ARecursive, ref ASequence, ALevel);
				}
		}

		public override object InternalExecute(ServerProcess AProcess)
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
						LID = Compiler.ResolveCatalogIdentifier(AProcess.Plan, (string)Nodes[0].Execute(AProcess), true).ID;
					else
						LID = (int)Nodes[0].Execute(AProcess);
						
					List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependencies(LID, Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProcess) : true);
					
					for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					{
						LRow[0] = LHeaders[LIndex].ID;
						LRow[1] = LHeaders[LIndex].Name;
						LRow[2] = LHeaders[LIndex].Description;
						LRow[3] = LHeaders[LIndex].Sequence;
						LRow[4] = LHeaders[LIndex].Level;
						LResult.Insert(LRow);
					}
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
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

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateLibrary(ServerProcess AProcess, Table ATable, Row ARow, Schema.LoadedLibrary ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = ALibrary.Name;
			ARow[1] = ASequence;
			ARow[2] = ALevel;
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
		
		public override object InternalExecute(ServerProcess AProcess)
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
						AProcess.CatalogDeviceSession.ResolveLoadedLibrary((string)Nodes[0].Execute(AProcess)), 
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProcess) : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
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

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		private void PopulateRequiredLibrary(ServerProcess AProcess, Table ATable, Row ARow, Schema.Library ALibrary, bool ARecursive, ref int ASequence, int ALevel)
		{
			ARow[0] = ALibrary.Name;
			ARow[1] = ASequence;
			ARow[2] = ALevel;
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
		
		public override object InternalExecute(ServerProcess AProcess)
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
						AProcess.Plan.Catalog.Libraries[(string)Nodes[0].Execute(AProcess)],
						Nodes.Count == 2 ? (bool)Nodes[1].Execute(AProcess) : true, 
						ref LSequence, 
						1
					);
				}
				finally
				{
					LRow.Dispose();
				}
				
				LResult.First();
				
				return LResult;
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
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
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

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = TableVar.FindClusteringOrder(APlan);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
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
				
				return LResult;
			}
			catch
			{
				LResult.Dispose();
				throw;
			}
		}
    }
}
