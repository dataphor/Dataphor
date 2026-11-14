/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Language
{
	/// <summary>Indicates an invalid configuration of the elements in an abstract syntax tree.</summary>
	/// <remarks>
	/// The LanguageException is an internal exception used to assert the validity of a given abstract syntax tree.
	/// LanguageExceptions are thrown when an attempt is made to construct an invalid abstract syntax tree.
	/// Only the parser should throw this exceptions of this type.
	/// </remarks>
	public class LanguageException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 107100: "Statement expected."</summary>
			StatementExpected = 107100,

			/// <summary>Error code 107101: "Expression expected."</summary>
			ExpressionExpected = 107101,

			/// <summary>Error code 107102: "Identifier expected."</summary>
			IdentifierExpected = 107102,

			/// <summary>Error code 107103: "Instruction expected."</summary>
			InstructionExpected = 107103,

			/// <summary>Error code 107104: "Statement container."</summary>
			StatementContainer = 107104,

			/// <summary>Error code 107105: "Expression container."</summary>
			ExpressionContainer = 107105,

			/// <summary>Error code 107106: "Case item expected."</summary>
			CaseItemExpected = 107106,

			/// <summary>Error code 107107: "Else expression expected."</summary>
			CaseElseExpected = 107107,

			/// <summary>Error code 107108: "Case item expression container."</summary>
			CaseItemExpressionContainer = 107108,

			/// <summary>Error code 107109: "ErrorHandler container."</summary>
			ErrorHandlerContainer = 107109,

			/// <summary>Error code 107110: "Target expected."</summary>
			TargetExpected = 107110,

			/// <summary>Error code 107111: "Update column expression container."</summary>
			UpdateColumnExpressionContainer = 107111,

			/// <summary>Error code 107112: "Server name expected."</summary>
			ServerNameExpected = 107112,

			/// <summary>Error code 107113: "Column expression container."</summary>
			ColumnExpressionContainer = 107113,

			/// <summary>Error code 107114: "NamedColumn expression container."</summary>
			NamedColumnExpressionContainer = 107114,

			/// <summary>Error code 107115: "Column name expected."</summary>
			ColumnNameExpected = 107115,

			/// <summary>Error code 107116: "RenameColumn expression container."</summary>
			RenameColumnExpressionContainer = 107116,

			/// <summary>Error code 107117: "Adorn column expression container."</summary>
			AdornColumnExpressionContainer = 107117,

			//			/// <summary>Error code 107118: "AggregateColumn expression container."</summary>
			//			AggregateColumnExpressionContainer = 107118,

			/// <summary>Error code 107119: "Aggregate expression "{0}" may only contain expression, column, or aggregate column expressions."</summary>
			AggregateColumnExpressionContainer = 107119,

			//			/// <summary>Error code 107120: "Column name expected in element "{0}"."</summary>
			//			ColumnNameExpected = 107120,

			/// <summary>Error code 107121: "Table name expected."</summary>
			TableNameExpected = 107121,

			/// <summary>Error code 107122: "Device name expected."</summary>
			DeviceNameExpected = 107122,

			/// <summary>Error code 107123: "View name expected."</summary>
			ViewNameExpected = 107123,

			/// <summary>Error code 107124: "PropertyDefintion container."</summary>
			PropertyDefinitionContainer = 107124,

			/// <summary>Error code 107125: "PropertyDefintion container."</summary>
			AlterPropertyDefinitionContainer = 107125,

			/// <summary>Error code 107126: "PropertyDefintion container."</summary>
			DropPropertyDefinitionContainer = 107126,

			/// <summary>Error code 107127: "RepresentationDefintion container."</summary>
			RepresentationDefinitionContainer = 107127,

			/// <summary>Error code 107128: "RepresentationDefintion container."</summary>
			AlterRepresentationDefinitionContainer = 107128,

			/// <summary>Error code 107129: "RepresentationDefintion container."</summary>
			DropRepresentationDefinitionContainer = 107129,

			/// <summary>Error code 107130: "ScalarType special value definition container."</summary>
			SpecialDefinitionContainer = 107130,

			/// <summary>Error code 107131: "ScalarType special value definition container."</summary>
			AlterSpecialDefinitionContainer = 107131,

			/// <summary>Error code 107132: "ScalarType special value definition container."</summary>
			DropSpecialDefinitionContainer = 107132,

			/// <summary>Error code 107133: "ScalarType name definition container."</summary>
			ScalarTypeNameDefinitionContainer = 107133,

			/// <summary>Error code 107134: "ScalarType name expected."</summary>
			ScalarTypeNameExpected = 107134,

			/// <summary>Error code 107135: "Class definition expected."</summary>
			ClassDefinitionExpected = 107135,

			/// <summary>Error code 107136: "Operator name expected."</summary>
			OperatorNameExpected = 107136,

			/// <summary>Error code 107137: "Type specifier expected."</summary>
			TypeSpecifierExpected = 107137,

			//			/// <summary>Error code 107138: "Class definition expected."</summary>
			//			ClassDefinitionExpected = 107138,

			/// <summary>Error code 107139: "Device map item container."</summary>
			DeviceScalarTypeMapContainer = 107139,

			/// <summary>Error code 107140: "Device map item container."</summary>
			AlterDeviceScalarTypeMapContainer = 107140,

			/// <summary>Error code 107141: "Device map item container."</summary>
			DropDeviceScalarTypeMapContainer = 107141,

			/// <summary>Error code 107142: "Device map item container."</summary>
			DeviceOperatorMapContainer = 107142,

			/// <summary>Error code 107143: "Device map item container."</summary>
			AlterDeviceOperatorMapContainer = 107143,

			/// <summary>Error code 107144: "Device map item container."</summary>
			DropDeviceOperatorMapContainer = 107144,

			/// <summary>Error code 107145: "ColumnDefinition container."</summary>
			ColumnDefinitionContainer = 107145,

			/// <summary>Error code 107146: "AlterColumnDefinition container."</summary>
			AlterColumnDefinitionContainer = 107146,

			/// <summary>Error code 107147: "DropColumnDefinition container."</summary>
			DropColumnDefinitionContainer = 107147,

			/// <summary>Error code 107148: "KeyColumnDefinition container."</summary>
			KeyColumnDefinitionContainer = 107148,

			/// <summary>Error code 107149: "ReferenceColumnDefinition container."</summary>
			ReferenceColumnDefinitionContainer = 107149,

			/// <summary>Error code 107150: "OrderColumnDefinition container."</summary>
			OrderColumnDefinitionContainer = 107150,

			/// <summary>Error code 107151: "KeyDefinition container."</summary>
			KeyDefinitionContainer = 107151,

			/// <summary>Error code 107152: "AlterKeyDefinition container."</summary>
			AlterKeyDefinitionContainer = 107152,

			/// <summary>Error code 107153: "DropKeyDefinition container."</summary>
			DropKeyDefinitionContainer = 107153,

			/// <summary>Error code 107154: "ReferenceDefinition container."</summary>
			ReferenceDefinitionContainer = 107154,

			/// <summary>Error code 107155: "AlterReferenceDefinition container."</summary>
			AlterReferenceDefinitionContainer = 107155,

			/// <summary>Error code 107156: "DropReferenceDefinition container."</summary>
			DropReferenceDefinitionContainer = 107156,

			/// <summary>Error code 107157: "OrderDefinition container."</summary>
			OrderDefinitionContainer = 107157,

			/// <summary>Error code 107158: "AlterOrderDefinition container."</summary>
			AlterOrderDefinitionContainer = 107158,

			/// <summary>Error code 107159: "DropOrderDefinition container."</summary>
			DropOrderDefinitionContainer = 107159,

			/// <summary>Error code 107160: "ConstraintDefinition container."</summary>
			ConstraintDefinitionContainer = 107160,

			/// <summary>Error code 107161: "AlterConstraintDefinition container."</summary>
			AlterConstraintDefinitionContainer = 107161,

			/// <summary>Error code 107162: "DropConstraintDefinition container."</summary>
			DropConstraintDefinitionContainer = 107162,

			/// <summary>Error code 107163: "ClassAttributeDefinition container."</summary>
			ClassAttributeDefinitionContainer = 107163,

			/// <summary>Error code 107164: "Class attribute "{0}" not found."</summary>
			ClassAttributeNotFound = 107164,

			/// <summary>Error code 107165: "NamedTypeSpecifier container."</summary>
			NamedTypeSpecifierContainer = 107165,

			/// <summary>Error code 107166: "Formal parameter container."</summary>
			FormalParameterContainer = 107166,

			/// <summary>Error code 107167: "Modified type specifier container."</summary>
			FormalParameterSpecifierContainer = 107167,

			/// <summary>Error code 107168: "Delete statement "{0}" may only contain delete, from, and where clauses."</summary>
			DeleteStatementContainer = 107168,

			/// <summary>Error code 107169: "Delete clause expected in delete statement "{0}"."</summary>
			DeleteClauseExpected = 107169,

			/// <summary>Error code 107170: "From clause expected in delete statement "{0}"."</summary>
			FromClauseExpectedInDelete = 107170,

			//			/// <summary>Error code 107171: "Select clause "{0}" may only contain column expressions."</summary>
			//			ColumnExpressionContainer = 107171,

			/// <summary>Error code 107172: "Join clause "{0}" may only contain a from clause and a join expression."</summary>
			FromClauseOrExpressionContainer = 107172,

			/// <summary>Error code 107173: "From clause expected in join clause "{0}"."</summary>
			FromClauseExpectedInJoin = 107173,

			/// <summary>Error code 107174: "From clause "{0}" may only contain join clauses."</summary>
			ExpressionOrJoinClauseContainer = 107174,

			/// <summary>Error code 107175: "Table expression expected in from clause "{0}"."</summary>
			TableExpressionExpected = 107175,

			/// <summary>Error code 107176: "Table alias expected in from clause "{0}"."</summary>
			TableAliasExpected = 107176,

			/// <summary>Error code 107177: "Join expression expected in join clause "{0}"."</summary>
			JoinExpressionExpected = 107177,

			/// <summary>Error code 107178: "Invalid join type in join clause "{0}"."</summary>
			InvalidJoinType = 107178,

			/// <summary>Error code 107179: "Order clause "{0}" may only contain order field expressions."</summary>
			OrderFieldExpressionContainer = 107179,

			/// <summary>Error code 107180: "Select expression "{0}" may only contain select, from, where, group or having clauses."</summary>
			SelectExpressionContainer = 107180,

			/// <summary>Error code 107181: "Select clause expected in select expression "{0}"."</summary>
			SelectClauseExpected = 107181,

			/// <summary>Error code 107182: "From clause expected in select expression "{0}"."</summary>
			FromClauseExpectedInSelect= 107182,

			//			/// <summary>Error code 107183: "Union expression "{0}" may only contain select expressions."</summary>
			//			SelectExpressionContainer = 107183,

			/// <summary>Error code 107184: "Select expression expected in union expression "{0}"."</summary>
			SelectExpressionExpectedInUnion = 107184,

			/// <summary>Error code 107185: "Query expression "{0}" may only contain select or union expressions."</summary>
			SelectOrUnionExpressionContainer = 107185,

			/// <summary>Error code 107186: "Select expression expected in query expression "{0}"."</summary>
			SelectExpressionExpectedInQuery = 107186,

			/// <summary>Error code 107187: "Select statement "{0}" may only contain a query expression or order by clause."</summary>
			QueryExpressionOrOrderClauseContainer = 107187,

			/// <summary>Error code 107188: "Query expression expected in select statement "{0}"."</summary>
			QueryExpressionExpected = 107188,

			/// <summary>Error code 107189: "Insert clause "{0}" may only contain a table expression and insert field expressions."</summary>
			InsertFieldOrTableExpressionContainer = 107189,

			/// <summary>Error code 107190: "Insert statement "{0}" may only contain an insert clause and a values expression."</summary>
			InsertClauseOrValuesExpressionContainer = 107190,

			/// <summary>Error code 107191: "Insert clause expected in insert statement "{0}"."</summary>
			InsertClauseExpected = 107191,

			/// <summary>Error code 107192: "Expression expected in insert statement "{0}"."</summary>
			ValuesExpressionExpected = 107192,

			/// <summary>Error code 107193: "Update clause "{0}" may only contain a table expression and update field expressions."</summary>
			UpdateFieldOrTableExpressionContainer = 107193,

			/// <summary>Error code 107194: "Update statement "{0}" may only contain update, from, and where clauses."</summary>
			UpdateStatementContainer = 107194,

			/// <summary>Error code 107195: "Update clause expected in update statement "{0}"."</summary>
			UpdateClauseExpected = 107195,

			/// <summary>Error code 107196: "From clause expected in update statement "{0}"."</summary>
			FromClauseExpectedInUpdate = 107196,

			/// <summary>Error code 107197: "Delete clause "{0}" may only contain a table expression."</summary>
			TableExpressionContainer = 107197,

			/// <summary>Error code 107198: "Dataphor exception: {0}"</summary>
			DataphorException = 107198,

			/// <summary>Error code 107199: "Unknown type specifier: "{0}"."</summary>
			UnknownTypeSpecifier = 107199,

			/// <summary>Error code 107200: "Tag reference may not be null."</summary>
			TagReferenceRequired = 107200,

			/// <summary>Error code 107201: "Invalid operand "{0}"."</summary>
			InvalidOperand = 107201,
			
			/// <summary>Error code 107202: "Unknown statement class "{0}"."</summary>
			UnknownStatementClass = 107202,
			
			/// <summary>Error code 107203: "Unknown expression class "{0}"."</summary>
			UnknownExpressionClass = 107203,
			
			/// <summary>Error code 107204: "Unknown instruction "{0}"."</summary>
			UnknownInstruction = 107204,
			
			/// <summary>Error code 107205: "Named expression "{0}" not found."</summary>
			NamedExpressionNotFound = 107205,
			
			/// <summary>Error code 107206: "Unknown join type "{0}"."</summary>
			UnknownJoinType = 107206,
			
			/// <summary>Error code 107207: "Table alias required."</summary>
			TableAliasRequired = 107207,
			
			/// <summary>Error code 107208: "An entry for the given expression was not found."</summary>
			NamedExpressionNotFoundByExpression = 107208,
			
			/// <summary>Error code 107209: "{0} container."</summary>
			InvalidContainer = 107209,
			
			/// <summary>Error code 107210: "Duplicate language modifier "{0}"."</summary>
			DuplicateLanguageModifierName = 107210,
			
			/// <summary>Error code 107211: "Language modifier "{0}" not found."</summary>
			LanguageModifierNotFound = 107211,
			
			/// <summary>Error code 107212: "Language modifier "{0}" is ambiguous."</summary>
			AmbiguousModifierReference = 107212,
			
			/// <summary>Error code 107213: "Class definition already contains a definition for the attribute "{0}"."</summary>
			DuplicateAttributeDefinition = 107213
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Language.LanguageException", typeof(LanguageException).Assembly);

		// Constructors
		public LanguageException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public LanguageException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public LanguageException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public LanguageException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public LanguageException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public LanguageException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public LanguageException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public LanguageException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public LanguageException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}