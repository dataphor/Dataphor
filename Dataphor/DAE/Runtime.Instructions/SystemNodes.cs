/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define UseElaborable
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
		public override object InternalExecute(Program program, object[] arguments)
		{
			switch (arguments.Length)
			{
				case 1 :
					// (String)
					#if NILPROPOGATION
					if (arguments[0] == null)
						return null;
					#endif
					
					return new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, (string)arguments[0]);
				
				case 2 :
					// (String, Error)
					// (Integer, String)
					// (String, String)
					#if NILPROPOGATION
					if (arguments[0] == null || arguments[1] == null)
						return null;
					#endif

					if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
						if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemString))
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[0], false), DataphorException.ApplicationError, (string)arguments[1]);
						else
							return new DataphorException(ErrorSeverity.User, DataphorException.ApplicationError, (string)arguments[0], (Exception)arguments[1]);

					return new DataphorException(ErrorSeverity.User, (int)arguments[0], (string)arguments[1]);
				
				case 3 :
					// (Integer, String, Error) (Code, Message, InnerError)
					// (String, String, Error) (Severity, Message, InnerError)
					// (String, Integer, String) (Severity, Code, Message)
					#if NILPROPOGATION
					if (arguments[0] == null || arguments[1] == null || arguments[2] == null)
						return null;
					#endif
					
					if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
						return new DataphorException(ErrorSeverity.User, (int)arguments[0], (string)arguments[1], (Exception)arguments[2]);
					else
					{
						if (Operator.Operands[1].DataType.Is(program.DataTypes.SystemInteger))
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[0], false), (int)arguments[1], (string)arguments[2]);
						else
							return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[0], false), DataphorException.ApplicationError, (string)arguments[1], (Exception)arguments[2]);
					}
				
				default :
					// (String Integer, String, Error)
					#if NILPROPOGATION
					if (arguments[0] == null || arguments[1] == null || arguments[2] == null || arguments[3] == null)
						return null;
					#endif
					
					return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[0], false), (int)arguments[1], (string)arguments[2], (Exception)arguments[3]);
			}
		}
	}
	
	/// <remarks>operator System.Error.ReadSeverity(const AValue : Error) : String;</remarks>
	public class SystemErrorReadSeverityNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			DataphorException exception = (Exception)arguments[0] as DataphorException;
			return (exception == null ? ErrorSeverity.Application : exception.Severity).ToString();
		}
	}
	
	/// <remarks>operator System.Error.WriteSeverity(const AValue : Error, const ASeverity : String) : Error;</remarks>
	public class SystemErrorWriteSeverityNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif

			Exception exception = (Exception)arguments[0];
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
				return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[1], false), dataphorException.Code, dataphorException.Message, dataphorException.InnerException);
			else
				return new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), (string)arguments[1], false), DataphorException.ApplicationError, exception.Message, exception.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadCode(const AValue : Error) : Integer;</remarks>
	public class SystemErrorReadCodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			DataphorException exception = (Exception)arguments[0] as DataphorException;
			return exception == null ? DataphorException.ApplicationError : exception.Code;
		}
	}
	
	/// <remarks>operator System.Error.WriteCode(const AValue : Error, const ACode : Integer) : Error;</remarks>
	public class SystemErrorWriteCodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif

			Exception exception = (Exception)arguments[0];
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
				return new DataphorException(dataphorException.Severity, (int)arguments[1], dataphorException.Message, dataphorException.InnerException);
			else
				return new DataphorException(ErrorSeverity.Application, (int)arguments[1], exception.Message, exception.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadMessage(const AValue : Error) : String;</remarks>
	public class SystemErrorReadMessageNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return ((Exception)arguments[0]).Message;
		}
	}
	
	/// <remarks>operator System.Error.WriteMessage(const AValue : Error, const AMessage : String) : Error;</remarks>
	public class SystemErrorWriteMessageNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif

			Exception exception = (Exception)arguments[0];
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
				return new DataphorException(dataphorException.Severity, dataphorException.Code, (string)arguments[1], dataphorException.InnerException);
			else
				return new DataphorException(ErrorSeverity.Application, DataphorException.ApplicationError, (string)arguments[1], exception.InnerException);
		}
	}
	
	/// <remarks>operator System.Error.ReadInnerError(const AValue : Error) : Error;</remarks>
	public class SystemErrorReadInnerErrorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return ((Exception)arguments[0]).InnerException;
		}
	}
	
	/// <remarks>operator System.Error.WriteInnerError(const AValue : Error, const AInnerError : Error) : Error;</remarks>
	public class SystemErrorWriteInnerErrorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null || arguments[1] == null)
				return null;
			#endif

			Exception exception = (Exception)arguments[0];
			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
				return new DataphorException(dataphorException.Severity, dataphorException.Code, dataphorException.Message, (Exception)arguments[1]);
			else
				return new DataphorException(ErrorSeverity.Application, DataphorException.ApplicationError, exception.Message, (Exception)arguments[1]);
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetErrorDescriptionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return ExceptionUtility.BriefDescription((Exception)arguments[0]);
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetDetailedErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetDetailedErrorDescriptionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			if (arguments[0] == null)
				return null;
			#endif

			return ExceptionUtility.DetailedDescription((Exception)arguments[0]);
		}
	}
	
    /// <remarks> operator System.Binary.Binary(AValue : String) : System.Binary </remarks>
    public class SystemBinarySelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			using (Scalar scalar = new Scalar(program.ValueManager, (Schema.ScalarType)_dataType, program.StreamManager.Allocate()))
			{
				scalar.AsBase64String = (string)argument1;
				return scalar.StreamID;
			}
		}
    }

    // SystemBinaryReadAccessorNode
    public class SystemBinaryReadAccessorNode : UnaryInstructionNode
    {
		public SystemBinaryReadAccessorNode() : base()
		{
			IsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			using 
			(
				Scalar scalar = 
					(argument1 is StreamID) 
						? new Scalar(program.ValueManager, program.DataTypes.SystemBinary, (StreamID)argument1) 
						: new Scalar(program.ValueManager, program.DataTypes.SystemBinary, argument1)
			)
			{
				scalar.ValuesOwned = false;
				return scalar.AsBase64String;
			}
		}

		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsOrderPreserving = true;
		}
    }
    
    // SystemBinaryWriteAccessorNode
    public class SystemBinaryWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			
			using (Scalar scalar = new Scalar(program.ValueManager, (Schema.ScalarType)_dataType, (StreamID)argument1))
			{
				scalar.AsBase64String = (string)argument2;
				return scalar.StreamID;
			}
		}
    }
    
    /// <remarks> operator System.Guid.Guid(AValue : String) : System.Guid </remarks>
    public class SystemGuidSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return new Guid((string)argument1);
		}
    }

    // SystemGuidReadAccessorNode
    public class SystemGuidReadAccessorNode : UnaryInstructionNode
    {
		public SystemGuidReadAccessorNode() : base()
		{
			IsOrderPreserving = true;
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsOrderPreserving = true;
		}

		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return ((Guid)argument1).ToString();
		}
    }
    
    // SystemGuidWriteAccessorNode
    public class SystemGuidWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif

			return new Guid((string)argument2);
		}
    }
    
    // ScalarSelectorNode
    public class ScalarSelectorNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return DataValue.CopyValue(program.ValueManager, argument1);
		}
    }

	// ValidatingScalarSelectorNode
	public class ValidatingScalarSelectorNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, null);
			#endif

			return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, DataValue.CopyValue(program.ValueManager, argument1));
		}
	}
    
    // ScalarReadAccessorNode
    public class ScalarReadAccessorNode : UnaryInstructionNode
    {
		public ScalarReadAccessorNode() : base()
		{
			IsOrderPreserving = true;
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			return DataValue.CopyValue(program.ValueManager, argument1);
		}
    }
    
    // ScalarWriteAccessorNode
    public class ScalarWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			#endif
			
			return DataValue.CopyValue(program.ValueManager, argument2);
		}
    }

	// ValidatingScalarWriteAccessorNode
	public class ValidatingScalarWriteAccessorNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, null);
			#endif
			
			return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, DataValue.CopyValue(program.ValueManager, argument2));
		}
	}
    
    // CompoundScalarSelectorNode
    public class CompoundScalarSelectorNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			#if NILPROPOGATION
			for (int index = 0; index < arguments.Length; index++)
				if (arguments[index] == null)
					return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, null);
			#endif
			
			Schema.IRowType rowType = ((Schema.ScalarType)_dataType).CompoundRowType;
			Row row = new Row(program.ValueManager, rowType);
			for (int index = 0; index < rowType.Columns.Count; index++)
				row[index] = arguments[index];

			return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, row.AsNative);
		}
    }
    
    public class CompoundScalarReadAccessorNode : UnaryInstructionNode
    {
		private string _propertyName;
		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value; }
		}
		
		private int _propertyIndex;
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			Schema.IRowType compoundRowType = ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType;
			_propertyIndex = compoundRowType.Columns.IndexOfName(PropertyName);
			if (!compoundRowType.Columns[_propertyIndex].DataType.Equals(_dataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, plan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, _dataType.Name);
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif
			
			if (_dataType is Schema.IScalarType)
				return DataValue.CopyValue(program.ValueManager, ((NativeRow)argument1).Values[_propertyIndex]);
			return 
				DataValue.FromNativeRow
				(
					program.ValueManager, 
					((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, 
					(NativeRow)argument1, 
					_propertyIndex
				).Copy(program.ValueManager);
		}
    }
    
    public class CompoundScalarWriteAccessorNode : BinaryInstructionNode
    {
		private string _propertyName;
		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value; }
		}
		
		private int _propertyIndex;
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			Schema.IRowType compoundRowType = ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType;
			_propertyIndex = compoundRowType.Columns.IndexOfName(PropertyName);
			if (!compoundRowType.Columns[_propertyIndex].DataType.Equals(Nodes[1].DataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, plan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, _dataType.Name);
		}
		
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, null);
			#endif
			
			NativeRow result = (NativeRow)DataValue.CopyValue(program.ValueManager, argument1);
			using (Row row = new Row(program.ValueManager, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, result))
			{
				row[_propertyIndex] = argument2;
			}

			return ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType, result);
		}
    }
    
    // ScalarIsSpecialNode
    public class ScalarIsSpecialNode : UnaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			#endif

			return false;
		}
    }
    
    // operator IsNil(AValue : generic) : Boolean;
    public class IsNilNode : UnaryInstructionNode
    {
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsNilable = false;
		}

		public override object InternalExecute(Program program, object argument1)
		{
			return argument1 == null;
		}
    }

	// operator IsNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNilRowNode : BinaryInstructionNode
	{
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsNilable = false;
		}

		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return true;
			#endif

			return !(((IRow)argument1).HasValue((string)argument2));
		}
	}

    // operator IsNotNil(AValue : generic) : Boolean;
    public class IsNotNilNode : UnaryInstructionNode
    {
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsNilable = false;
		}

		public override object InternalExecute(Program program, object argument1)
		{
			return !(argument1 == null);
		}
    }

	// operator IsNotNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNotNilRowNode : BinaryInstructionNode
	{
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsNilable = false;
		}

		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return false;
			#endif

			return (((IRow)argument1).HasValue((string)argument2));
		}
	}

	// operator IfNil(AValue : generic, AValue : generic) : generic;
    public class IfNilNode : InstructionNodeBase
    {
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsNilable = Nodes[1].IsNilable;
		}

		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);

			if (Nodes[1].DataType.Is(Nodes[0].DataType))
			{
				_dataType = Nodes[0].DataType;
				Nodes[1] = Compiler.Upcast(plan, Nodes[1], _dataType);
			}
			else if (Nodes[0].DataType.Is(Nodes[1].DataType))
			{
				_dataType = Nodes[1].DataType;
				Nodes[0] = Compiler.Upcast(plan, Nodes[0], _dataType);
			}
			else
			{
				ConversionContext context = Compiler.FindConversionPath(plan, Nodes[1].DataType, Nodes[0].DataType);
				if (context.CanConvert)
				{
					_dataType = Nodes[0].DataType;
					Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[0].DataType);
				}
				else
				{
					context = Compiler.FindConversionPath(plan, Nodes[0].DataType, Nodes[1].DataType);
					if (context.CanConvert)
					{
						_dataType = Nodes[1].DataType;
						Nodes[0] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[0], context), Nodes[1].DataType);
					}
					else
						Compiler.CheckConversionContext(plan, context);
				}
			}

			_dataType = Nodes[0].DataType;
		}
		
		public override object InternalExecute(Program program)
		{
			object result = Nodes[0].Execute(program);
			if (result == null)
				result = Nodes[1].Execute(program);
			return DataValue.CopyValue(program.ValueManager, result);
		}
    }
    
    /// <remarks>operator System.Diagnostics.IsSupported(AStatement : String, ADeviceName : Name) : Boolean;</remarks>
    public class SystemIsSupportedNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string deviceName = (string)arguments[0];
			string statementString = (string)arguments[1];
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, deviceName, true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
				
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				ParserMessages parserMessages = new ParserMessages();
				Statement statement = new Parser().ParseStatement(statementString, parserMessages);
				plan.Messages.AddRange(parserMessages);

				PlanNode node = Compiler.Compile(plan, statement);
				if (plan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, plan.Messages.ToString(CompilerErrorLevel.NonFatal));
				if (node is FrameNode)
					node = node.Nodes[0];
				if ((node is ExpressionStatementNode) || (node is CursorNode))
					node = node.Nodes[0];
				return node.DeviceSupported && (node.Device == device);
			}
			finally
			{
				plan.Dispose();
			}
		}
    }

	//	operator Reconcile() : table { Sequence : Integer, Error : Error };
	//	operator Reconcile(ADeviceName : System.Name) : table { Sequence : Integer, Error : Error };
	//	operator Reconcile(ADeviceName : System.Name, ATableName : System.Name) : table { Sequence : Integer, Error : Error };
	public class SystemReconcileNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));			
			DataType.Columns.Add(new Schema.Column("Error", plan.DataTypes.SystemError));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{TableVar.Columns["Sequence"]}));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					string deviceName;
					
					if (Nodes.Count > 0)
						deviceName = (string)Nodes[0].Execute(program);
					else
						deviceName = program.Plan.DefaultDeviceName;

					Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, deviceName, true) as Schema.Device;
					if (device == null)
						throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
						
					program.Plan.CheckRight(device.GetRight(Schema.RightNames.Reconcile));
					ErrorList errorList;
					if (Nodes.Count == 2)
					{
						string tableName = (string)Nodes[1].Execute(program);
						Schema.TableVar tableVar = Compiler.ResolveCatalogIdentifier(program.Plan, tableName, false) as Schema.TableVar;
						if (tableVar == null)
							tableVar = new Schema.BaseTableVar(tableName, new Schema.TableType(), device);
							
						errorList = device.Reconcile(program.ServerProcess, tableVar);
					}
					else
						errorList = device.Reconcile(program.ServerProcess);
						
					for (int index = 0; index < errorList.Count; index++)
					{
						row[0] = index;
						row[1] = errorList[index];
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class SystemEnsureDeviceStartedNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string deviceName = (string)arguments[0];
			Schema.Device device = Compiler.ResolveCatalogIdentifier(program.Plan, deviceName, true) as Schema.Device;
			if (device == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			program.ServerProcess.EnsureDeviceStarted(device);
			return null;
		}
	}
	
	public class SystemShowPlanNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string statementString = (string)arguments[0];
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				ParserMessages parserMessages = new ParserMessages();
				Statement statement = new Parser().ParseScript(statementString, parserMessages);
				Block block = statement as Block;
				if (block.Statements.Count == 1)
					statement = block.Statements[0];
				plan.Messages.AddRange(parserMessages);
				PlanNode node = Compiler.Compile(plan, statement);
				if (plan.Messages.HasErrors)
					throw new ServerException(ServerException.Codes.UncompiledPlan, plan.Messages.ToString(CompilerErrorLevel.NonFatal));

				System.IO.StringWriter stringWriter = new System.IO.StringWriter();
				System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(stringWriter, new System.Xml.XmlWriterSettings { Indent = true });
				node.WritePlan(writer);
				writer.Flush();

				return stringWriter.ToString();
			}
			finally
			{
				plan.Dispose();
			}
		}
	}

	/// <remarks>operator System.Diagnostics.GetRestrictionAlgorithm(AExpression : String) : String;</remarks>
	public class SystemGetRestrictionAlgorithmNode : InstructionNode    
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				PlanNode node = Compiler.Compile(plan, (string)arguments[0]);

				node = node.ExtractNode<RestrictNode>();
			
				return ((RestrictNode)node).RestrictionAlgorithm.Name;
			}
			finally
			{
				plan.Dispose();
			}
		}
	}
    
	/// <remarks>operator System.Diagnostics.GetJoinAlgorithm(AExpression : String) : String;</remarks>
	public class SystemGetJoinAlgorithmNode : InstructionNode    
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				PlanNode node = Compiler.Compile(plan, (string)arguments[0]);

				node = node.ExtractNode<JoinNode>();
			
				return ((JoinNode)node).JoinAlgorithm.Name;
			}
			finally
			{
				plan.Dispose();
			}
		}
	}
    
	/// <remarks>operator StartProcess() : Integer;</remarks>
	public class SystemStartProcessNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return ((IServerSession)program.ServerProcess.ServerSession).StartProcess(new ProcessInfo(program.ServerProcess.ServerSession.SessionInfo)).ProcessID;
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
		public static void ExecuteScript(ServerProcess process, Program program, PlanNode node, string script, DebugLocator locator)
		{
			ExecuteScript(process, program, node, script, null, null, locator);
		}

		public static void ExecuteScript(ServerProcess process, Program program, PlanNode node, string script, IRow inParams, IRow outParams, DebugLocator locator)
		{
			DataParams paramsValue = SystemEvaluateNode.ParamsFromRows(program, inParams, outParams);
			process.PushProcessLocals();
			try
			{
				IServerProcess localProcess = (IServerProcess)process;
				IServerScript localScript = localProcess.PrepareScript(script, locator);
				try
				{
					localScript.Execute(paramsValue);
				}
				finally
				{
					localProcess.UnprepareScript(localScript);
				}
				SystemEvaluateNode.UpdateRowFromParams(outParams, paramsValue);
			}
			finally
			{
				process.PopProcessLocals();
			}
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
				ExecuteScript
				(
					program.ServerProcess.ServerSession.Processes.GetProcess((int)arguments[0]),
					program,
					this,
					(string)arguments[1],
					arguments.Length >= 3 ? (IRow)arguments[2] : null,
					arguments.Length >= 4 ? (IRow)arguments[3] : null,
					null
				);
			else
				ExecuteScript
				(
					program.ServerProcess,
					program,
					this,
					(string)arguments[0],
					arguments.Length >= 2 ? (IRow)arguments[1] : null,
					arguments.Length >= 3 ? (IRow)arguments[2] : null,
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
		public static DataParams ParamsFromRows(Program program, IRow inParams, IRow outParams)
		{
			DataParams paramsValue = new DataParams();
			int outIndex;
			for (int index = 0; index < ((inParams == null) ? 0 : inParams.DataType.Columns.Count); index++)
			{
				outIndex = outParams == null ? -1 : outParams.DataType.Columns.IndexOfName(inParams.DataType.Columns[index].Name);
				if (outIndex >= 0)
					paramsValue.Add(new DataParam(inParams.DataType.Columns[index].Name, inParams.DataType.Columns[index].DataType, Modifier.Var, DataValue.CopyValue(program.ValueManager, inParams[index])));
				else
					paramsValue.Add(new DataParam(inParams.DataType.Columns[index].Name, inParams.DataType.Columns[index].DataType, Modifier.In, DataValue.CopyValue(program.ValueManager, inParams[index])));
			}
			
			for (int index = 0; index < ((outParams == null) ? 0 : outParams.DataType.Columns.Count); index++)
			{
				if (!paramsValue.Contains(outParams.DataType.Columns[index].Name))
					paramsValue.Add(new DataParam(outParams.DataType.Columns[index].Name, outParams.DataType.Columns[index].DataType, Modifier.Var, DataValue.CopyValue(program.ValueManager, outParams[index])));
			}
			return paramsValue;
		}

		public static void UpdateRowFromParams(IRow outParams, DataParams paramsValue)
		{
			if (outParams != null)
				for (int index = 0; index < outParams.DataType.Columns.Count; index++)
					outParams[index] = paramsValue[paramsValue.IndexOf(outParams.DataType.Columns[index].Name)].Value;
		}
		
		private object Evaluate(ServerProcess process, Program program, PlanNode node, string expression, IRow inParams, IRow outParams)
		{
			DataParams paramsValue = ParamsFromRows(program, inParams, outParams);
			
			process.PushProcessLocals();
			try
			{
				IServerProcess localProcess = (IServerProcess)process;
				IServerExpressionPlan plan = localProcess.PrepareExpression(expression, paramsValue);
				try
				{
					plan.CheckCompiled();
					PlanNode localNode = ((ServerExpressionPlan)plan).Program.Code;
					if ((IsLiteral && !localNode.IsLiteral) || (IsFunctional && !localNode.IsFunctional) || (IsDeterministic && !localNode.IsDeterministic) || (IsRepeatable && !localNode.IsRepeatable) || (!IsNilable && localNode.IsNilable))
						throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(localNode));
					
					IDataValue result = plan.Evaluate(paramsValue);
					UpdateRowFromParams(outParams, paramsValue);
					return (result == null || result.IsNil) ? null : (result is IScalar ? result.AsNative : result);
				}
				finally
				{
					localProcess.UnprepareExpression(plan);
				}
			}
			finally
			{
				process.PopProcessLocals();
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
				return 
					Evaluate
					(
						program.ServerProcess.ServerSession.Processes.GetProcess((int)arguments[0]),
						program,
						this,
						(string)arguments[1],
						arguments.Length >= 3 ? (IRow)arguments[2] : null,
						arguments.Length >= 4 ? (IRow)arguments[3] : null
					);
			else
				return 
					Evaluate
					(
						program.ServerProcess, 
						program,
						this,
						(string)arguments[0], 
						arguments.Length >= 2 ? (IRow)arguments[1] : null, 
						arguments.Length >= 3 ? (IRow)arguments[2] : null
					);
		}
	}

	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row, var AOutParams : row) 
	public class SystemExecuteOnNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string serverName = (string)arguments[0];
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0]);
			Schema.ServerLink serverLink = objectValue as Schema.ServerLink;
			if (serverLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string statement = (string)arguments[1];
			
			IRow inRow = arguments.Length >= 3 ? (IRow)arguments[2] : null;
			IRow outRow = arguments.Length >= 4 ? (IRow)arguments[3] : null;
			
			DataParams paramsValue = SystemEvaluateNode.ParamsFromRows(program, inRow, outRow);
			program.RemoteConnect(serverLink).Execute(statement, paramsValue);
			SystemEvaluateNode.UpdateRowFromParams(outRow, paramsValue);
			
			return null;
		}
	}
	
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row, var AOutParams : row) : generic;</remarks>
	public class SystemEvaluateOnNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string serverName = (string)arguments[0];
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0]);
			Schema.ServerLink serverLink = objectValue as Schema.ServerLink;
			if (serverLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string expression = (string)arguments[1];

			IRow inRow = arguments.Length >= 3 ? (IRow)arguments[2] : null;
			IRow outRow = arguments.Length >= 4 ? (IRow)arguments[3] : null;
			
			DataParams paramsValue = SystemEvaluateNode.ParamsFromRows(program, inRow, outRow);
			IDataValue dataValue = program.RemoteConnect(serverLink).Evaluate(expression, paramsValue);
			SystemEvaluateNode.UpdateRowFromParams(outRow, paramsValue);
			
			return (dataValue == null || dataValue.IsNil) ? null : (dataValue is IScalar ? dataValue.AsNative : dataValue);
		}
	}
	
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row; var AOutParams : row);</remarks>
	public class SystemExecuteAsNode : InstructionNode
	{
		public static void ExecuteScript(ServerProcess process, Program program, PlanNode node, string stringValue, SessionInfo sessionInfo)
		{
			ExecuteScript(process, program, node, stringValue, sessionInfo, null, null);
		}
		
		public static void ExecuteScript(ServerProcess process, Program program, PlanNode node, string script, SessionInfo sessionInfo, IRow inParams, IRow outParams)
		{
			IServerSession session = ((IServer)process.ServerSession.Server).Connect(sessionInfo);
			try
			{
				IServerProcess localProcess = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					SystemExecuteNode.ExecuteScript((ServerProcess)localProcess, program, node, script, inParams, outParams, null);
				}
				finally
				{
					session.StopProcess(localProcess);
				}
			}
			finally
			{
				((IServer)process.ServerSession.Server).Disconnect(session);
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			ExecuteScript
			(
				program.ServerProcess, 
				program,
				this,
				(string)arguments[0], 
				new SessionInfo((string)arguments[1], (string)arguments[2], program.Plan.CurrentLibrary.Name),
				arguments.Length >= 4 ? (IRow)arguments[3] : null,
				arguments.Length >= 5 ? (IRow)arguments[4] : null
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
		
		private void ExecuteResults(IAsyncResult result)
		{
			try
			{
				IServerScript script = (IServerScript)result.AsyncState;
				IServerProcess process = script.Process;
				if (process != null)
				{
					try
					{
						process.UnprepareScript(script);
					}
					finally
					{
						if (((ServerScript)script).ShouldCleanupProcess)
							process.Session.StopProcess(process);
					}
				}
			}
			catch
			{
				// do nothing, we are on a worker thread and an unhandled exception will bring down the application.
				// Cause that's what you'd want to do if you got an unhandled exception on a thread. Crash.
			}
		}
		
		private void ExecuteAsync(IServerProcess process, Program program, string script, IRow inParams, bool shouldCleanup)
		{
			IServerScript localScript = process.PrepareScript(script);
			((ServerScript)localScript).ShouldCleanupProcess = shouldCleanup;
			DataParams paramsValue = SystemEvaluateNode.ParamsFromRows(program, inParams, null);
			new ExecuteDelegate(localScript.Execute).BeginInvoke(paramsValue, new AsyncCallback(ExecuteResults), localScript);
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
				ExecuteAsync
				(
					program.ServerProcess.ServerSession.Processes.GetProcess((int)arguments[0]),
					program,
					(string)arguments[1],
					arguments.Length >= 3 ? (IRow)arguments[2] : null,
					false
				);
			else
				ExecuteAsync
				(
					((IServerSession)program.ServerProcess.ServerSession).StartProcess(new ProcessInfo(program.ServerProcess.ServerSession.SessionInfo)),
					program,
					(string)arguments[0],
					arguments.Length >= 2 ? (IRow)arguments[1] : null,
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
		
		private bool _done;
		private IServerScript _script;
		
		private void ExecuteResults(IAsyncResult result)
		{
			_done = true;
		}
		
		private void ExecuteWithTimeout(IServerProcess process, Program program, string script, int timeout, IRow inParams, IRow outParams)
		{
			_done = false;
			DataParams paramsValue = SystemEvaluateNode.ParamsFromRows(program, inParams, outParams);
			_script = process.PrepareScript(script);
			try
			{
				new ExecuteDelegate(_script.Execute).BeginInvoke(paramsValue, new AsyncCallback(ExecuteResults), null);
				System.Threading.Thread.Sleep(timeout);
				if (!_done)	
					throw new ServerException(ServerException.Codes.ExecutionTimeout);
					
				SystemEvaluateNode.UpdateRowFromParams(outParams, paramsValue);
			}
			finally
			{
				process.UnprepareScript(_script);
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{	
			if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemInteger))
				ExecuteWithTimeout
				(
					program.ServerProcess.ServerSession.Processes.GetProcess((int)arguments[0]),
					program,
					(string)arguments[1],
					(int)arguments[2],
					arguments.Length >= 4 ? (IRow)arguments[3] : null,
					arguments.Length >= 5 ? (IRow)arguments[4] : null
				);
			else
				ExecuteWithTimeout
				(
					((IServerSession)program.ServerProcess.ServerSession).StartProcess(new ProcessInfo(program.ServerProcess.ServerSession.SessionInfo)),
					program,
					(string)arguments[0],
					(int)arguments[1],
					arguments.Length >= 3 ? (IRow)arguments[2] : null,
					arguments.Length >= 4 ? (IRow)arguments[3] : null
				);
			return null;
		}
	}

	// operator System.ExecuteMultiple(const AStatement : String, const ANumber : Integer);	
	// operator System.ExecuteMultiple(const AStatement : String, const ANumber : Integer, const AUserID : String, const APassword : String);
	public class SystemExecuteMultipleNode : InstructionNode
	{
		bool[] _doneList;
		bool _done;
		
		private delegate void ExecuteDelegate(DataParams AParams);
		
		private void ExecuteResults(IAsyncResult result)
		{
			lock (_doneList)
			{
				_doneList[(int)result.AsyncState] = true;
				for (int index = 0; index < _doneList.Length; index++)
					if (!_doneList[index])
						return;
				_done = true;
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			string statement = (string)arguments[0];
			int number = (int)arguments[1];
			string userID = arguments.Length > 2 ? (string)arguments[2] : program.ServerProcess.ServerSession.SessionInfo.UserID;
			string password = arguments.Length > 3 ? (string)arguments[3] : program.ServerProcess.ServerSession.SessionInfo.Password;
			IServer server = (IServer)program.ServerProcess.ServerSession.Server;
			IServerSession[] sessions = new IServerSession[number];
			IServerProcess[] processes = new IServerProcess[number];
			IServerStatementPlan[] plans = new IServerStatementPlan[number];
			ExecuteDelegate[] delegates = new ExecuteDelegate[number];
			_doneList = new bool[number];
			try
			{
				_done = false;
				for (int index = 0; index < number; index++)
				{
					sessions[index] = server.Connect(new SessionInfo(userID, password));
					processes[index] = sessions[index].StartProcess(new ProcessInfo(sessions[index].SessionInfo));
					plans[index] = processes[index].PrepareStatement(statement, null);
					delegates[index] = new ExecuteDelegate(plans[index].Execute);
					_doneList[index] = false;
				}
				
				for (int index = 0; index < number; index++)
					delegates[index].BeginInvoke(null, new AsyncCallback(ExecuteResults), index);
					
				while (!_done);

				return null;
			}
			finally
			{
				for (int index = 0; index < number; index++)
				{
					try
					{
						if (plans[index] != null)
							processes[index].UnprepareStatement(plans[index]);
						if (processes[index] != null)
							sessions[index].StopProcess(processes[index]);
						if (sessions[index] != null)
							server.Disconnect(sessions[index]);
					}
					catch { }
				}
			}
		}
	}
	
	/// <remarks>operator Open(AExpression : System.String): System.Cursor;</remarks>
	public class SystemOpenNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string stringValue = (string)arguments[0];
			IServerExpressionPlan plan = ((IServerProcess)program.ServerProcess).PrepareExpression(stringValue, null);
			try
			{
				plan.CheckCompiled();
				PlanNode node = ((ServerExpressionPlan)plan).Program.Code;
				if ((IsLiteral && !node.IsLiteral) || (IsFunctional && !node.IsFunctional) || (IsDeterministic && !node.IsDeterministic) || (IsRepeatable && !node.IsRepeatable) || (!IsNilable && node.IsNilable))
					throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(node));
				
				return plan.Evaluate(null);
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareExpression(plan);
			}
		}
	}

	/// <remarks>operator Sleep(AMilliseconds : integer);</remarks>
	public class SystemSleepNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			System.Threading.Thread.Sleep((int)arguments[0]);
			
			return null;
		}
	}
	
    /// <remarks> operator NewGuid() : Guid </remarks>
    public class NewGuidNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Guid.NewGuid();
		}
    }
    
	/// <remarks>operator GetDefaultDeviceName();</remarks>    
	/// <remarks>operator GetDefaultDeviceName(ALibraryName : Name);</remarks>
    public class SystemGetDefaultDeviceNameNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 0)
				return program.Plan.DefaultDeviceName;
			else
				return program.Plan.GetDefaultDeviceName((string)arguments[0], false);
		}
    }

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemEnableErrorLoggingNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.LogErrors = true;
			return null;
		}
	}

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemDisableErrorLoggingNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.LogErrors = false;
			return null;
		}
	}

    /// <remarks>operator System.EncryptPassword(const AString : System.String) : System.String;</remarks>
    public class SystemEncryptPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			return Schema.SecurityUtility.EncryptPassword((string)arguments[0]);
		}
	}
    
    /// <remarks>operator CreateServerLinkUser(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			serverLink.Users.Add(new Schema.ServerLinkUser(userID, serverLink, (string)arguments[2], Schema.SecurityUtility.EncryptPassword((string)arguments[3])));
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator CreateServerLinkUserWithEncryptedPassword(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AEncryptedServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserWithEncryptedPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			serverLink.Users.Add(new Schema.ServerLinkUser(userID, serverLink, (string)arguments[2], (string)arguments[3]));
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator SetServerLinkUserID(AUserID : string, AServerLinkName : System.Name, AServerUserID : string); </remarks>
    public class SystemSetServerLinkUserIDNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			serverLink.Users[userID].ServerLinkUserID = (string)arguments[2];
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator SetServerLinkUserPassword(AUserID : string, AServerLinkName : System.Name, APassword : string); </remarks>
    public class SystemSetServerLinkUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			serverLink.Users[userID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword((string)arguments[2]);
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator ChangeServerLinkUserPassword(AServerLinkName : System.Name, AOldPassword : string, APassword : string); </remarks>
    public class SystemChangeServerLinkUserPasswordNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[0]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			Schema.User user = program.ServerProcess.ServerSession.User;
			if (!String.Equals((string)arguments[1], Schema.SecurityUtility.DecryptPassword(serverLink.Users[user.ID].ServerLinkPassword), StringComparison.OrdinalIgnoreCase))
				throw new ServerException(ServerException.Codes.InvalidPassword);
			serverLink.Users[user.ID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword((string)arguments[2]);
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator DropServerLinkUser(AUserID : string, AServerLinkName : System.Name); </remarks>
    public class SystemDropServerLinkUserNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			program.Plan.CheckAuthorized(userID);
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			
			// TODO: Prevent drop if user has active sessions on this server?
			
			if (!serverLink.Users.ContainsKey(userID))
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ServerLinkUserNotFound, userID);

			serverLink.Users.Remove(userID);
			program.CatalogDeviceSession.UpdateCatalogObject(serverLink);
			return null;
		}
    }
    
    /// <remarks>operator ServerLinkUserExists(AUserID : string, AServerLinkName : System.Name) : Boolean;</remarks>
    public class SystemServerLinkUserExistsNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			string userID = (string)arguments[0];
			Schema.Object objectValue = Compiler.ResolveCatalogIdentifier(program.Plan, (string)arguments[1]);
			if (!(objectValue is Schema.ServerLink))
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
			Schema.ServerLink serverLink = (Schema.ServerLink)objectValue;
			
			return serverLink.Users.ContainsKey(userID);
		}
    }
    
    public class SystemStopProcessNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			// Only the Admin and System user can stop a process that was started on a different session
			if (program.ServerProcess.ServerSession.User.IsAdminUser())
				program.ServerProcess.ServerSession.Server.StopProcess((int)arguments[0]);
			else
				program.ServerProcess.ServerSession.StopProcess((int)arguments[0]);
			return null;
		}
    }

    public class SystemCloseSessionNode : InstructionNode
    {
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.CloseSession((int)arguments[0]);
			return null;
		}
	}
	
	public class SystemBeginTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (arguments.Length == 0)
				program.ServerProcess.BeginTransaction(program.ServerProcess.DefaultIsolationLevel);
			else
				program.ServerProcess.BeginTransaction((IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)arguments[0], true));
			return null;
		}
	}
	
	public class SystemPrepareTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.PrepareTransaction();
			return null;
		}
	}
	
	public class SystemCommitTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.CommitTransaction();
			return null;
		}
	}
	
	public class SystemRollbackTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.RollbackTransaction();
			return null;
		}
	}
	
	public class SystemTransactionCountNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.TransactionCount;
		}
	}
	
	public class SystemInTransactionNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.InTransaction;
		}
	}
	
	public class SystemCollectNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			GC.Collect();
			return null;
		}
	}
	
	public class SystemGetTotalMemoryNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			return GC.GetTotalMemory(true);
		}
	}
	
	public class SystemGetPlanCountNode : NilaryInstructionNode
	{
		public override object NilaryInternalExecute(Program program)
		{
			return program.ServerProcess.ServerSession.Server.PlanCacheCount;
		}
	}
	
	// operator ServerName() : System.Name;
	public class SystemServerNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.Name;
		}
	}
	
	// operator UserID() : string;
	public class SystemUserIDNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.User.ID;
		}
	}
	
	// operator UserName() : string;
	public class SystemUserNameNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.User.Name;
		}
	}

	// operator SessionID() : integer;	
	public class SystemSessionIDNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.SessionID;
		}
	}
	
	// operator ProcessID() : integer;
	public class SystemProcessIDNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ProcessID;
		}
	}
	
	// operator SetLanguage(ALanguage : string);
	public class SystemSetLanguageNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ProcessInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), (string)arguments[0], true);
			return null;
		}
	}
	
	// operator SetDefaultIsolationLevel(const AIsolationLevel : String);
	public class SystemSetDefaultIsolationLevelNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.SessionInfo.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)arguments[0], true);
			return null;
		}
	}

	// operator SetIsolationLevel(const AIsolationLevel : String);
	public class SystemSetIsolationLevelNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), (string)arguments[0], true);
			return null;
		}
	}

	// operator SetDefaultMaxStackDepth(const ADefaultMaxStackDepth : Integer);
	public class SystemSetDefaultMaxStackDepthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.SessionInfo.DefaultMaxStackDepth = (int)arguments[0];
			return null;
		}
	}

	// operator SetDefaultMaxCallDepth(const ADefaultMaxCallDepth : Integer);
	public class SystemSetDefaultMaxCallDepthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.SessionInfo.DefaultMaxCallDepth = (int)arguments[0];
			return null;
		}
	}

	// operator SetMaxStackDepth(const AMaxStackDepth : Integer);
	public class SystemSetMaxStackDepthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.Stack.MaxStackDepth = (int)arguments[0];
			return null;
		}
	}
	
	// operator SetMaxCallDepth(const AMaxCallDepth : Integer);
	public class SystemSetMaxCallDepthNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.Stack.MaxCallDepth = (int)arguments[0];
			return null;
		}
	}
	
	// operator SetDefaultUseImplicitTransactions(const ADefaultUseImplicitTransactions : Boolean);
	public class SystemSetDefaultUseImplicitTransactionsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.SessionInfo.DefaultUseImplicitTransactions = (bool)arguments[0];
			return null;
		}
	}
	
	// operator SetUseImplicitTransactions(const AUseImplicitTransactions : Boolean);
	public class SystemSetUseImplicitTransactionsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.UseImplicitTransactions = (bool)arguments[0];
			return null;
		}
	}
	
	// operator PushNonLoggedContext()
	public class SystemPushNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.NonLogged = true;
			return null;
		}
	}

	// operator PopNonLoggedContext()
	public class SystemPopNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.NonLogged = false;
			return null;
		}
	}
	
	// operator IsNonLoggedContext() : Boolean
	public class SystemIsNonLoggedContextNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.NonLogged;
		}
	}

	// operator DisableReconciliation()
	public class SystemDisableReconciliationNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.DisableReconciliation();
			return null;
		}
	}

	// operator EnableReconciliation()
	public class SystemEnableReconciliationNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.EnableReconciliation();
			return null;
		}
	}
	
	// operator IsReconciliationEnabled() : Boolean
	public class SystemIsReconciliationEnabledNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.IsReconciliationEnabled();
		}
	}

	// operator System.Diagnostics.LogError(const AError : Error);
	public class SystemLogErrorNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.LogError((Exception)arguments[0]);
			return null;
		}
	}
	
	// operator System.Diagnostics.LogMessage(const AMessage : String);
	public class SystemLogMessageNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.Server.LogMessage((string)arguments[0]);
			return null;
		}
	}

    public class TableAsNode : UnaryTableNode
    {
        public override object InternalExecute(Program program)
        {
            return SourceNode.Execute(program);
        }

		public override void DetermineCharacteristics(Plan plan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = Nodes[0].IsNilable;
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			
			CopyTableVarColumns(SourceTableVar.Columns);
			CopyPreservedKeys(SourceTableVar.Keys);
			CopyPreservedOrders(SourceTableVar.Orders);

			Order = CopyOrder(SourceNode.Order);

			if ((Order != null) && !TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			return new AsExpression((Expression)Nodes[0].EmitStatement(mode), _dataType.EmitSpecifier(mode));
		}
    }

	// operator iAs(const AValue : generic, const ADataType : System.Type) : generic
	public class AsNode : PlanNode
	{
		public override void DetermineCharacteristics(Plan plan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = Nodes[0].IsNilable;
		}
		
		public static bool CanCastNative(Program program, object tempValue, Schema.IDataType dataType)
		{
			if (tempValue == null)
				return true;
				
			if ((dataType.Equals(program.DataTypes.SystemScalar)) || (dataType.Equals(program.DataTypes.SystemGeneric)))
				return true;
				
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (scalarType.IsCompound)
				{
					NativeRow nativeRow = tempValue as NativeRow;
					if ((nativeRow != null) && (scalarType.CompoundRowType.Columns.Count == nativeRow.Values.Length))
					{
						for (int index = 0; index < nativeRow.Values.Length; index++)
							if (!CanCastNative(program, nativeRow.Values[index], scalarType.CompoundRowType.Columns[index].DataType))
								return false;
						
						return true;
					}
				}
				else
					return ((scalarType.NativeType != null) && (scalarType.NativeType.Equals(tempValue.GetType())));
			}
			
			return false;
		}

		public override object InternalExecute(Program program)
		{
			object objectValue = Nodes[0].Execute(program);

			DataValue tempValue = objectValue as DataValue;
			if (tempValue != null) 
			{
				if (!tempValue.DataType.Is(_dataType))
					throw new RuntimeException(RuntimeException.Codes.InvalidCast, tempValue.DataType.Name, _dataType.Name);
				return tempValue;
			}
			
			if (!CanCastNative(program, objectValue, _dataType))
				throw new RuntimeException(RuntimeException.Codes.InvalidCast, objectValue.GetType().Name, _dataType.Name);
				
			return objectValue;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new AsExpression((Expression)Nodes[0].EmitStatement(mode), _dataType.EmitSpecifier(mode));
		}
	}

	// operator iIs(const AValue : generic, const ADataType : System.Type) : System.Boolean
	public class IsNode : PlanNode
	{
		private Schema.IDataType _targetType;
		public Schema.IDataType TargetType
		{
			get { return _targetType; }
			set { _targetType = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newIsNode = (IsNode)newNode;
			newIsNode._targetType = _targetType;
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = plan.DataTypes.SystemBoolean;
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = false;
		}

		public override object InternalExecute(Program program)
		{
			object objectValue = Nodes[0].Execute(program);
			DataValue tempValue = objectValue as DataValue;
			if (tempValue != null)
				return tempValue.DataType.Is(_targetType);
				
			return AsNode.CanCastNative(program, objectValue, _targetType);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new IsExpression((Expression)Nodes[0].EmitStatement(mode), _targetType.EmitSpecifier(mode));
		}
	}

	// create operator System.Diagnostics.BenchmarkStreamAllocation(const AAllocationCount : Integer, const AStepped : Boolean) : TimeSpan
	//		class "Alphora.Dataphor.DAE.Runtime.Instructions.SystemBenchmarkStreamAllocationNode,Alphora.Dataphor.DAE";
	public class SystemBenchmarkStreamAllocationNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			DateTime startTime = DateTime.Now;

			int count = (int)arguments[0];
			StreamID[] streamIDs = null;
			bool stepped = (bool)arguments[1];
			if (stepped)
				streamIDs = new StreamID[count];

			if (stepped)				
			{
				for (int index = 0; index < count; index++)
					streamIDs[index] = program.StreamManager.Allocate();
				for (int index = 0; index < count; index++)
					program.StreamManager.Deallocate(streamIDs[index]);
			}
			else
				for (int index = 0; index < count; index++)
					program.StreamManager.Deallocate(program.StreamManager.Allocate());
			
			return DateTime.Now.Subtract(startTime);
		}
	}

	public class SystemBenchmarkParserNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			DateTime startTime = DateTime.Now;

			Statement statement = new Parser().ParseScript((string)arguments[0], null);
						
			return DateTime.Now.Subtract(startTime);
		}
	}

	public class SystemBenchmarkCompilerNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			long startTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long accumulator = 0;
			#endif
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				PlanNode node = Compiler.Compile(plan, new Parser().ParseScript((string)arguments[0], null));
				#if ACCUMULATOR
				accumulator = plan.Accumulator;
				#endif
			}
			finally
			{
				plan.Dispose();
			}

			#if ACCUMULATOR			
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks - accumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#else
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#endif			
		}
	}
	
	public class SystemBenchmarkBindingNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			long startTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long accumulator = 0;
			#endif
			Plan plan = new Plan(program.ServerProcess);
			try
			{
				PlanNode node = Compiler.Compile(plan, (string)arguments[0]);
				#if ACCUMULATOR
				accumulator = plan.Accumulator;
				#endif
			}
			finally
			{
				plan.Dispose();
			}

			#if ACCUMULATOR						
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks - accumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#else
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
			#endif
		}
	}
	
	public class SystemBenchmarkExecuteNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			long startTicks = TimingUtility.CurrentTicks;
			SystemExecuteNode.ExecuteScript(program.ServerProcess, program, this, (string)arguments[0], null);
			return new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - startTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond));
		}
	}
	
	public class SystemLoadStringFromFileNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			using (System.IO.FileStream stream = new System.IO.FileStream((string)arguments[0], System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
	
	public class SystemStreamCountNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.StreamManager.Count();
		}
	}
	
	public class SystemLockCountNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			// TODO: Restore lock management
			return 0;
		}
	}
	
	public class SystemRowCountNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
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
		public override object InternalExecute(Program program, object[] arguments)
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
		public override object InternalExecute(Program program, object[] arguments)
		{
			program.ServerProcess.ServerSession.CurrentLibrary = program.CatalogDeviceSession.ResolveLoadedLibrary((string)arguments[0]);
			return null;
		}
	}
	
	// create operator System.StreamsOpen() : System.String class "System.SystemStreamsOpenNode";
	public class SystemStreamsOpenNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			return program.ServerProcess.ServerSession.Server.StreamManager.StreamOpensAsString();
		}
	}

	// create operator System.LockEvents() : System.String class "System.SystemLockEventsNode";
	public class SystemLockEventsNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			// TODO: implement once a lock manager is reintroduced
			return "";
		}
	}
	
	// create operator ClearOperatorResolutionCache();
	public class SystemClearOperatorResolutionCacheNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (program.ServerProcess.ServerSession.User.ID != program.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.ServerProcess.ServerSession.User.ID);
			program.Catalog.OperatorResolutionCache.Clear();
			return null;
		}
	}

	// create operator ClearConversionPathCache();
	public class SystemClearConversionPathCacheNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			if (program.ServerProcess.ServerSession.User.ID != program.ServerProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, program.ServerProcess.ServerSession.User.ID);
			program.Catalog.ConversionPathCache.Clear();
			return null;
		}
	}
	
	// create operator GetInstanceSize(const AReference : String, const AMode : String) : Integer;
	public class SystemGetInstanceSizeNode : BinaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if ((argument1 == null) || (argument2 == null))
				return null;
			#endif
				
			return 
				MemoryUtility.SizeOf
				(
					ReflectionUtility.ResolveReference
					(
						(string)argument1, 
						new Token("#Server", program.ServerProcess.ServerSession.Server), 
						new Token("#Catalog", program.Catalog), 
						new Token("#Program", program),
						new Token("#Process", program.ServerProcess), 
						new Token("#Session", program.ServerProcess.ServerSession)
					), 
					(TraversalMode)Enum.Parse(typeof(TraversalMode), (string)argument2, true)
				);
		}
	}

	// operator GetInstanceSizes(const AReference : String, const AMode : String) : table { FieldName : Name, FieldType : Name, FieldSize : Integer };
	public class SystemGetInstanceSizesNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("DeclaringType", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("FieldName", plan.DataTypes.SystemName));			
			DataType.Columns.Add(new Schema.Column("FieldType", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("FieldSize", plan.DataTypes.SystemInteger));
			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["DeclaringType"], TableVar.Columns["FieldName"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					List<FieldSizeInfo> fieldSizes = 
						MemoryUtility.SizesOf
						(
							ReflectionUtility.ResolveReference
							(
								(string)Nodes[0].Execute(program), 
								new Token("#Server", program.ServerProcess.ServerSession.Server), 
								new Token("#Catalog", program.ServerProcess.ServerSession.Server.Catalog), 
								new Token("#Program", program),
								new Token("#Process", program.ServerProcess), 
								new Token("#Session", program.ServerProcess.ServerSession)
							), 
							(TraversalMode)Enum.Parse(typeof(TraversalMode), (string)Nodes[1].Execute(program), true)
						);
						
					for (int index = 0; index < fieldSizes.Count; index++)
					{
						row[0] = fieldSizes[index].DeclaringType;
						row[1] = fieldSizes[index].FieldName;
						row[2] = fieldSizes[index].FieldType;
						row[3] = fieldSizes[index].FieldSize;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
}
