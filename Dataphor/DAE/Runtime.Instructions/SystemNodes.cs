/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/*
        Operator precedence ->
			()
			.
			[]
			~ 
            +, - (arithmetic unary)
            **
            *, /, %
            + -
            =, <>, >, <, >=, <=, ?=
            ^, &, |, <<, >>
            not, exists
            and
            in, or, xor, like, between, matches
            table operators
            
            Monadic table operators ->
                PROJECT
                    Given a table, return a table with the header defined by columns, having the
                    set of rows in table projected over columns.
                    
                REMOVE
					Given a table, return a table with the header defined by the Columns in the source
					table minus the columns given columns.  This is equivalent in function to a project,
					it is simply syntatic shorthand for the special case when a column is not desired,
					as opposed to when it is.
					
				EXTEND
					Given a table and a set of named expressions, return a table with the header defined
					by the table extended to include the set of columns defined by the named expressions,
					with each row in the resulting table having the extended columns populated with the
					result of evaluating each expression in the context of that row.  Note that this operation
					cannot change the key of the table.
					
				RENAME
					Given a table and a set of column renaming expressions, return a table with the header
					defined by all the columns in the table not affected by the rename, and all columns
					specified in the rename with the new column names.  Note that this operation cannot change
					the key of the table.
                    
                RESTRICT
                    Given a table, return a table with the same header, restricted to the rows that
                    satisfy the given condition. A row for which the condition evaluates to null is 
                    considered to be false and excluded from the result.

                AGGREGATE
                    Given a table, return a table with the header defined by ByColumns and the return types
                    of AggregateColumns, having a record for each distinct group denoted in ByColumns
                    with aggregate operations performed as denoted by AggregateColumns.
                    
                ORDER
                    Given a table, return a table with the same header, ordered by the given columns.
                    
                BROWSE
                    Given a table, return a searchable table with the same header, ordered by the given columns.
                    
                QUOTA
                    Given a table, return the given number of rows, by the given quota columns.
                
                EXPLODE
                    Given a table, and a relationship as defined by the explode condition, starting from the
                    root condition, return a table in which the hierarchy defined by the relationship is
                    flattened, beginning at the root node.  The keyword parent may only appear in the
                    context of an explode condition to designate the parent side of the condition.
                    Also includes an optional level specifier to include in the output of the operation
                    the current level of evaluation within the hierarchy, with the root node being level 1.
                    The explode operation is equivalent to evaluating the following, procedural pseudocode:
                    
                        EXPLODE (<expression>) BY (<explode condition>) WHERE (<root condition>) ::=
                        
                            RECURSE (<expression>)
                                OUTPUT (<expression>) WHERE (<explode condition>)
                                FOREACH (<row> in (<expression>) WHERE (<explode condition>))
                                    RECURSE (<expression>) WHERE (<explode condition>)
                                    
                            RECURSE (<expression>) WHERE (<root condition>)
                            
                    As a concrete example, consider the following:
                        
                        CREATE TABLE RightType AS (ID int, Parent_ID int)
                        CREATE REFERENCE RightType_Tree AS (RightType(Parent_ID) REFERENCES RightType(ID))
                        
                        INSERT 
                        (
                            TABLE
                            (
                                ROW(1000, NULL),
                                ROW(1100, 1000),
                                ROW(1110, 1100),
                                ROW(1111, 1110),
                                ROW(1112, 1110),
                                ROW(1113, 1110),
                                ROW(1120, 1100),
                                ROW(1130, 1100),
                                ROW(1200, 1000),
                                ROW(1300, 1000)
                            )
                        )
                        INTO RightType
                        
                    Then the expression:
                        
                        EXPLODE (RightType) BY (Parent_ID = PARENT ID) WHERE (ID = 1100)
                        
                    Would be equivalent to:
                    
                        RECURSE (INPUT)
                            OUTPUT (RightType) WHERE (ID = INPUT.ID)
                            RECURSE (RightType) WHERE (Parent_ID = INPUT.ID)
                            
                        RECURSE (RightType) WHERE (ID = 1100)   
                        
                    And would return:
                    
                        ID: int,    Parent_ID: int, Level: int, Sequence: int
                        
                        1100        1000            1           1
                        1110        1100            2           2
                        1111        1110            3           3
                        1112        1110            3           3
                        1113        1110            3           3
                        1120        1100            2           4
                        1130        1100            2           4

            Dyadic table operators ->
                UNION
                    Given two tables, with headers of the same type return a table with that same header, 
                    which consists of all rows from the first table and all rows from the second table.
                    The optional all or distinct keyword specified after the union operator determines 
                    whether the operation is duplicate preserving.  The default is distinct.
                    
                INTERSECT
                    Given two tables, with headers of the same type, return a table with the same header
                    which consists of all rows that exist in both tables.  The operation is extended to
                    allow for the treatment of duplicate rows.
        
                DIFFERENCE
                    Given two tables, with headers of the same type, return a table with the same header
                    which consists of all rows that exist in the first table, minus all the rows that exist in
                    the second table.  The operation is extended to allow for the treatment of duplicate rows.
                    
                PRODUCT
                    Given any two tables, return a table with a header that consists of all the columns in the 
                    first table, plus all the columns in the second table, containing the product of the two
                    tables, that is for each row in the first table, a row for each row in the second table
                    is returned in the resulting table.
                    
                JOIN
                    Given any two tables, return a table with a header that consists of all the columns in the
                    first table, plus all the columns in the second table, containing the product of the two
                    tables, restricted to rows for which the given expression evaluates to true.  This operation
                    can be expressed equivalently as:
                        
                        Table Restrict(Product(Table, Table), Expression);
                        
                LEFT JOIN
                    Given any two tables, return a table with a header that consists of all the columns in the
                    first table, plus all the columns in the second table, containing the product of the two
                    tables, restricted to rows for which the given expression evaluates to true plus all rows from
                    the first table for which the given expression evaluates to false with columns from the second
                    table in these rows set to null.
                    
                RIGHT JOIN
                    Given any two tables, return a table with a header that consists of all the columns in the
                    first table plus all the columns in the second table, containing the product of the two
                    tables, restricted to rows for which the given expression evaluates to true plus all rows from 
                    the second table for which the given expression evaluates to false with columns from the first
                    table in these rows set to null.
                    
                    Note: The full outer join is not supported because it cannot be unambiguously updated.  If full
                    outer join functionality is desired, it can be simulated using the union of a left outer
                    and a restricted right outer.
                    
                DIVIDE
                    Given tables A and B, with attribute sets XY and Y, respectively, return a C with attribute set X
                    such that C product B is a subtable of A.
                    
                    Possibly include support for the "small" divide operator as Date has defined it:
                    Given three tables, A, B, and C, with attribute sets X, Y, and XY, respectively, return a table
                    with attribute set X containing a row for every row in A which has a pair in C for all rows in B.
                   
		Integer Data Types ->

			type Huge is {System.Alpha};
			type Long is {Huge} { constraint LongValue Value between -(2 ** 63) and (2 ** 63) - 1 };
			type ULong is {Huge} { constraint ULongValue Value between 0 and (2 ** 64) };
			type Integer is {Long} { constraint IntegerValue Value between -(2 ** 31) and (2 ** 31) - 1 };
			type UInteger is {Long, ULong} { constraint UIntegerValue Value between 0 and (2 ** 32) };
			type Short is {Integer} { constraint ShortValue Value between -(2 ** 15) and (2 ** 15) - 1 };
			type UShort is {Integer, UInteger} { constraint UShortValue Value between 0 and (2 ** 16) };
			type SByte is {Short} { constraint SByteValue Value between -(2 ** 7) and (2** 7) - 1 };
			type Byte is {UShort} { constraint ByteValue Value between 0 and (2 ** 8) };
			type Puny is {SByte, Byte} { constraint PunyValue Value between 0 and (2 ** 7) - 1 };
			
				   Huge
				   /   \
				  /	    \
				Long	ULong
				/  \	 /
		       /    \	/
			Integer	UInteger
			 /	 \	  /
			/	  \	 /
		   Short  UShort
		   /  \	   /
		  /	   \  /
		SByte  Byte
		  \    /
		   \  /
		   Puny
		
	*/
	
	/// <remarks>
	///	operator System.Error(const AMessage : String) : System.Error;
	/// operator System.Error(const AMessage : String, const AInnerError : System.Error) : System.Error;
	/// operator System.Error(const ACode : Integer, const AMessage : String) : Error;
	/// operator System.Error(const ACode : Integer, const AMessage : String, const AInnerError : System.Error) : Error;
	/// operator System.Error(const ASeverity : String, const AMessage : String) : Error;
	/// operator System.Error(const ASeverity : String, const AMessage : String, const AInnerError : Error) : Error;
	/// operator System.Error(const ASeverity : String, const ACode : Integer, const AMessage : String) : Error;
	/// operator System.Error(const ASeverity : String, const ACode : Integer, const AMessage : String, const AInnerError : Error) : Error;
	/// </remarks>
	public class SystemErrorSelectorNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			switch (AArguments.Length)
			{
				case 1 :
					// (String)
					#if NILPROPOGATION
					if (AArguments[0] == null)
						return null;
					#endif
					
					return new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, (string)AArguments[0]);
				
				case 2 :
					// (String, Error)
					// (Integer, String)
					// (String, String)
					#if NILPROPOGATION
					if (AArguments[0] == null || AArguments[1] == null)
						return null;
					#endif

					if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemString))
						if (Operator.Operands[1].DataType.Is(AProgram.DataTypes.SystemString))
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[0]), DataphorException.CApplicationError, (string)AArguments[1]);
						else
							return new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, (string)AArguments[0], (Exception)AArguments[1]);

					return new DataphorException(ErrorSeverity.User, (int)AArguments[0], (string)AArguments[1]);
				
				case 3 :
					// (Integer, String, Error) (Code, Message, InnerError)
					// (String, String, Error) (Severity, Message, InnerError)
					// (String, Integer, String) (Severity, Code, Message)
					#if NILPROPOGATION
					if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null)
						return null;
					#endif
					
					if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
						return new DataphorException(ErrorSeverity.User, (int)AArguments[0], (string)AArguments[1], (Exception)AArguments[2]);
					else
					{
						if (Operator.Operands[1].DataType.Is(AProgram.DataTypes.SystemInteger))
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[0]), (int)AArguments[1], (string)AArguments[2]);
						else
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[0]), DataphorException.CApplicationError, (string)AArguments[1], (Exception)AArguments[2]);
					}
				
				default :
					// (String Integer, String, Error)
					#if NILPROPOGATION
					if (AArguments[0] == null || AArguments[1] == null || AArguments[2] == null || AArguments[3] == null)
						return null;
					#endif
					
					return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[0]), (int)AArguments[1], (string)AArguments[2], (Exception)AArguments[3]);
			}
		}
	}
	
	/// <remarks>operator System.Error.ReadSeverity(const AValue : Error) : String;</remarks>
	public class SystemErrorReadSeverityNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			DataphorException LException = (Exception)AArguments[0] as DataphorException;
			return (LException == null ? ErrorSeverity.Application : LException.Severity).ToString();
		}
	}
	
	/// <remarks>operator System.Error.WriteSeverity(const AValue : Error, const ASeverity : String) : Error;</remarks>
	public class SystemErrorWriteSeverityNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			Exception LException = (Exception)AArguments[0];
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[1]), LDataphorException.Code, LDataphorException.Message, LDataphorException.InnerException);
			else
				return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)AArguments[1]), DataphorException.CApplicationError, LException.Message, LException.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadCode(const AValue : Error) : Integer;</remarks>
	public class SystemErrorReadCodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			DataphorException LException = (Exception)AArguments[0] as DataphorException;
			return LException == null ? DataphorException.CApplicationError : LException.Code;
		}
	}
	
	/// <remarks>operator System.Error.WriteCode(const AValue : Error, const ACode : Integer) : Error;</remarks>
	public class SystemErrorWriteCodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			Exception LException = (Exception)AArguments[0];
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataphorException(LDataphorException.Severity, (int)AArguments[1], LDataphorException.Message, LDataphorException.InnerException);
			else
				return new DataphorException(ErrorSeverity.Application, (int)AArguments[1], LException.Message, LException.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadMessage(const AValue : Error) : String;</remarks>
	public class SystemErrorReadMessageNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			return ((Exception)AArguments[0]).Message;
		}
	}
	
	/// <remarks>operator System.Error.WriteMessage(const AValue : Error, const AMessage : String) : Error;</remarks>
	public class SystemErrorWriteMessageNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			Exception LException = (Exception)AArguments[0];
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataphorException(LDataphorException.Severity, LDataphorException.Code, (string)AArguments[1], LDataphorException.InnerException);
			else
				return new DataphorException(ErrorSeverity.Application, DataphorException.CApplicationError, (string)AArguments[1], LException.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadInnerError(const AValue : Error) : Error;</remarks>
	public class SystemErrorReadInnerErrorNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			return ((Exception)AArguments[0]).InnerException;
		}
	}
	
	/// <remarks>operator System.Error.WriteInnerError(const AValue : Error, const AInnerError : Error) : Error;</remarks>
	public class SystemErrorWriteInnerErrorNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null || AArguments[1] == null)
				return null;
			#endif

			Exception LException = (Exception)AArguments[0];
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataphorException(LDataphorException.Severity, LDataphorException.Code, LDataphorException.Message, (Exception)AArguments[1]);
			else
				return new DataphorException(ErrorSeverity.Application, DataphorException.CApplicationError, LException.Message, (Exception)AArguments[1]);
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetErrorDescriptionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			return ExceptionUtility.BriefDescription((Exception)AArguments[0]);
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetDetailedErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetDetailedErrorDescriptionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			if (AArguments[0] == null)
				return null;
			#endif

			return ExceptionUtility.DetailedDescription((Exception)AArguments[0]);
		}
	}
	
    /// <remarks> operator System.Binary.Binary(AValue : String) : System.Binary </remarks>
    public class SystemBinarySelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			// TODO: Streams and binary data types
			using (Scalar LScalar = new Scalar(AProgram.ValueManager, (Schema.ScalarType)FDataType, AProgram.StreamManager.Allocate()))
			{
				LScalar.AsBase64String = (string)AArgument1;
				LScalar.ValuesOwned = false;
				return LScalar.AsNative;
			}
		}
    }

    // SystemBinaryReadAccessorNode
    public class SystemBinaryReadAccessorNode : UnaryInstructionNode
    {
		public SystemBinaryReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return new Scalar(AProgram.ValueManager, (Schema.IScalarType)Operator.Operands[0].DataType, (StreamID)AArgument1).AsBase64String;
		}

		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    // SystemBinaryWriteAccessorNode
    public class SystemBinaryWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			
			using (Scalar LScalar = new Scalar(AProgram.ValueManager, (Schema.ScalarType)FDataType, (StreamID)AArgument1))
			{
				LScalar.AsBase64String = (string)AArgument2;
				return LScalar.AsNative;
			}
		}
    }
    
    /// <remarks> operator System.Guid.Guid(AValue : String) : System.Guid </remarks>
    public class SystemGuidSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return new Guid((string)AArgument1);
		}
    }

    // SystemGuidReadAccessorNode
    public class SystemGuidReadAccessorNode : UnaryInstructionNode
    {
		public SystemGuidReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}

		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return ((Guid)AArgument1).ToString();
		}
    }
    
    // SystemGuidWriteAccessorNode
    public class SystemGuidWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif

			return new Guid((string)AArgument2);
		}
    }
    
    // ScalarSelectorNode
    public class ScalarSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return DataValue.CopyValue(AProgram.ValueManager, AArgument1);
		}
    }
    
    // ScalarReadAccessorNode
    public class ScalarReadAccessorNode : UnaryInstructionNode
    {
		public ScalarReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return DataValue.CopyValue(AProgram.ValueManager, AArgument1);
		}
    }
    
    // ScalarWriteAccessorNode
    public class ScalarWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument2 == null)
				return null;
			#endif
			
			return DataValue.CopyValue(AProgram.ValueManager, AArgument2);
		}
    }
    
    // CompoundScalarSelectorNode
    public class CompoundScalarSelectorNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if NILPROPOGATION
			for (int LIndex = 0; LIndex < AArguments.Length; LIndex++)
				if (AArguments[LIndex] == null)
					return null;
			#endif
			
			Schema.IRowType LRowType = ((Schema.ScalarType)FDataType).CompoundRowType;
			Row LRow = new Row(AProgram.ValueManager, LRowType);
			for (int LIndex = 0; LIndex < LRowType.Columns.Count; LIndex++)
				LRow[LIndex] = AArguments[LIndex];
			return LRow.AsNative;
		}
    }
    
    public class CompoundScalarReadAccessorNode : UnaryInstructionNode
    {
		private string FPropertyName;
		public string PropertyName
		{
			get { return FPropertyName; }
			set { FPropertyName = value; }
		}
		
		private int FPropertyIndex;
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			Schema.IRowType LCompoundRowType = ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType;
			FPropertyIndex = LCompoundRowType.Columns.IndexOfName(PropertyName);
			if (!LCompoundRowType.Columns[FPropertyIndex].DataType.Equals(FDataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, APlan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, FDataType.Name);
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif
			
			return DataValue.CopyValue(AProgram.ValueManager, ((NativeRow)AArgument1).Values[FPropertyIndex]);
		}
    }
    
    public class CompoundScalarWriteAccessorNode : BinaryInstructionNode
    {
		private string FPropertyName;
		public string PropertyName
		{
			get { return FPropertyName; }
			set { FPropertyName = value; }
		}
		
		private int FPropertyIndex;
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			Schema.IRowType LCompoundRowType = ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType;
			FPropertyIndex = LCompoundRowType.Columns.IndexOfName(PropertyName);
			if (!LCompoundRowType.Columns[FPropertyIndex].DataType.Equals(Nodes[1].DataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, APlan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, FDataType.Name);
		}
		
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return null;
			#endif
			
			NativeRow LResult = (NativeRow)DataValue.CopyValue(AProgram.ValueManager, AArgument1);
			DataValue.DisposeValue(AProgram.ValueManager, LResult.Values[FPropertyIndex]);
			LResult.Values[FPropertyIndex] = DataValue.CopyValue(AProgram.ValueManager, AArgument2);
			return LResult;
		}
    }
    
    // ScalarIsSpecialNode
    public class ScalarIsSpecialNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			#if NILPROPOGATION
			if (AArgument1 == null)
				return null;
			#endif

			return false;
		}
    }
    
    // operator IsNil(AValue : generic) : Boolean;
    public class IsNilNode : UnaryInstructionNode
    {
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return AArgument1 == null;
		}
    }

	// operator IsNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNilRowNode : BinaryInstructionNode
	{
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return true;
			#endif

			return !(((Row)AArgument1).HasValue((string)AArgument2));
		}
	}

    // operator IsNotNil(AValue : generic) : Boolean;
    public class IsNotNilNode : UnaryInstructionNode
    {
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override object InternalExecute(Program AProgram, object AArgument1)
		{
			return !(AArgument1 == null);
		}
    }

	// operator IsNotNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNotNilRowNode : BinaryInstructionNode
	{
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if (AArgument1 == null || AArgument2 == null)
				return false;
			#endif

			return (((Row)AArgument1).HasValue((string)AArgument2));
		}
	}

	// operator IfNil(AValue : generic, AValue : generic) : generic;
    public class IfNilNode : InstructionNodeBase
    {
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = Nodes[1].IsNilable;
		}

		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);

			if (Nodes[1].DataType.Is(Nodes[0].DataType))
			{
				FDataType = Nodes[0].DataType;
				Nodes[1] = Compiler.Upcast(APlan, Nodes[1], FDataType);
			}
			else if (Nodes[0].DataType.Is(Nodes[1].DataType))
			{
				FDataType = Nodes[1].DataType;
				Nodes[0] = Compiler.Upcast(APlan, Nodes[0], FDataType);
			}
			else
			{
				ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[1].DataType, Nodes[0].DataType);
				if (LContext.CanConvert)
				{
					FDataType = Nodes[0].DataType;
					Nodes[1] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[1], LContext), Nodes[0].DataType);
				}
				else
				{
					LContext = Compiler.FindConversionPath(APlan, Nodes[0].DataType, Nodes[1].DataType);
					if (LContext.CanConvert)
					{
						FDataType = Nodes[1].DataType;
						Nodes[0] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[0], LContext), Nodes[1].DataType);
					}
					else
						Compiler.CheckConversionContext(APlan, LContext);
				}
			}

			FDataType = Nodes[0].DataType;
		}
		
		public override object InternalExecute(Program AProgram)
		{
			object LResult = Nodes[0].Execute(AProgram);
			if (LResult == null)
				LResult = Nodes[1].Execute(AProgram);
			return DataValue.CopyValue(AProgram.ValueManager, LResult);
		}
    }
    
    /// <remarks>operator System.Diagnostics.IsSupported(AStatement : String, ADeviceName : Name) : Boolean;</remarks>
    public class SystemIsSupportedNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LDeviceName = (string)AArguments[0];
			string LStatementString = (string)AArguments[1];
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, LDeviceName, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
				
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				ParserMessages LParserMessages = new ParserMessages();
				Statement LStatement = new Parser().ParseStatement(LStatementString, LParserMessages);
				LPlan.Messages.AddRange(LParserMessages);

				PlanNode LNode = Compiler.Compile(LPlan, LStatement);
				if (LPlan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));
				if (LNode is FrameNode)
					LNode = LNode.Nodes[0];
				if ((LNode is ExpressionStatementNode) || (LNode is CursorNode))
					LNode = LNode.Nodes[0];
				return LNode.DeviceSupported && (LNode.Device == LDevice);
			}
			finally
			{
				LPlan.Dispose();
			}
		}
    }

	//	operator Reconcile() : table { Sequence : Integer, Error : Error };
	//	operator Reconcile(ADeviceName : System.Name) : table { Sequence : Integer, Error : Error };
	//	operator Reconcile(ADeviceName : System.Name, ATableName : System.Name) : table { Sequence : Integer, Error : Error };
	public class SystemReconcileNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("Sequence", APlan.DataTypes.SystemInteger));			
			DataType.Columns.Add(new Schema.Column("Error", APlan.DataTypes.SystemError));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					string LDeviceName;
					
					if (Nodes.Count > 0)
						LDeviceName = (string)Nodes[0].Execute(AProgram);
					else
						LDeviceName = AProgram.Plan.DefaultDeviceName;

					Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, LDeviceName, true) as Schema.Device;
					if (LDevice == null)
						throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
						
					AProgram.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.Reconcile));
					ErrorList LErrorList;
					if (Nodes.Count == 2)
					{
						string LTableName = (string)Nodes[1].Execute(AProgram);
						Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProgram.Plan, LTableName, false) as Schema.TableVar;
						if (LTableVar == null)
							LTableVar = new Schema.BaseTableVar(LTableName, new Schema.TableType(), LDevice);
							
						LErrorList = LDevice.Reconcile(AProgram.ServerProcess, LTableVar);
					}
					else
						LErrorList = LDevice.Reconcile(AProgram.ServerProcess);
						
					for (int LIndex = 0; LIndex < LErrorList.Count; LIndex++)
					{
						LRow[0] = LIndex;
						LRow[1] = LErrorList[LIndex];
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

	public class SystemEnsureDeviceStartedNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LDeviceName = (string)AArguments[0];
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProgram.Plan, LDeviceName, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			AProgram.ServerProcess.EnsureDeviceStarted(LDevice);
			return null;
		}
	}
	
	public class SystemShowPlanNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LStatementString = (string)AArguments[0];
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				ParserMessages LParserMessages = new ParserMessages();
				Statement LStatement = new Parser().ParseScript(LStatementString, LParserMessages);
				Block LBlock = LStatement as Block;
				if (LBlock.Statements.Count == 1)
					LStatement = LBlock.Statements[0];
				LPlan.Messages.AddRange(LParserMessages);
				PlanNode LNode = Compiler.Compile(LPlan, LStatement);
				if (LPlan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));

				System.IO.StringWriter LStringWriter = new System.IO.StringWriter();
				System.Xml.XmlTextWriter LWriter = new System.Xml.XmlTextWriter(LStringWriter);
				LWriter.Formatting = System.Xml.Formatting.Indented;
				LWriter.Indentation = 4;
				LNode.WritePlan(LWriter);
				LWriter.Flush();

				return LStringWriter.ToString();
			}
			finally
			{
				LPlan.Dispose();
			}
		}
	}

	/// <remarks>operator System.Diagnostics.GetRestrictionAlgorithm(AExpression : String) : String;</remarks>
	public class SystemGetRestrictionAlgorithmNode : InstructionNode    
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				PlanNode LNode = Compiler.CompileExpression(LPlan, new Parser().ParseExpression((string)AArguments[0]));
				LPlan.CheckCompiled();

				LNode = Compiler.BindNode(LPlan, LNode);

				if (!(LNode is RestrictNode))
					throw new Exception("Restrict expression expected");
			
				return ((RestrictNode)LNode).RestrictionAlgorithm.Name;
			}
			finally
			{
				LPlan.Dispose();
			}
		}
	}
    
	/// <remarks>operator System.Diagnostics.GetJoinAlgorithm(AExpression : String) : String;</remarks>
	public class SystemGetJoinAlgorithmNode : InstructionNode    
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				PlanNode LNode = Compiler.CompileExpression(LPlan, new Parser().ParseExpression((string)AArguments[0]));
				if (LPlan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));
					
				LNode = Compiler.BindNode(LPlan, LNode);
				if (!(LNode is JoinNode))
					throw new Exception("Join expression expected");
			
				return ((JoinNode)LNode).JoinAlgorithm.Name;
			}
			finally
			{
				LPlan.Dispose();
			}
		}
	}
    
	/// <remarks>operator StartProcess() : Integer;</remarks>
	public class SystemStartProcessNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return ((IServerSession)AProgram.ServerProcess.ServerSession).StartProcess(new ProcessInfo(AProgram.ServerProcess.ServerSession.SessionInfo)).ProcessID;
		}
	}
	
	// create operator Execute(const AScript : String) 
	// create operator Execute(const AScript : String, const AInParams : row) 
	// create operator Execute(const AScript : String, const AInParams : row, var AOutParams : row) 
	// create operator Execute(const AProcessID : Integer, const AScript : String)
	// create operator Execute(const AProcessID : Integer, const AScript : String, const AInParams : row)
	// create operator Execute(const AProcessID : Integer, const AScript : String, const AInParams : row, var AOutParams : row)
	public class SystemExecuteNode : InstructionNode
	{
		public static void ExecuteScript(ServerProcess AProcess, Program AProgram, PlanNode ANode, string AScript, DebugLocator ALocator)
		{
			ExecuteScript(AProcess, AProgram, ANode, AScript, null, null, ALocator);
		}

		public static void ExecuteScript(ServerProcess AProcess, Program AProgram, PlanNode ANode, string AScript, Row AInParams, Row AOutParams, DebugLocator ALocator)
		{
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AProgram, AInParams, AOutParams);
			AProcess.PushProcessLocals();
			try
			{
				IServerProcess LProcess = (IServerProcess)AProcess;
				IServerScript LScript = LProcess.PrepareScript(AScript, ALocator);
				try
				{
					LScript.Execute(LParams);
				}
				finally
				{
					LProcess.UnprepareScript(LScript);
				}
				SystemEvaluateNode.UpdateRowFromParams(AOutParams, LParams);
			}
			finally
			{
				AProcess.PopProcessLocals();
			}
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
				ExecuteScript
				(
					AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArguments[0]),
					AProgram,
					this,
					(string)AArguments[1],
					AArguments.Length >= 3 ? (Row)AArguments[2] : null,
					AArguments.Length >= 4 ? (Row)AArguments[3] : null,
					null
				);
			else
				ExecuteScript
				(
					AProgram.ServerProcess,
					AProgram,
					this,
					(string)AArguments[0],
					AArguments.Length >= 2 ? (Row)AArguments[1] : null,
					AArguments.Length >= 3 ? (Row)AArguments[2] : null,
					null
				);
			return null;
		}
	}
	
	/// <remarks>operator Evaluate(const AExpression : System.String) : generic;</remarks>
	/// <remarks>operator Evaluate(const AExpression : System.String, const AInParams : row) : generic;</remarks>
	/// <remarks>operator Evaluate(const AExpression : System.String, const AInParams : row, var AOutParams : row) : generic;</remarks>
	/// <remarks>operator Evaluate(const AProcessID : Integer, const AExpression : String) : generic;</remarks>
	/// <remarks>operator Evaluate(const AProcessID : Integer, const AExpression : String, const AInParams : row) : generic;</remarks>
	/// <remarks>operator Evaluate(const AProcessID : Integer, const AExpression : String, const AInParams : row, var AOutParams : row) : generic;</remarks>
	public class SystemEvaluateNode : InstructionNode
	{
		public static DataParams ParamsFromRows(Program AProgram, Row AInParams, Row AOutParams)
		{
			DataParams LParams = new DataParams();
			int LOutIndex;
			for (int LIndex = 0; LIndex < ((AInParams == null) ? 0 : AInParams.DataType.Columns.Count); LIndex++)
			{
				LOutIndex = AOutParams == null ? -1 : AOutParams.DataType.Columns.IndexOfName(AInParams.DataType.Columns[LIndex].Name);
				if (LOutIndex >= 0)
					LParams.Add(new DataParam(AInParams.DataType.Columns[LIndex].Name, AInParams.DataType.Columns[LIndex].DataType, Modifier.Var, DataValue.CopyValue(AProgram.ValueManager, AInParams[LIndex])));
				else
					LParams.Add(new DataParam(AInParams.DataType.Columns[LIndex].Name, AInParams.DataType.Columns[LIndex].DataType, Modifier.In, DataValue.CopyValue(AProgram.ValueManager, AInParams[LIndex])));
			}
			
			for (int LIndex = 0; LIndex < ((AOutParams == null) ? 0 : AOutParams.DataType.Columns.Count); LIndex++)
			{
				if (!LParams.Contains(AOutParams.DataType.Columns[LIndex].Name))
					LParams.Add(new DataParam(AOutParams.DataType.Columns[LIndex].Name, AOutParams.DataType.Columns[LIndex].DataType, Modifier.Var, DataValue.CopyValue(AProgram.ValueManager, AOutParams[LIndex])));
			}
			return LParams;
		}

		public static void UpdateRowFromParams(Row AOutParams, DataParams AParams)
		{
			if (AOutParams != null)
				for (int LIndex = 0; LIndex < AOutParams.DataType.Columns.Count; LIndex++)
					AOutParams[LIndex] = AParams[AParams.IndexOf(AOutParams.DataType.Columns[LIndex].Name)].Value;
		}
		
		private object Evaluate(ServerProcess AProcess, Program AProgram, PlanNode ANode, string AExpression, Row AInParams, Row AOutParams)
		{
			DataParams LParams = ParamsFromRows(AProgram, AInParams, AOutParams);
			
			AProcess.PushProcessLocals();
			try
			{
				IServerProcess LProcess = (IServerProcess)AProcess;
				IServerExpressionPlan LPlan = LProcess.PrepareExpression(AExpression, LParams);
				try
				{
					LPlan.CheckCompiled();
					PlanNode LNode = ((ServerExpressionPlan)LPlan).Program.Code;
					if ((IsLiteral && !LNode.IsLiteral) || (IsFunctional && !LNode.IsFunctional) || (IsDeterministic && !LNode.IsDeterministic) || (IsRepeatable && !LNode.IsRepeatable) || (!IsNilable && LNode.IsNilable))
						throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(LNode));
					
					DataValue LResult = LPlan.Evaluate(LParams);
					UpdateRowFromParams(AOutParams, LParams);
					Scalar LScalar = LResult as Scalar;
					return LScalar == null ? LResult : LScalar.AsNative;
				}
				finally
				{
					LProcess.UnprepareExpression(LPlan);
				}
			}
			finally
			{
				AProcess.PopProcessLocals();
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
				return 
					Evaluate
					(
						AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArguments[0]),
						AProgram,
						this,
						(string)AArguments[1],
						AArguments.Length >= 3 ? (Row)AArguments[2] : null,
						AArguments.Length >= 4 ? (Row)AArguments[3] : null
					);
			else
				return 
					Evaluate
					(
						AProgram.ServerProcess, 
						AProgram,
						this,
						(string)AArguments[0], 
						AArguments.Length >= 2 ? (Row)AArguments[1] : null, 
						AArguments.Length >= 3 ? (Row)AArguments[2] : null
					);
		}
	}

	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row, var AOutParams : row) 
	public class SystemExecuteOnNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LServerName = (string)AArguments[0];
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0]);
			Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
			if (LServerLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string LStatement = (string)AArguments[1];
			
			Row LInRow = AArguments.Length >= 3 ? (Row)AArguments[2] : null;
			Row LOutRow = AArguments.Length >= 4 ? (Row)AArguments[3] : null;
			
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AProgram, LInRow, LOutRow);
			AProgram.RemoteConnect(LServerLink).Execute(LStatement, LParams);
			SystemEvaluateNode.UpdateRowFromParams(LOutRow, LParams);
			
			return null;
		}
	}
	
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row, var AOutParams : row) : generic;</remarks>
	public class SystemEvaluateOnNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LServerName = (string)AArguments[0];
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0]);
			Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
			if (LServerLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string LExpression = (string)AArguments[1];

			Row LInRow = AArguments.Length >= 3 ? (Row)AArguments[2] : null;
			Row LOutRow = AArguments.Length >= 4 ? (Row)AArguments[3] : null;
			
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AProgram, LInRow, LOutRow);
			DataValue LDataValue = AProgram.RemoteConnect(LServerLink).Evaluate(LExpression, LParams);
			SystemEvaluateNode.UpdateRowFromParams(LOutRow, LParams);
			
			return LDataValue is Scalar ? LDataValue.AsNative : LDataValue;
		}
	}
	
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row; var AOutParams : row);</remarks>
	public class SystemExecuteAsNode : InstructionNode
	{
		public static void ExecuteScript(ServerProcess AProcess, Program AProgram, PlanNode ANode, string AString, SessionInfo ASessionInfo)
		{
			ExecuteScript(AProcess, AProgram, ANode, AString, ASessionInfo, null, null);
		}
		
		public static void ExecuteScript(ServerProcess AProcess, Program AProgram, PlanNode ANode, string AScript, SessionInfo ASessionInfo, Row AInParams, Row AOutParams)
		{
			IServerSession LSession = ((IServer)AProcess.ServerSession.Server).Connect(ASessionInfo);
			try
			{
				IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
				try
				{
					SystemExecuteNode.ExecuteScript((ServerProcess)LProcess, AProgram, ANode, AScript, AInParams, AOutParams, null);
				}
				finally
				{
					LSession.StopProcess(LProcess);
				}
			}
			finally
			{
				((IServer)AProcess.ServerSession.Server).Disconnect(LSession);
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			ExecuteScript
			(
				AProgram.ServerProcess, 
				AProgram,
				this,
				(string)AArguments[0], 
				new SessionInfo((string)AArguments[1], (string)AArguments[2], AProgram.Plan.CurrentLibrary.Name),
				AArguments.Length >= 4 ? (Row)AArguments[3] : null,
				AArguments.Length >= 5 ? (Row)AArguments[4] : null
			);
			return null;
		}
	}

	// operator ExecuteAsync(const AScript : String);
	// operator ExecuteAsync(const AScript : String; const AInParams : row);
	// operator ExecuteAsync(const AProcessID : Integer, const AScript : String);
	// operator ExecuteAsync(const AProcessID : Integer, const AScript : String; const AInParams : row);
	public class SystemExecuteAsyncNode : InstructionNode
	{
		private delegate void ExecuteDelegate(DataParams AParams);
		
		private void ExecuteResults(IAsyncResult AResult)
		{
			try
			{
				IServerScript LScript = (IServerScript)AResult.AsyncState;
				IServerProcess LProcess = LScript.Process;
				if (LProcess != null)
				{
					try
					{
						LProcess.UnprepareScript(LScript);
					}
					finally
					{
						if (((ServerScript)LScript).ShouldCleanupProcess)
							LProcess.Session.StopProcess(LProcess);
					}
				}
			}
			catch
			{
				// do nothing, we are on a worker thread and an unhandled exception will bring down the application.
				// Cause that's what you'd want to do if you got an unhandled exception on a thread. Crash.
			}
		}
		
		private void ExecuteAsync(IServerProcess AProcess, Program AProgram, string AScript, Row AInParams, bool AShouldCleanup)
		{
			IServerScript LScript = AProcess.PrepareScript(AScript);
			((ServerScript)LScript).ShouldCleanupProcess = AShouldCleanup;
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AProgram, AInParams, null);
			new ExecuteDelegate(LScript.Execute).BeginInvoke(LParams, new AsyncCallback(ExecuteResults), LScript);
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
				ExecuteAsync
				(
					AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArguments[0]),
					AProgram,
					(string)AArguments[1],
					AArguments.Length >= 3 ? (Row)AArguments[2] : null,
					false
				);
			else
				ExecuteAsync
				(
					((IServerSession)AProgram.ServerProcess.ServerSession).StartProcess(new ProcessInfo(AProgram.ServerProcess.ServerSession.SessionInfo)),
					AProgram,
					(string)AArguments[0],
					AArguments.Length >= 2 ? (Row)AArguments[1] : null,
					true
				);
			return null;
		}
	}
	
	// operator ExecuteWithTimeout(const AScript : String, const ATimeout : Integer);
	// operator ExecuteWithTimeout(const AScript : String, const ATimeout : Integer; const AInParams : row);
	// operator ExecuteWithTimeout(const AScript : String, const ATimeout : Integer; const AInParams : row; var AOutParams : row);
	// operator ExecuteWithTimeout(const AProcessID : Integer, const AScript : String, const ATimeout : Integer);
	// operator ExecuteWithTimeout(const AProcessID : Integer, const AScript : String, const ATimeout : Integer; const AInParams : row);
	// operator ExecuteWithTimeout(const AProcessID : Integer, const AScript : String, const ATimeout : Integer; const AInParams : row; var AOutParams : row);
	public class SystemExecuteWithTimeoutNode : InstructionNode
	{
		private delegate void ExecuteDelegate(DataParams AParams);
		
		private bool FDone;
		private IServerScript FScript;
		
		private void ExecuteResults(IAsyncResult AResult)
		{
			FDone = true;
		}
		
		private void ExecuteWithTimeout(IServerProcess AProcess, Program AProgram, string AScript, int ATimeout, Row AInParams, Row AOutParams)
		{
			FDone = false;
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AProgram, AInParams, AOutParams);
			FScript = AProcess.PrepareScript(AScript);
			try
			{
				new ExecuteDelegate(FScript.Execute).BeginInvoke(LParams, new AsyncCallback(ExecuteResults), null);
				System.Threading.Thread.Sleep(ATimeout);
				if (!FDone)	
					throw new ServerException(ServerException.Codes.ExecutionTimeout);
					
				SystemEvaluateNode.UpdateRowFromParams(AOutParams, LParams);
			}
			finally
			{
				AProcess.UnprepareScript(FScript);
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{	
			if (Operator.Operands[0].DataType.Is(AProgram.DataTypes.SystemInteger))
				ExecuteWithTimeout
				(
					AProgram.ServerProcess.ServerSession.Processes.GetProcess((int)AArguments[0]),
					AProgram,
					(string)AArguments[1],
					(int)AArguments[2],
					AArguments.Length >= 4 ? (Row)AArguments[3] : null,
					AArguments.Length >= 5 ? (Row)AArguments[4] : null
				);
			else
				ExecuteWithTimeout
				(
					((IServerSession)AProgram.ServerProcess.ServerSession).StartProcess(new ProcessInfo(AProgram.ServerProcess.ServerSession.SessionInfo)),
					AProgram,
					(string)AArguments[0],
					(int)AArguments[1],
					AArguments.Length >= 3 ? (Row)AArguments[2] : null,
					AArguments.Length >= 4 ? (Row)AArguments[3] : null
				);
			return null;
		}
	}

	// operator System.ExecuteMultiple(const AStatement : String, const ANumber : Integer);	
	// operator System.ExecuteMultiple(const AStatement : String, const ANumber : Integer, const AUserID : String, const APassword : String);
	public class SystemExecuteMultipleNode : InstructionNode
	{
		bool[] FDoneList;
		bool FDone;
		
		private delegate void ExecuteDelegate(DataParams AParams);
		
		private void ExecuteResults(IAsyncResult AResult)
		{
			lock (FDoneList)
			{
				FDoneList[(int)AResult.AsyncState] = true;
				for (int LIndex = 0; LIndex < FDoneList.Length; LIndex++)
					if (!FDoneList[LIndex])
						return;
				FDone = true;
			}
		}
		
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LStatement = (string)AArguments[0];
			int LNumber = (int)AArguments[1];
			string LUserID = AArguments.Length > 2 ? (string)AArguments[2] : AProgram.ServerProcess.ServerSession.SessionInfo.UserID;
			string LPassword = AArguments.Length > 3 ? (string)AArguments[3] : AProgram.ServerProcess.ServerSession.SessionInfo.Password;
			IServer LServer = (IServer)AProgram.ServerProcess.ServerSession.Server;
			IServerSession[] LSessions = new IServerSession[LNumber];
			IServerProcess[] LProcesses = new IServerProcess[LNumber];
			IServerStatementPlan[] LPlans = new IServerStatementPlan[LNumber];
			ExecuteDelegate[] LDelegates = new ExecuteDelegate[LNumber];
			FDoneList = new bool[LNumber];
			try
			{
				FDone = false;
				for (int LIndex = 0; LIndex < LNumber; LIndex++)
				{
					LSessions[LIndex] = LServer.Connect(new SessionInfo(LUserID, LPassword));
					LProcesses[LIndex] = LSessions[LIndex].StartProcess(new ProcessInfo(LSessions[LIndex].SessionInfo));
					LPlans[LIndex] = LProcesses[LIndex].PrepareStatement(LStatement, null);
					LDelegates[LIndex] = new ExecuteDelegate(LPlans[LIndex].Execute);
					FDoneList[LIndex] = false;
				}
				
				for (int LIndex = 0; LIndex < LNumber; LIndex++)
					LDelegates[LIndex].BeginInvoke(null, new AsyncCallback(ExecuteResults), LIndex);
					
				while (!FDone);

				return null;
			}
			finally
			{
				for (int LIndex = 0; LIndex < LNumber; LIndex++)
				{
					try
					{
						if (LPlans[LIndex] != null)
							LProcesses[LIndex].UnprepareStatement(LPlans[LIndex]);
						if (LProcesses[LIndex] != null)
							LSessions[LIndex].StopProcess(LProcesses[LIndex]);
						if (LSessions[LIndex] != null)
							LServer.Disconnect(LSessions[LIndex]);
					}
					catch { }
				}
			}
		}
	}
	
	/// <remarks>operator Open(AExpression : System.String): System.Cursor;</remarks>
	public class SystemOpenNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LString = (string)AArguments[0];
			IServerExpressionPlan LPlan = ((IServerProcess)AProgram.ServerProcess).PrepareExpression(LString, null);
			try
			{
				LPlan.CheckCompiled();
				PlanNode LNode = ((ServerExpressionPlan)LPlan).Program.Code;
				if ((IsLiteral && !LNode.IsLiteral) || (IsFunctional && !LNode.IsFunctional) || (IsDeterministic && !LNode.IsDeterministic) || (IsRepeatable && !LNode.IsRepeatable) || (!IsNilable && LNode.IsNilable))
					throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(LNode));
				
				return LPlan.Evaluate(null);
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).UnprepareExpression(LPlan);
			}
		}
	}

	/// <remarks>operator Sleep(AMilliseconds : integer);</remarks>
	public class SystemSleepNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			System.Threading.Thread.Sleep((int)AArguments[0]);
			
			return null;
		}
	}
	
	/// <remarks>operator MachineName() : String;</remarks>
	public class SystemMachineNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return System.Environment.MachineName;
		}
	}
	
	/// <remarks>operator HostName() : String;</remarks>
	public class SystemHostNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.SessionInfo.HostName;
		}
	}

    /// <remarks> operator NewGuid() : Guid </remarks>
    public class NewGuidNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return Guid.NewGuid();
		}
    }
    
	/// <remarks>operator GetDefaultDeviceName();</remarks>    
	/// <remarks>operator GetDefaultDeviceName(ALibraryName : Name);</remarks>
    public class SystemGetDefaultDeviceNameNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments.Length == 0)
				return AProgram.Plan.DefaultDeviceName;
			else
				return AProgram.Plan.GetDefaultDeviceName((string)AArguments[0], false);
		}
    }

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemEnableErrorLoggingNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.Server.LogErrors = true;
			return null;
		}
	}

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemDisableErrorLoggingNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.Server.LogErrors = false;
			return null;
		}
	}

    /// <remarks>operator System.EncryptPassword(const AString : System.String) : System.String;</remarks>
    public class SystemEncryptPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return Schema.SecurityUtility.EncryptPassword((string)AArguments[0]);
		}
	}
    
    /// <remarks>operator CreateServerLinkUser(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			AProgram.Plan.CheckAuthorized(LUserID);
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			LServerLink.Users.Add(new Schema.ServerLinkUser(LUserID, LServerLink, (string)AArguments[2], Schema.SecurityUtility.EncryptPassword((string)AArguments[3])));
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator CreateServerLinkUserWithEncryptedPassword(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AEncryptedServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserWithEncryptedPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			AProgram.Plan.CheckAuthorized(LUserID);
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			LServerLink.Users.Add(new Schema.ServerLinkUser(LUserID, LServerLink, (string)AArguments[2], (string)AArguments[3]));
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator SetServerLinkUserID(AUserID : string, AServerLinkName : System.Name, AServerUserID : string); </remarks>
    public class SystemSetServerLinkUserIDNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			AProgram.Plan.CheckAuthorized(LUserID);
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			LServerLink.Users[LUserID].ServerLinkUserID = (string)AArguments[2];
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator SetServerLinkUserPassword(AUserID : string, AServerLinkName : System.Name, APassword : string); </remarks>
    public class SystemSetServerLinkUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			AProgram.Plan.CheckAuthorized(LUserID);
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			LServerLink.Users[LUserID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword((string)AArguments[2]);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator ChangeServerLinkUserPassword(AServerLinkName : System.Name, AOldPassword : string, APassword : string); </remarks>
    public class SystemChangeServerLinkUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[0]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			Schema.User LUser = AProgram.ServerProcess.ServerSession.User;
			if (String.Compare((string)AArguments[1], Schema.SecurityUtility.DecryptPassword(LServerLink.Users[LUser.ID].ServerLinkPassword), true) != 0)
				throw new ServerException(ServerException.Codes.InvalidPassword);
			LServerLink.Users[LUser.ID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword((string)AArguments[2]);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator DropServerLinkUser(AUserID : string, AServerLinkName : System.Name); </remarks>
    public class SystemDropServerLinkUserNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			AProgram.Plan.CheckAuthorized(LUserID);
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			
			// TODO: Prevent drop if user has active sessions on this server?
			
			if (!LServerLink.Users.ContainsKey(LUserID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ServerLinkUserNotFound, LUserID);

			LServerLink.Users.Remove(LUserID);
			AProgram.CatalogDeviceSession.UpdateCatalogObject(LServerLink);
			return null;
		}
    }
    
    /// <remarks>operator ServerLinkUserExists(AUserID : string, AServerLinkName : System.Name) : Boolean;</remarks>
    public class SystemServerLinkUserExistsNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			string LUserID = (string)AArguments[0];
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProgram.Plan, (string)AArguments[1]);
			if (!(LObject is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
			
			return LServerLink.Users.ContainsKey(LUserID);
		}
    }
    
    public class SystemStopProcessNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			// Only the Admin and System user can stop a process that was started on a different session
			if (AProgram.ServerProcess.ServerSession.User.IsAdminUser())
				AProgram.ServerProcess.ServerSession.Server.StopProcess((int)AArguments[0]);
			else
				AProgram.ServerProcess.ServerSession.StopProcess((int)AArguments[0]);
			return null;
		}
    }

    public class SystemCloseSessionNode : InstructionNode
    {
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.Server.CloseSession((int)AArguments[0]);
			return null;
		}
	}
	
	public class SystemBeginTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AArguments.Length == 0)
				AProgram.ServerProcess.BeginTransaction(AProgram.ServerProcess.DefaultIsolationLevel);
			else
				AProgram.ServerProcess.BeginTransaction((IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)AArguments[0], true));
			return null;
		}
	}
	
	public class SystemPrepareTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.PrepareTransaction();
			return null;
		}
	}
	
	public class SystemCommitTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.CommitTransaction();
			return null;
		}
	}
	
	public class SystemRollbackTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.RollbackTransaction();
			return null;
		}
	}
	
	public class SystemTransactionCountNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.TransactionCount;
		}
	}
	
	public class SystemInTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.InTransaction;
		}
	}
	
	public class SystemCollectNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			GC.Collect();
			return null;
		}
	}
	
	public class SystemGetTotalMemoryNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			return GC.GetTotalMemory(true);
		}
	}
	
	public class SystemGetPlanCountNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program AProgram)
		{
			return AProgram.ServerProcess.ServerSession.Server.PlanCacheCount;
		}
	}
	
	// operator ServerName() : System.Name;
	public class SystemServerNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.Name;
		}
	}
	
	// operator UserID() : string;
	public class SystemUserIDNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.User.ID;
		}
	}
	
	// operator UserName() : string;
	public class SystemUserNameNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.User.Name;
		}
	}

	// operator SessionID() : integer;	
	public class SystemSessionIDNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.SessionID;
		}
	}
	
	// operator ProcessID() : integer;
	public class SystemProcessIDNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ProcessID;
		}
	}
	
	// operator SetLanguage(ALanguage : string);
	public class SystemSetLanguageNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.SessionInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)AArguments[0], true);
			return null;
		}
	}
	
	// operator SetDefaultIsolationLevel(const AIsolationLevel : String);
	public class SystemSetDefaultIsolationLevelNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.SessionInfo.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)AArguments[0], true);
			return null;
		}
	}

	// operator SetIsolationLevel(const AIsolationLevel : String);
	public class SystemSetIsolationLevelNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)AArguments[0], true);
			return null;
		}
	}

	// operator SetDefaultMaxStackDepth(const ADefaultMaxStackDepth : Integer);
	public class SystemSetDefaultMaxStackDepthNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.SessionInfo.DefaultMaxStackDepth = (int)AArguments[0];
			return null;
		}
	}

	// operator SetDefaultMaxCallDepth(const ADefaultMaxCallDepth : Integer);
	public class SystemSetDefaultMaxCallDepthNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.SessionInfo.DefaultMaxCallDepth = (int)AArguments[0];
			return null;
		}
	}

	// operator SetMaxStackDepth(const AMaxStackDepth : Integer);
	public class SystemSetMaxStackDepthNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.Stack.MaxStackDepth = (int)AArguments[0];
			return null;
		}
	}
	
	// operator SetMaxCallDepth(const AMaxCallDepth : Integer);
	public class SystemSetMaxCallDepthNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.Stack.MaxCallDepth = (int)AArguments[0];
			return null;
		}
	}
	
	// operator SetDefaultUseImplicitTransactions(const ADefaultUseImplicitTransactions : Boolean);
	public class SystemSetDefaultUseImplicitTransactionsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.SessionInfo.DefaultUseImplicitTransactions = (bool)AArguments[0];
			return null;
		}
	}
	
	// operator SetUseImplicitTransactions(const AUseImplicitTransactions : Boolean);
	public class SystemSetUseImplicitTransactionsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.UseImplicitTransactions = (bool)AArguments[0];
			return null;
		}
	}
	
	// operator PushNonLoggedContext()
	public class SystemPushNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.NonLogged = true;
			return null;
		}
	}

	// operator PopNonLoggedContext()
	public class SystemPopNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.NonLogged = false;
			return null;
		}
	}
	
	// operator IsNonLoggedContext() : Boolean
	public class SystemIsNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.NonLogged;
		}
	}

	// operator DisableReconciliation()
	public class SystemDisableReconciliationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.DisableReconciliation();
			return null;
		}
	}

	// operator EnableReconciliation()
	public class SystemEnableReconciliationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.EnableReconciliation();
			return null;
		}
	}
	
	// operator IsReconciliationEnabled() : Boolean
	public class SystemIsReconciliationEnabledNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.IsReconciliationEnabled();
		}
	}

	// operator System.Diagnostics.LogError(const AError : Error);
	public class SystemLogErrorNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.Server.LogError((Exception)AArguments[0]);
			return null;
		}
	}
	
	// operator System.Diagnostics.LogMessage(const AMessage : String);
	public class SystemLogMessageNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.Server.LogMessage((string)AArguments[0]);
			return null;
		}
	}
	
	// operator iAs(const AValue : generic, const ADataType : System.Type) : generic
	public class AsNode : PlanNode
	{
		public override void DetermineCharacteristics(Plan APlan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = Nodes[0].IsNilable;
		}
		
		public static bool CanCastNative(Program AProgram, object AValue, Schema.IDataType ADataType)
		{
			if (AValue == null)
				return true;
				
			if ((ADataType == AProgram.DataTypes.SystemScalar) || (ADataType == AProgram.DataTypes.SystemGeneric))
				return true;
				
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (LScalarType.IsCompound)
				{
					NativeRow LNativeRow = AValue as NativeRow;
					if ((LNativeRow != null) && (LScalarType.CompoundRowType.Columns.Count == LNativeRow.Values.Length))
					{
						for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
							if (!CanCastNative(AProgram, LNativeRow.Values[LIndex], LScalarType.CompoundRowType.Columns[LIndex].DataType))
								return false;
						
						return true;
					}
				}
				else
					return ((LScalarType.NativeType != null) && (LScalarType.NativeType.Equals(AValue.GetType())));
			}
			
			return false;
		}

		public override object InternalExecute(Program AProgram)
		{
			object LObject = Nodes[0].Execute(AProgram);

			DataValue LValue = LObject as DataValue;
			if (LValue != null) 
			{
				if (!LValue.DataType.Is(FDataType))
					throw new RuntimeException(RuntimeException.Codes.InvalidCast, LValue.DataType.Name, FDataType.Name);
				return LValue;
			}
			
			if (!CanCastNative(AProgram, LObject, FDataType))
				throw new RuntimeException(RuntimeException.Codes.InvalidCast, LObject.GetType().Name, FDataType.Name);
				
			return LObject;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new AsExpression((Expression)Nodes[0].EmitStatement(AMode), FDataType.EmitSpecifier(AMode));
		}
	}

	// operator iIs(const AValue : generic, const ADataType : System.Type) : System.Boolean
	public class IsNode : PlanNode
	{
		private Schema.IDataType FTargetType;
		public Schema.IDataType TargetType
		{
			get { return FTargetType; }
			set { FTargetType = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = APlan.DataTypes.SystemBoolean;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = false;
		}

		public override object InternalExecute(Program AProgram)
		{
			object LObject = Nodes[0].Execute(AProgram);
			DataValue LValue = LObject as DataValue;
			if (LValue != null)
				return LValue.DataType.Is(FTargetType);
				
			return AsNode.CanCastNative(AProgram, LObject, FTargetType);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new IsExpression((Expression)Nodes[0].EmitStatement(AMode), FTargetType.EmitSpecifier(AMode));
		}
	}

	// create operator System.Diagnostics.BenchmarkStreamAllocation(const AAllocationCount : Integer, const AStepped : Boolean) : TimeSpan
	//		class "Alphora.Dataphor.DAE.Runtime.Instructions.SystemBenchmarkStreamAllocationNode,Alphora.Dataphor.DAE";
	public class SystemBenchmarkStreamAllocationNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			DateTime LStartTime = DateTime.Now;

			int LCount = (int)AArguments[0];
			StreamID[] LStreamIDs = null;
			bool LStepped = (bool)AArguments[1];
			if (LStepped)
				LStreamIDs = new StreamID[LCount];

			if (LStepped)				
			{
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					LStreamIDs[LIndex] = AProgram.StreamManager.Allocate();
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					AProgram.StreamManager.Deallocate(LStreamIDs[LIndex]);
			}
			else
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					AProgram.StreamManager.Deallocate(AProgram.StreamManager.Allocate());
			
			return DateTime.Now.Subtract(LStartTime);
		}
	}

	public class SystemBenchmarkParserNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			DateTime LStartTime = DateTime.Now;

			Statement LStatement = new Parser().ParseScript((string)AArguments[0], null);
						
			return DateTime.Now.Subtract(LStartTime);
		}
	}

	public class SystemBenchmarkCompilerNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long LAccumulator = 0;
			#endif
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				PlanNode LNode = Compiler.Compile(LPlan, new Parser().ParseScript((string)AArguments[0], null));
				#if ACCUMULATOR
				LAccumulator = LPlan.Accumulator;
				#endif
			}
			finally
			{
				LPlan.Dispose();
			}

			#if ACCUMULATOR			
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks - LAccumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#else
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#endif			
		}
	}
	
	public class SystemBenchmarkBindingNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long LAccumulator = 0;
			#endif
			Plan LPlan = new Plan(AProgram.ServerProcess);
			try
			{
				PlanNode LNode = Compiler.Compile(LPlan, new Parser().ParseScript((string)AArguments[0], null));
				LNode = Compiler.OptimizeNode(LPlan, LNode);
				LNode = Compiler.BindNode(LPlan, LNode);
				#if ACCUMULATOR
				LAccumulator = LPlan.Accumulator;
				#endif
			}
			finally
			{
				LPlan.Dispose();
			}

			#if ACCUMULATOR						
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks - LAccumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#else
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#endif
		}
	}
	
	public class SystemBenchmarkExecuteNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			SystemExecuteNode.ExecuteScript(AProgram.ServerProcess, AProgram, this, (string)AArguments[0], null);
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
		}
	}
	
	public class SystemLoadStringFromFileNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			using (System.IO.FileStream LStream = new System.IO.FileStream((string)AArguments[0], System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				using (System.IO.StreamReader LReader = new System.IO.StreamReader(LStream))
				{
					return LReader.ReadToEnd();
				}
			}
		}
	}
	
	public class SystemStreamCountNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.StreamManager.Count();
		}
	}
	
	public class SystemLockCountNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.LockManager.Count();
		}
	}
	
	public class SystemRowCountNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if USEROWMANAGER
			return AProgram.ServerProcess.RowManager.RowCount;
			#else
			return 0;	
			#endif
		}
	}
	
	public class SystemUsedRowCountNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			#if USEROWMANAGER
			return AProgram.ServerProcess.RowManager.UsedRowCount;
			#else
			return 0;
			#endif
		}
	}
	
	public class SystemSetLibraryNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			AProgram.ServerProcess.ServerSession.CurrentLibrary = AProgram.CatalogDeviceSession.ResolveLoadedLibrary((string)AArguments[0]);
			return null;
		}
	}
	
	// create operator System.StreamsOpen() : System.String class "System.SystemStreamsOpenNode";
	public class SystemStreamsOpenNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.StreamManager.StreamOpensAsString();
		}
	}

	// create operator System.LockEvents() : System.String class "System.SystemLockEventsNode";
	public class SystemLockEventsNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			return AProgram.ServerProcess.ServerSession.Server.LockManager.LockEventsAsString();
		}
	}
	
	// create operator ClearOperatorResolutionCache();
	public class SystemClearOperatorResolutionCacheNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
			AProgram.Catalog.OperatorResolutionCache.Clear();
			return null;
		}
	}

	// create operator ClearConversionPathCache();
	public class SystemClearConversionPathCacheNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{
			if (AProgram.ServerProcess.ServerSession.User.ID != AProgram.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProgram.ServerProcess.ServerSession.User.ID);
			AProgram.Catalog.ConversionPathCache.Clear();
			return null;
		}
	}
	
	// create operator GetInstanceSize(const AReference : String, const AMode : String) : Integer;
	public class SystemGetInstanceSizeNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program AProgram, object AArgument1, object AArgument2)
		{
			#if NILPROPOGATION
			if ((AArgument1 == null) || (AArgument2 == null))
				return null;
			#endif
				
			return 
				MemoryUtility.SizeOf
				(
					ReflectionUtility.ResolveReference
					(
						(string)AArgument1, 
						new Token("#Server", AProgram.ServerProcess.ServerSession.Server), 
						new Token("#Catalog", AProgram.Catalog), 
						new Token("#Program", AProgram),
						new Token("#Process", AProgram.ServerProcess), 
						new Token("#Session", AProgram.ServerProcess.ServerSession)
					), 
					(TraversalMode)Enum.Parse(typeof(TraversalMode), (string)AArgument2, true)
				);
		}
	}

	// operator GetInstanceSizes(const AReference : String, const AMode : String) : table { FieldName : Name, FieldType : Name, FieldSize : Integer };
	public class SystemGetInstanceSizesNode : TableNode
	{
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;

			DataType.Columns.Add(new Schema.Column("DeclaringType", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("FieldName", APlan.DataTypes.SystemName));			
			DataType.Columns.Add(new Schema.Column("FieldType", APlan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("FieldSize", APlan.DataTypes.SystemInteger));
			foreach (Schema.Column LColumn in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(LColumn));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["DeclaringType"], TableVar.Columns["FieldName"] }));

			TableVar.DetermineRemotable(APlan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(APlan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program AProgram)
		{
			LocalTable LResult = new LocalTable(this, AProgram);
			try
			{
				LResult.Open();

				// Populate the result
				Row LRow = new Row(AProgram.ValueManager, LResult.DataType.RowType);
				try
				{
					LRow.ValuesOwned = false;

					List<FieldSizeInfo> LFieldSizes = 
						MemoryUtility.SizesOf
						(
							ReflectionUtility.ResolveReference
							(
								(string)Nodes[0].Execute(AProgram), 
								new Token("#Server", AProgram.ServerProcess.ServerSession.Server), 
								new Token("#Catalog", AProgram.ServerProcess.ServerSession.Server.Catalog), 
								new Token("#Program", AProgram),
								new Token("#Process", AProgram.ServerProcess), 
								new Token("#Session", AProgram.ServerProcess.ServerSession)
							), 
							(TraversalMode)Enum.Parse(typeof(TraversalMode), (string)Nodes[1].Execute(AProgram), true)
						);
						
					for (int LIndex = 0; LIndex < LFieldSizes.Count; LIndex++)
					{
						LRow[0] = LFieldSizes[LIndex].DeclaringType;
						LRow[1] = LFieldSizes[LIndex].FieldName;
						LRow[2] = LFieldSizes[LIndex].FieldType;
						LRow[3] = LFieldSizes[LIndex].FieldSize;
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
}
