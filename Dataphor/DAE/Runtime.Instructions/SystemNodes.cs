/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define NILPROPOGATION

using System; 
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			switch (AArguments.Length)
			{
				case 1 :
					// (String)
					#if NILPROPOGATION
					if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
						return new DataVar(FDataType, null);
					#endif
					
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, AArguments[0].Value.AsString)));
				
				case 2 :
					// (String, Error)
					// (Integer, String)
					// (String, String)
					#if NILPROPOGATION
					if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
						return new DataVar(FDataType, null);
					#endif

					if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemString))
						if (AArguments[1].DataType.Is(AProcess.DataTypes.SystemString))
							return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[0].Value.AsString), DataphorException.CApplicationError, AArguments[1].Value.AsString)));
						else
							return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.User, DataphorException.CApplicationError, AArguments[0].Value.AsString, AArguments[1].Value.AsException)));

					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.User, AArguments[0].Value.AsInt32, AArguments[1].Value.AsString)));
				
				case 3 :
					// (Integer, String, Error) (Code, Message, InnerError)
					// (String, String, Error) (Severity, Message, InnerError)
					// (String, Integer, String) (Severity, Code, Message)
					#if NILPROPOGATION
					if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil)
						return new DataVar(FDataType, null);
					#endif
					
					if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
						return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.User, AArguments[0].Value.AsInt32, AArguments[1].Value.AsString, AArguments[2].Value.AsException)));
					else
					{
						if (AArguments[1].DataType.Is(AProcess.DataTypes.SystemInteger))
							return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[0].Value.AsString), AArguments[1].Value.AsInt32, AArguments[2].Value.AsString)));
						else
							return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[0].Value.AsString), DataphorException.CApplicationError, AArguments[1].Value.AsString, AArguments[2].Value.AsException)));
					}
				
				default :
					// (String Integer, String, Error)
					#if NILPROPOGATION
					if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil || (AArguments[2].Value == null) || AArguments[2].Value.IsNil || (AArguments[3].Value == null) || AArguments[3].Value.IsNil)
						return new DataVar(FDataType, null);
					#endif
					
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[0].Value.AsString), AArguments[1].Value.AsInt32, AArguments[2].Value.AsString, AArguments[3].Value.AsException)));
			}
		}
	}
	
	/// <remarks>operator System.Error.ReadSeverity(const AValue : Error) : String;</remarks>
	public class SystemErrorReadSeverityNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			DataphorException LException = AArguments[0].Value.AsException as DataphorException;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (LException == null ? ErrorSeverity.Application : LException.Severity).ToString()));
		}
	}
	
	/// <remarks>operator System.Error.WriteSeverity(const AValue : Error, const ASeverity : String) : Error;</remarks>
	public class SystemErrorWriteSeverityNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Exception LException = AArguments[0].Value.AsException;
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[1].Value.AsString), LDataphorException.Code, LDataphorException.Message, LDataphorException.InnerException)));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException((ErrorSeverity)Enum.Parse(typeof(ErrorSeverity), AArguments[1].Value.AsString), DataphorException.CApplicationError, LException.Message, LException.InnerException)));
		}
	}
	
	/// <remarks>operator System.Error.ReadCode(const AValue : Error) : Integer;</remarks>
	public class SystemErrorReadCodeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			DataphorException LException = AArguments[0].Value.AsException as DataphorException;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LException == null ? DataphorException.CApplicationError : LException.Code));
		}
	}
	
	/// <remarks>operator System.Error.WriteCode(const AValue : Error, const ACode : Integer) : Error;</remarks>
	public class SystemErrorWriteCodeNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Exception LException = AArguments[0].Value.AsException;
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(LDataphorException.Severity, AArguments[1].Value.AsInt32, LDataphorException.Message, LDataphorException.InnerException)));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.Application, AArguments[1].Value.AsInt32, LException.Message, LException.InnerException)));
		}
	}
	
	/// <remarks>operator System.Error.ReadMessage(const AValue : Error) : String;</remarks>
	public class SystemErrorReadMessageNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsException.Message));
		}
	}
	
	/// <remarks>operator System.Error.WriteMessage(const AValue : Error, const AMessage : String) : Error;</remarks>
	public class SystemErrorWriteMessageNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Exception LException = AArguments[0].Value.AsException;
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(LDataphorException.Severity, LDataphorException.Code, AArguments[1].Value.AsString, LDataphorException.InnerException)));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.Application, DataphorException.CApplicationError, AArguments[1].Value.AsString, LException.InnerException)));
		}
	}
	
	/// <remarks>operator System.Error.ReadInnerError(const AValue : Error) : Error;</remarks>
	public class SystemErrorReadInnerErrorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsException.InnerException));
		}
	}
	
	/// <remarks>operator System.Error.WriteInnerError(const AValue : Error, const AInnerError : Error) : Error;</remarks>
	public class SystemErrorWriteInnerErrorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Exception LException = AArguments[0].Value.AsException;
			DataphorException LDataphorException = LException as DataphorException;
			if (LDataphorException != null)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(LDataphorException.Severity, LDataphorException.Code, LDataphorException.Message, AArguments[1].Value.AsException)));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new DataphorException(ErrorSeverity.Application, DataphorException.CApplicationError, LException.Message, AArguments[1].Value.AsException)));
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetErrorDescriptionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ExceptionUtility.BriefDescription(AArguments[0].Value.AsException)));
		}
	}
	
	/// <remarks>operator System.Diagnostics.GetDetailedErrorDescription(const AValue : Error) : String;</remarks>
	public class SystemGetDetailedErrorDescriptionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ExceptionUtility.DetailedDescription(AArguments[0].Value.AsException)));
		}
	}
	
    /// <remarks> operator System.Binary.Binary(AValue : String) : System.Binary </remarks>
    public class SystemBinarySelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Scalar LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, ((IStreamManager)AProcess).Allocate());
			LScalar.AsBase64String = AArguments[0].Value.AsString;
			return new DataVar(FDataType, LScalar);
		}
    }

    // SystemBinaryReadAccessorNode
    public class SystemBinaryReadAccessorNode : InstructionNode
    {
		public SystemBinaryReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsBase64String));
		}

		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    // SystemBinaryWriteAccessorNode
    public class SystemBinaryWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Scalar LScalar = new Scalar(AProcess, (Schema.ScalarType)FDataType, ((IStreamManager)AProcess).Allocate());
			LScalar.AsBase64String = AArguments[1].Value.AsString;
			return new DataVar(FDataType, LScalar);
		}
    }
    
    /// <remarks> operator System.Guid.Guid(AValue : String) : System.Guid </remarks>
    public class SystemGuidSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Guid(AArguments[0].Value.AsString)));
		}
    }

    // SystemGuidReadAccessorNode
    public class SystemGuidReadAccessorNode : InstructionNode
    {
		public SystemGuidReadAccessorNode() : base()
		{
			FIsOrderPreserving = true;
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AArguments[0].Value.AsGuid.ToString()));
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsOrderPreserving = true;
		}
    }
    
    // SystemGuidWriteAccessorNode
    public class SystemGuidWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new Guid(AArguments[1].Value.AsString)));
		}
    }
    
    // ScalarSelectorNode
    public class ScalarSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, AArguments[0].Value.CopyAs(FDataType));
		}
    }
    
    // ScalarReadAccessorNode
    public class ScalarReadAccessorNode : InstructionNode
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, AArguments[0].Value.CopyAs(FDataType));
		}
    }
    
    // ScalarWriteAccessorNode
    public class ScalarWriteAccessorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, AArguments[1].Value.CopyAs(FDataType));
		}
    }
    
    // CompoundScalarSelectorNode
    public class CompoundScalarSelectorNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			for (int LIndex = 0; LIndex < AArguments.Length; LIndex++)
				if ((AArguments[LIndex].Value == null) || AArguments[LIndex].Value.IsNil)
					return new DataVar(FDataType, null);
			#endif
			
			Schema.IRowType LRowType = ((Schema.ScalarType)FDataType).CompoundRowType;
			Row LRow = new Row(AProcess, LRowType);
			for (int LIndex = 0; LIndex < LRowType.Columns.Count; LIndex++)
				LRow[LIndex] = AArguments[LIndex].Value;
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LRow.AsNative));
		}
    }
    
    public class CompoundScalarReadAccessorNode : InstructionNode
    {
		private string FPropertyName;
		public string PropertyName
		{
			get { return FPropertyName; }
			set { FPropertyName = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			if (!((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Equals(FDataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, APlan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, FDataType.Name);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			using (Row LRow = new Row(AProcess, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, (NativeRow)AArguments[0].Value.AsNative))
			{
				return new DataVar(FDataType, LRow[FPropertyName].Copy());
			}
		}
    }
    
    public class CompoundScalarWriteAccessorNode : InstructionNode
    {
		private string FPropertyName;
		public string PropertyName
		{
			get { return FPropertyName; }
			set { FPropertyName = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			if (!((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Equals(Nodes[1].DataType))
				throw new CompilerException(CompilerException.Codes.ExpressionTypeMismatch, APlan.CurrentStatement(), ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns[PropertyName].DataType.Name, FDataType.Name);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			Scalar LResult = AArguments[0].Value.Copy() as Scalar;
			using (Row LRow = new Row(AProcess, LResult.DataType.CompoundRowType, (NativeRow)LResult.AsNative))
			{
				LRow[FPropertyName] = AArguments[1].Value;
			}
			
			return new DataVar(FDataType, LResult);
		}
    }
    
    // ScalarIsSpecialNode
    public class ScalarIsSpecialNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif

			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));
		}
    }
    
    // operator IsNil(AValue : generic) : Boolean;
    public class IsNilNode : InstructionNode
    {
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, (AArguments[0].Value == null) || AArguments[0].Value.IsNil));
		}
    }

	// operator IsNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNilRowNode : InstructionNode
	{
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, true));
			#endif

			return new DataVar
			(
				FDataType, 
				new Scalar
				(
					AProcess,
					(Schema.ScalarType)FDataType,
					!(((Row)AArguments[0].Value).HasValue(AArguments[1].Value.AsString))
				)			 
			);
		}
	}

    // operator IsNotNil(AValue : generic) : Boolean;
    public class IsNotNilNode : InstructionNode
    {
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, !((AArguments[0].Value == null) || AArguments[0].Value.IsNil)));
		}
    }

	// operator IsNotNil(AValue : row, AColumnName : System.String) : Boolean;
	public class IsNotNilRowNode : InstructionNode
	{
		public override void DetermineCharacteristics(Plan APlan)
		{
			base.DetermineCharacteristics(APlan);
			FIsNilable = false;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if NILPROPOGATION
			if ((AArguments[0].Value == null) || AArguments[0].Value.IsNil || (AArguments[1].Value == null) || AArguments[1].Value.IsNil)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, false));
			#endif

			return new DataVar
			(
				FDataType, 
				new Scalar
				(
					AProcess,
					(Schema.ScalarType)FDataType,
					(((Row)AArguments[0].Value).HasValue(AArguments[1].Value.AsString))
				)			 
			);
		}
	}

	// operator IfNil(AValue : generic, AValue : generic) : generic;
    public class IfNilNode : InstructionNode
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
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LResult = Nodes[0].Execute(AProcess);
			if ((LResult.Value == null) || LResult.Value.IsNil)
				LResult = Nodes[1].Execute(AProcess);
			return new DataVar(LResult.DataType, LResult.Value == null ? null : LResult.Value.Copy());
		}
    }
    
    /// <remarks>operator System.Diagnostics.IsSupported(AStatement : String, ADeviceName : Name) : Boolean;</remarks>
    public class SystemIsSupportedNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LDeviceName = AArguments[0].Value.AsString;
			string LStatementString = AArguments[1].Value.AsString;
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, LDeviceName, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
				
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					ParserMessages LParserMessages = new ParserMessages();
					Statement LStatement = new Parser().ParseStatement(LStatementString, LParserMessages);
					LPlan.Messages.AddRange(LParserMessages);
					PlanNode LNode = Compiler.Compile(LPlan.Plan, LStatement);
					if (LPlan.Messages.HasErrors)
						throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));
					if (LNode is FrameNode)
						LNode = LNode.Nodes[0];
					if ((LNode is ExpressionStatementNode) || (LNode is CursorNode))
						LNode = LNode.Nodes[0];
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LNode.DeviceSupported && (LNode.Device == LDevice)));
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
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

			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));			
			DataType.Columns.Add(new Schema.Column("Error", APlan.Catalog.DataTypes.SystemError));
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

					string LDeviceName;
					
					if (Nodes.Count > 0)
						LDeviceName = Nodes[0].Execute(AProcess).Value.AsString;
					else
						LDeviceName = AProcess.Plan.DefaultDeviceName;

					Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, LDeviceName, true) as Schema.Device;
					if (LDevice == null)
						throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
						
					AProcess.Plan.CheckRight(LDevice.GetRight(Schema.RightNames.Reconcile));
					ErrorList LErrorList;
					if (Nodes.Count == 2)
					{
						string LTableName = Nodes[1].Execute(AProcess).Value.AsString;
						Schema.TableVar LTableVar = Compiler.ResolveCatalogIdentifier(AProcess.Plan, LTableName, false) as Schema.TableVar;
						if (LTableVar == null)
							LTableVar = new Schema.BaseTableVar(LTableName, new Schema.TableType(), LDevice);
							
						LErrorList = LDevice.Reconcile(AProcess, LTableVar);
					}
					else
						LErrorList = LDevice.Reconcile(AProcess);
						
					for (int LIndex = 0; LIndex < LErrorList.Count; LIndex++)
					{
						LRow[0].AsInt32 = LIndex;
						LRow[1].AsException = LErrorList[LIndex];
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

	public class SystemEnsureDeviceStartedNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LDeviceName = AArguments[0].Value.AsString;
			Schema.Device LDevice = Compiler.ResolveCatalogIdentifier(AProcess.Plan, LDeviceName, true) as Schema.Device;
			if (LDevice == null)
				throw new CompilerException(CompilerException.Codes.DeviceIdentifierExpected);
			AProcess.EnsureDeviceStarted(LDevice);
			return null;
		}
	}
	
	public class SystemShowPlanNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LStatementString = AArguments[0].Value.AsString;
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					ParserMessages LParserMessages = new ParserMessages();
					Statement LStatement = new Parser().ParseScript(LStatementString, LParserMessages);
					Block LBlock = LStatement as Block;
					if (LBlock.Statements.Count == 1)
						LStatement = LBlock.Statements[0];
					LPlan.Messages.AddRange(LParserMessages);
					PlanNode LNode = Compiler.Compile(LPlan.Plan, LStatement);
					if (LPlan.Messages.HasErrors)
						throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));

					System.IO.StringWriter LStringWriter = new System.IO.StringWriter();
					System.Xml.XmlTextWriter LWriter = new System.Xml.XmlTextWriter(LStringWriter);
					LWriter.Formatting = System.Xml.Formatting.Indented;
					LWriter.Indentation = 4;
					LNode.WritePlan(LWriter);
					LWriter.Flush();

					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LStringWriter.ToString()));
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					PlanNode LNode = Compiler.CompileExpression(LPlan.Plan, new Parser().ParseExpression(AArguments[0].Value.AsString));
					LPlan.CheckCompiled();

					LNode = Compiler.BindNode(LPlan.Plan, LNode);

					if (!(LNode is RestrictNode))
						throw new Exception("Restrict expression expected");
				
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((RestrictNode)LNode).RestrictionAlgorithm.Name));
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					PlanNode LNode = Compiler.CompileExpression(LPlan.Plan, new Parser().ParseExpression(AArguments[0].Value.AsString));
					if (LPlan.Messages.HasErrors)
						throw new ServerException(ServerException.Codes.UncompiledPlan, LPlan.Messages.ToString(CompilerErrorLevel.NonFatal));
						
					LNode = Compiler.BindNode(LPlan.Plan, LNode);
					if (!(LNode is JoinNode))
						throw new Exception("Join expression expected");
				
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((JoinNode)LNode).JoinAlgorithm.Name));
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, ((IServerSession)AProcess.ServerSession).StartProcess(new ProcessInfo(AProcess.ServerSession.SessionInfo)).ProcessID));
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
		public static void ExecuteScript(ServerProcess AProcess, string AScript)
		{
			ExecuteScript(AProcess, AScript, null, null);
		}
		
		public static void ExecuteScript(ServerProcess AProcess, string AScript, Row AInParams, Row AOutParams)
		{
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AInParams, AOutParams);
			AProcess.Context.PushWindow(0);
			try
			{
				IServerProcess LProcess = (IServerProcess)AProcess;
				IServerScript LScript = LProcess.PrepareScript(AScript);
				try
				{
					LScript.Execute(LParams);
				}
				finally
				{
					LProcess.UnprepareScript(LScript);
				}
			}
			finally
			{
				AProcess.Context.PopWindow();
			}
			SystemEvaluateNode.UpdateRowFromParams(AOutParams, LParams);
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
				ExecuteScript
				(
					AProcess.ServerSession.Processes.GetProcess(AArguments[0].Value.AsInt32),
					AArguments[1].Value.AsString,
					AArguments.Length >= 3 ? (Row)AArguments[2].Value : null,
					AArguments.Length >= 4 ? (Row)AArguments[3].Value : null
				);
			else
				ExecuteScript
				(
					AProcess,
					AArguments[0].Value.AsString,
					AArguments.Length >= 2 ? (Row)AArguments[1].Value : null,
					AArguments.Length >= 3 ? (Row)AArguments[2].Value : null
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
		public static DataParams ParamsFromRows(Row AInParams, Row AOutParams)
		{
			DataParams LParams = new DataParams();
			int LOutIndex;
			for (int LIndex = 0; LIndex < ((AInParams == null) ? 0 : AInParams.DataType.Columns.Count); LIndex++)
			{
				LOutIndex = AOutParams == null ? -1 : AOutParams.DataType.Columns.IndexOfName(AInParams.DataType.Columns[LIndex].Name);
				if (LOutIndex >= 0)
					LParams.Add(new DataParam(AInParams.DataType.Columns[LIndex].Name, AInParams.DataType.Columns[LIndex].DataType, Modifier.Var, AInParams[LIndex].Copy()));
				else
					LParams.Add(new DataParam(AInParams.DataType.Columns[LIndex].Name, AInParams.DataType.Columns[LIndex].DataType, Modifier.In, AInParams[LIndex].Copy()));
			}
			
			for (int LIndex = 0; LIndex < ((AOutParams == null) ? 0 : AOutParams.DataType.Columns.Count); LIndex++)
			{
				if (!LParams.Contains(AOutParams.DataType.Columns[LIndex].Name))
					LParams.Add(new DataParam(AOutParams.DataType.Columns[LIndex].Name, AOutParams.DataType.Columns[LIndex].DataType, Modifier.Var, AOutParams[LIndex].Copy()));
			}
			return LParams;
		}

		public static void UpdateRowFromParams(Row AOutParams, DataParams AParams)
		{
			if (AOutParams != null)
				for (int LIndex = 0; LIndex < AOutParams.DataType.Columns.Count; LIndex++)
					AOutParams[LIndex] = AParams[AParams.IndexOf(AOutParams.DataType.Columns[LIndex].Name)].Value;
		}
		
		private DataValue Evaluate(ServerProcess AProcess, string AExpression, Row AInParams, Row AOutParams)
		{
			DataParams LParams = ParamsFromRows(AInParams, AOutParams);

			AProcess.Context.PushWindow(0);
			try
			{
				IServerProcess LProcess = (IServerProcess)AProcess;
				IServerExpressionPlan LPlan = LProcess.PrepareExpression(AExpression, LParams);
				try
				{
					LPlan.CheckCompiled();
					PlanNode LNode = ((ServerExpressionPlan)LPlan).Code;
					if ((IsLiteral && !LNode.IsLiteral) || (IsFunctional && !LNode.IsFunctional) || (IsDeterministic && !LNode.IsDeterministic) || (IsRepeatable && !LNode.IsRepeatable) || (!IsNilable && LNode.IsNilable))
						throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(LNode));
					
					DataValue LResult = LPlan.Evaluate(LParams);
					UpdateRowFromParams(AOutParams, LParams);
					return LResult;
				}
				finally
				{
					LProcess.UnprepareExpression(LPlan);
				}
			}
			finally
			{
				AProcess.Context.PopWindow();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
				return new DataVar
				(
					FDataType,
					Evaluate
					(
						AProcess.ServerSession.Processes.GetProcess(AArguments[0].Value.AsInt32),
						AArguments[1].Value.AsString,
						AArguments.Length >= 3 ? (Row)AArguments[2].Value : null,
						AArguments.Length >= 4 ? (Row)AArguments[3].Value : null
					)
				);
			else
				return new DataVar
				(
					FDataType, 
					Evaluate
					(
						AProcess, 
						AArguments[0].Value.AsString, 
						AArguments.Length >= 2 ? (Row)AArguments[1].Value : null, 
						AArguments.Length >= 3 ? (Row)AArguments[2].Value : null
					)
				);
		}
	}

	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row) 
	// create operator ExecuteOn(const AServerName : System.Name, const AStatement : String, const AInParams : row, var AOutParams : row) 
	public class SystemExecuteOnNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LServerName = AArguments[0].Value.AsString;
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString);
			Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
			if (LServerLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string LStatement = AArguments[1].Value.AsString;
			
			Row LInRow = AArguments.Length >= 3 ? (Row)AArguments[2].Value : null;
			Row LOutRow = AArguments.Length >= 4 ? (Row)AArguments[3].Value : null;
			
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(LInRow, LOutRow);
			AProcess.RemoteConnect(LServerLink).Execute(LStatement, LParams);
			SystemEvaluateNode.UpdateRowFromParams(LOutRow, LParams);
			
			return null;
		}
	}
	
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row) : generic;</remarks>
	/// <remarks>operator EvaluateOn(const AServerName : System.Name, const AExpression : String, const AInParams : row, var AOutParams : row) : generic;</remarks>
	public class SystemEvaluateOnNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LServerName = AArguments[0].Value.AsString;
			Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString);
			Schema.ServerLink LServerLink = LObject as Schema.ServerLink;
			if (LServerLink == null)
				throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				
			string LExpression = AArguments[1].Value.AsString;

			Row LInRow = AArguments.Length >= 3 ? (Row)AArguments[2].Value : null;
			Row LOutRow = AArguments.Length >= 4 ? (Row)AArguments[3].Value : null;
			
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(LInRow, LOutRow);
			DataVar LResult = AProcess.RemoteConnect(LServerLink).Evaluate(LExpression, LParams);
			SystemEvaluateNode.UpdateRowFromParams(LOutRow, LParams);

			return LResult;
		}
	}
	
	#if OnExpression
	/// <remarks>operator ExecuteRemote(ADevice : System.Name, AStatement : System.String);</remarks>
	public class SystemExecuteRemoteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LServerName = AArguments[0].Value.AsString;
			Schema.ServerLink LServerLink;
			lock (AProcess.Plan)
			{
				LServerLink = (Schema.ServerLink)AProcess.Plan.Catalog[LServerName];
				AProcess.Plan.AcquireCatalogLock(LServerLink, LockMode.Shared);
			}
			string LCode = AArguments[1].Value.AsString;

			IServer LServer = ServerFactory.Connect(LServerLink.ServerURI);
			try 
			{
				IServerSession LServerSession = LServer.Connect(null);
				try 
				{
					IServerProcess LProcess = LServerSession.StartProcess();
					try
					{
						IServerStatementPlan LStatementPlan = LProcess.PrepareStatement(LCode, null);
						try
						{
							LStatementPlan.Execute(null);
						}
						finally
						{
							LProcess.UnprepareStatement(LStatementPlan);
						}
					}
					finally
					{
						LServerSession.StopProcess(LProcess);
					}
				}
				finally
				{
					LServer.Disconnect(LServerSession);
				}
			}
			finally
			{
				ServerFactory.Disconnect(LServer);
			}

			return null;
		}
	}
	#endif
	
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row);</remarks>
	/// <remarks>operator ExecuteAs(AScript : System.String, AUserID : System.UserID, APassword : System.String; const AInParams : row; var AOutParams : row);</remarks>
	public class SystemExecuteAsNode : InstructionNode
	{
		public static void ExecuteScript(ServerProcess AProcess, string AString, SessionInfo ASessionInfo)
		{
			ExecuteScript(AProcess, AString, ASessionInfo, null, null);
		}
		
		public static void ExecuteScript(ServerProcess AProcess, string AScript, SessionInfo ASessionInfo, Row AInParams, Row AOutParams)
		{
			IServerSession LSession = ((IServer)AProcess.ServerSession.Server).Connect(ASessionInfo);
			try
			{
				IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(LSession.SessionInfo));
				try
				{
					SystemExecuteNode.ExecuteScript((ServerProcess)LProcess, AScript, AInParams, AOutParams);
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			ExecuteScript
			(
				AProcess, 
				AArguments[0].Value.AsString, 
				new SessionInfo(AArguments[1].Value.AsString, AArguments[2].Value.AsString, AProcess.Plan.CurrentLibrary.Name),
				AArguments.Length >= 4 ? (Row)AArguments[3].Value : null,
				AArguments.Length >= 5 ? (Row)AArguments[4].Value : null
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
			IServerScript LScript = (IServerScript)AResult.AsyncState;
			IServerProcess LProcess = LScript.Process;
			try
			{
				try
				{
					((ServerProcess)LProcess).Context.PopWindow();
				}
				finally
				{
					LProcess.UnprepareScript(LScript);
				}
			}
			finally
			{
				if (((ServerScript)LScript).ShouldCleanupProcess)
					LProcess.Session.StopProcess(LProcess);
			}
		}
		
		private void ExecuteAsync(IServerProcess AProcess, string AScript, Row AInParams, bool AShouldCleanup)
		{
			((ServerProcess)AProcess).Context.PushWindow(0);
			IServerScript LScript = AProcess.PrepareScript(AScript);
			((ServerScript)LScript).ShouldCleanupProcess = AShouldCleanup;
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AInParams, null);
			new ExecuteDelegate(LScript.Execute).BeginInvoke(LParams, new AsyncCallback(ExecuteResults), LScript);
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
				ExecuteAsync
				(
					AProcess.ServerSession.Processes.GetProcess(AArguments[0].Value.AsInt32),
					AArguments[1].Value.AsString,
					AArguments.Length >= 3 ? (Row)AArguments[2].Value : null,
					false
				);
			else
				ExecuteAsync
				(
					((IServerSession)AProcess.ServerSession).StartProcess(new ProcessInfo(AProcess.ServerSession.SessionInfo)),
					AArguments[0].Value.AsString,
					AArguments.Length >= 2 ? (Row)AArguments[1].Value : null,
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
		
		private void ExecuteWithTimeout(IServerProcess AProcess, string AScript, int ATimeout, Row AInParams, Row AOutParams)
		{
			FDone = false;
			DataParams LParams = SystemEvaluateNode.ParamsFromRows(AInParams, AOutParams);
			((ServerProcess)AProcess).Context.PushWindow(0);
			try
			{
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
			finally
			{
				((ServerProcess)AProcess).Context.PopWindow();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{	
			if (AArguments[0].DataType.Is(AProcess.DataTypes.SystemInteger))
				ExecuteWithTimeout
				(
					AProcess.ServerSession.Processes.GetProcess(AArguments[0].Value.AsInt32),
					AArguments[1].Value.AsString,
					AArguments[2].Value.AsInt32,
					AArguments.Length >= 4 ? (Row)AArguments[3].Value : null,
					AArguments.Length >= 5 ? (Row)AArguments[4].Value : null
				);
			else
				ExecuteWithTimeout
				(
					((IServerSession)AProcess.ServerSession).StartProcess(new ProcessInfo(AProcess.ServerSession.SessionInfo)),
					AArguments[0].Value.AsString,
					AArguments[1].Value.AsInt32,
					AArguments.Length >= 3 ? (Row)AArguments[2].Value : null,
					AArguments.Length >= 4 ? (Row)AArguments[3].Value : null
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LStatement = AArguments[0].Value.AsString;
			int LNumber = AArguments[1].Value.AsInt32;
			string LUserID = AArguments.Length > 2 ? AArguments[2].Value.AsString : AProcess.ServerSession.SessionInfo.UserID;
			string LPassword = AArguments.Length > 3 ? AArguments[3].Value.AsString : AProcess.ServerSession.SessionInfo.Password;
			IServer LServer = (IServer)AProcess.ServerSession.Server;
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			string LString = AArguments[0].Value.AsString;
			DataVar LObject;
			
			AProcess.Context.PushWindow(0);
			try
			{
				IServerExpressionPlan LPlan = ((IServerProcess)AProcess).PrepareExpression(LString, null);
				try
				{
					LPlan.CheckCompiled();
					PlanNode LNode = ((ServerExpressionPlan)LPlan).Code;
					if ((IsLiteral && !LNode.IsLiteral) || (IsFunctional && !LNode.IsFunctional) || (IsDeterministic && !LNode.IsDeterministic) || (IsRepeatable && !LNode.IsRepeatable) || (!IsNilable && LNode.IsNilable))
						throw new RuntimeException(RuntimeException.Codes.InvalidCharacteristicOverride, PlanNode.CharacteristicsToString(this), PlanNode.CharacteristicsToString(LNode));
					
					LObject = LNode.Execute(AProcess);
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

			return new DataVar(String.Empty, FDataType, LObject.Value);
		}
	}

	/// <remarks>operator Sleep(AMilliseconds : integer);</remarks>
	public class SystemSleepNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			System.Threading.Thread.Sleep(AArguments[0].Value.AsInt32);
			
			return null;
		}
	}
	
	/// <remarks>operator MachineName() : String;</remarks>
	public class SystemMachineNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, System.Environment.MachineName));
		}
	}
	
	/// <remarks>operator HostName() : String;</remarks>
	public class SystemHostNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.SessionInfo.HostName));
		}
	}

    /// <remarks> operator NewGuid() : Guid </remarks>
    public class NewGuidNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Guid.NewGuid()));
		}
    }
    
	/// <remarks>operator GetDefaultDeviceName();</remarks>    
	/// <remarks>operator GetDefaultDeviceName(ALibraryName : Name);</remarks>
    public class SystemGetDefaultDeviceNameNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 0)
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.DefaultDeviceName));
			else
				return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.Plan.GetDefaultDeviceName(AArguments[0].Value.AsString, false)));
		}
    }

	/// <remarks>operator SetDefaultDeviceName(ADeviceName : Name);</remarks>    
	/// <remarks>operator SetDefaultDeviceName(ALibraryName : Name, ADeviceName : Name);</remarks>
    public class SystemSetDefaultDeviceNameNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 2)
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProcess, AArguments[0].Value.AsString, AArguments[1].Value.AsString, true);
			else
				SystemSetLibraryDescriptorNode.SetLibraryDefaultDeviceName(AProcess, AProcess.ServerSession.CurrentLibrary.Name, AArguments[0].Value.AsString, true);
			return null;
		}
    }
    
    /// <remarks>operator EnableTracing();</remarks>
    public class SystemEnableTracingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.TracingEnabled = true;
			return null;
		}
	}

    /// <remarks>operator EnableTracing();</remarks>
    public class SystemDisableTracingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.TracingEnabled = false;
			return null;
		}
	}

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemEnableErrorLoggingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.LogErrors = true;
			return null;
		}
	}

    /// <remarks>operator EnableErrorLogging();</remarks>
    public class SystemDisableErrorLoggingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.LogErrors = false;
			return null;
		}
	}

	/// <remarks>operator EnableSessionTracing();</remarks>    
    public class SystemEnableSessionTracingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.SessionTracingEnabled = true;
			return null;
		}
    }

	/// <remarks>operator DisableSessionTracing();</remarks>    
    public class SystemDisableSessionTracingNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.SessionTracingEnabled = false;
			return null;
		}
    }
    
    /// <remarks>operator System.EncryptPassword(const AString : System.String) : System.String;</remarks>
    public class SystemEncryptPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, Schema.SecurityUtility.EncryptPassword(AArguments[0].Value.AsString)));
		}
	}
    
	#if OnExpression
    /// <remarks>operator CreateServerLinkUser(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				Schema.User LUser = AProcess.Plan.Catalog.Users[AArguments[0].Value.AsString];
				if (String.Compare(LUser.ID, AProcess.Plan.User.ID, true) != 0)
					AProcess.Plan.CheckAdminUser();
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				LServerLink.Users.Add(new Schema.ServerLinkUser(LUser, LServerLink, AArguments[2].Value).ToString(), Schema.SecurityUtility.EncryptPassword(((Scalar)AArguments[3].Value.AsString)));
				return null;
			}
		}
    }
    
    /// <remarks>operator CreateServerLinkUserWithEncryptedPassword(AUserID : string, AServerLinkName : System.Name, AServerUserID : string, AEncryptedServerPassword : string); </remarks>
    public class SystemCreateServerLinkUserWithEncryptedPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				AProcess.Plan.CheckSystemUser();
				Schema.User LUser = AProcess.Plan.Catalog.Users[AArguments[0].Value.AsString];
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				LServerLink.Users.Add(new Schema.ServerLinkUser(LUser, LServerLink, AArguments[2].Value).ToString(), ((Scalar)AArguments[3].Value.AsString));
				return null;
			}
		}
    }
    
    /// <remarks>operator SetServerLinkUserID(AUserID : string, AServerLinkName : System.Name, AServerUserID : string); </remarks>
    public class SystemSetServerLinkUserIDNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				Schema.User LUser = AProcess.Plan.Catalog.Users[AArguments[0].Value.AsString];
				if (String.Compare(LUser.ID, AProcess.Plan.User.ID, true) != 0)
					AProcess.Plan.CheckAdminUser();
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				LServerLink.Users[LUser.ID].ServerLinkUserID = AArguments[2].Value.AsString;
				return null;
			}
		}
    }
    
    /// <remarks>operator SetServerLinkUserPassword(AUserID : string, AServerLinkName : System.Name, APassword : string); </remarks>
    public class SystemSetServerLinkUserPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				Schema.User LUser = AProcess.Plan.Catalog.Users[AArguments[0].Value.AsString];
				AProcess.Plan.CheckAdminUser();
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				LServerLink.Users[LUser.ID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString);
				return null;
			}
		}
    }
    
    /// <remarks>operator ChangeServerLinkUserPassword(AServerLinkName : System.Name, AOldPassword : string, APassword : string); </remarks>
    public class ChangeServerLinkUserPasswordNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				Schema.User LUser = AProcess.ServerSession.User;
				if (String.Compare(AArguments[1].Value.AsString, Schema.SecurityUtility.DecryptPassword(LServerLink.Users[LUser.ID].ServerLinkPassword), true) != 0)
					throw new ServerException(ServerException.Codes.InvalidPassword);
				LServerLink.Users[LUser.ID].ServerLinkPassword = Schema.SecurityUtility.EncryptPassword(AArguments[2].Value.AsString);
				return null;
			}
		}
    }
    
    /// <remarks>operator DropServerLinkUser(AUserID : string, AServerLinkName : System.Name); </remarks>
    public class SystemDropServerLinkUserNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			lock (AProcess.Plan.Catalog.Users)
			{
				Schema.User LUser = AProcess.Plan.Catalog.Users[AArguments[0].Value.AsString];
				if (String.Compare(LUser.ID, AProcess.Plan.User.ID, true) != 0)
					AProcess.Plan.CheckAdminUser();
				Schema.Object LObject = Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[1].Value.AsString);
				if (!(LObject is Schema.ServerLink))
					throw new CompilerException(CompilerException.Codes.ServerLinkIdentifierExpected);
				Schema.ServerLink LServerLink = (Schema.ServerLink)LObject;
				
				foreach (ServerSession LSession in AProcess.ServerSession.Server.Sessions)
					if (String.Compare(LSession.User.ID, LUser.ID, true) == 0)
						foreach (RemoteSession LRemoteSession in LSession.RemoteSessions)
							if (LRemoteSession.ServerLink.Equals(LServerLink))
								throw new ServerException(ServerException.Codes.UserHasOpenSessions, LUser.ID);
				
				LServerLink.Users.Remove(LServerLink.Users[LUser.ID]);
				return null;
			}
		}
    }
	#endif
	
    public class SystemStopProcessNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// Only the Admin and System user can stop a process that was started on a different session
			if (AProcess.ServerSession.User.IsAdminUser())
				AProcess.ServerSession.Server.StopProcess(AArguments[0].Value.AsInt32);
			else
				AProcess.ServerSession.StopProcess(AArguments[0].Value.AsInt32);
			return null;
		}
    }

    public class SystemCloseSessionNode : InstructionNode
    {
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.CloseSession(AArguments[0].Value.AsInt32);
			return null;
		}
	}
	
	public class SystemBeginTransactionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AArguments.Length == 0)
				AProcess.BeginTransaction(AProcess.DefaultIsolationLevel);
			else
				AProcess.BeginTransaction((IsolationLevel)Enum.Parse(typeof(IsolationLevel), AArguments[0].Value.AsString, true));
			return null;
		}
	}
	
	public class SystemPrepareTransactionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.PrepareTransaction();
			return null;
		}
	}
	
	public class SystemCommitTransactionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.CommitTransaction();
			return null;
		}
	}
	
	public class SystemRollbackTransactionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.RollbackTransaction();
			return null;
		}
	}
	
	public class SystemTransactionCountNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.TransactionCount));
		}
	}
	
	public class SystemInTransactionNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.InTransaction));
		}
	}
	
	public class SystemCollectNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			GC.Collect();
			return null;
		}
	}
	
	// operator ServerName() : System.Name;
	public class SystemServerNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.Name));
		}
	}
	
	// operator UserID() : string;
	public class SystemUserIDNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.User.ID));
		}
	}
	
	// operator UserName() : string;
	public class SystemUserNameNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.User.Name));
		}
	}

	// operator SessionID() : integer;	
	public class SystemSessionIDNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.SessionID));
		}
	}
	
	// operator ProcessID() : integer;
	public class SystemProcessIDNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ProcessID));
		}
	}
	
	// operator System.SetMaxConcurrentProcesses(const AMaxConcurrentProcesses : System.Integer);
	public class SystemSetMaxConcurrentProcessesNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.MaxConcurrentProcesses = AArguments[0].Value.AsInt32;
			AProcess.CatalogDeviceSession.SaveServerSettings(AProcess.ServerSession.Server);
			return null;
		}
	}

	// operator System.SetProcessWaitTimeout(const AProcessWaitTimeout : System.TimeSpan);
	public class SystemSetProcessWaitTimeoutNode: InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.ProcessWaitTimeout = AArguments[0].Value.AsTimeSpan;
			AProcess.CatalogDeviceSession.SaveServerSettings(AProcess.ServerSession.Server);
			return null;
		}
	}
	
	// operator SetLanguage(ALanguage : string);
	public class SystemSetLanguageNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.Language = (QueryLanguage)Enum.Parse(typeof(QueryLanguage), AArguments[0].Value.AsString, true);
			return null;
		}
	}
	
	// operator SetDefaultIsolationLevel(const AIsolationLevel : String);
	public class SystemSetDefaultIsolationLevelNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), AArguments[0].Value.AsString, true);
			return null;
		}
	}

	// operator SetIsolationLevel(const AIsolationLevel : String);
	public class SystemSetIsolationLevelNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.DefaultIsolationLevel = (IsolationLevel)Enum.Parse(typeof(IsolationLevel), AArguments[0].Value.AsString, true);
			return null;
		}
	}

	// operator SetDefaultMaxStackDepth(const ADefaultMaxStackDepth : Integer);
	public class SystemSetDefaultMaxStackDepthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.DefaultMaxStackDepth = AArguments[0].Value.AsInt32;
			return null;
		}
	}

	// operator SetDefaultMaxCallDepth(const ADefaultMaxCallDepth : Integer);
	public class SystemSetDefaultMaxCallDepthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.DefaultMaxCallDepth = AArguments[0].Value.AsInt32;
			return null;
		}
	}

	// operator SetMaxStackDepth(const AMaxStackDepth : Integer);
	public class SystemSetMaxStackDepthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.Context.MaxStackDepth = AArguments[0].Value.AsInt32;
			return null;
		}
	}
	
	// operator SetMaxCallDepth(const AMaxCallDepth : Integer);
	public class SystemSetMaxCallDepthNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.Context.MaxCallDepth = AArguments[0].Value.AsInt32;
			return null;
		}
	}
	
	// operator SetDefaultUseImplicitTransactions(const ADefaultUseImplicitTransactions : Boolean);
	public class SystemSetDefaultUseImplicitTransactionsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.SessionInfo.DefaultUseImplicitTransactions = AArguments[0].Value.AsBoolean;
			return null;
		}
	}
	
	// operator SetUseImplicitTransactions(const AUseImplicitTransactions : Boolean);
	public class SystemSetUseImplicitTransactionsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.UseImplicitTransactions = AArguments[0].Value.AsBoolean;
			return null;
		}
	}
	
	// operator PushNonLoggedContext()
	public class SystemPushNonLoggedContextNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.NonLogged = true;
			return null;
		}
	}

	// operator PopNonLoggedContext()
	public class SystemPopNonLoggedContextNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.NonLogged = false;
			return null;
		}
	}
	
	// operator IsNonLoggedContext() : Boolean
	public class SystemIsNonLoggedContextNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.IScalarType)FDataType, AProcess.NonLogged));
		}
	}

	// operator DisableReconciliation()
	public class SystemDisableReconciliationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.DisableReconciliation();
			return null;
		}
	}

	// operator EnableReconciliation()
	public class SystemEnableReconciliationNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.EnableReconciliation();
			return null;
		}
	}
	
	// operator IsReconciliationEnabled() : Boolean
	public class SystemIsReconciliationEnabledNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.IScalarType)FDataType, AProcess.IsReconciliationEnabled()));
		}
	}

	// operator System.Diagnostics.TraceOn(ATraceCode : string);
	public class SystemTraceOnNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if TRACEEVENTS
			AProcess.ServerSession.Server.TraceOn(AArguments[0].Value.AsString);
			#endif
			return null;
		}
	}

	// operator System.Diagnostics.TraceOff(ATraceCode : string);
	public class SystemTraceOffNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if TRACEEVENTS
			AProcess.ServerSession.Server.TraceOff(AArguments[0].Value.AsString);
			#endif
			return null;
		}
	}

	// operator System.Diagnostics.Tracing(ATraceCode : string) : boolean;
	public class SystemTracingNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if TRACEEVENTS
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.Tracing(AArguments[0].Value.AsString)));
			#else
			return new DataVar(FDataType, null);
			#endif
		}
	}
	
	// operator System.Diagnostics.LogError(const AError : Error);
	public class SystemLogErrorNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.LogError((Exception)AArguments[0].Value.AsNative);
			return null;
		}
	}
	
	// operator System.Diagnostics.LogMessage(const AMessage : String);
	public class SystemLogMessageNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.LogMessage(AArguments[0].Value.AsString);
			return null;
		}
	}
	
	// operator System.Diagnostics.ShowLog() : String
	// operator System.Diagnostics.ShowLog(const ALogIndex : Integer) : String
	public class SystemShowLogNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(DataType, new Scalar(AProcess, (Schema.ScalarType)DataType, AProcess.ServerSession.Server.ShowLog(AArguments.Length == 0 ? 0 : AArguments[0].Value.AsInt32)));
		}
	}
	
	// operator System.Diagnostics.ListLogs() : table { Sequence : Integer, LogName : String }
    public class SystemListLogsNode : TableNode
    {
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			
			DataType.Columns.Add(new Schema.Column("Sequence", APlan.Catalog.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("LogName", APlan.Catalog.DataTypes.SystemString));
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
					StringCollection LLogs = AProcess.ServerSession.Server.ListLogs();
					for (int LIndex = 0; LIndex < LLogs.Count; LIndex++)
					{
						LRow[0].AsInt32 = LIndex;
						LRow[1].AsString = LLogs[LIndex];
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

		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LObject = Nodes[0].Execute(AProcess);
			if ((LObject.Value != null) && !LObject.Value.DataType.Is(FDataType))
				throw new RuntimeException(RuntimeException.Codes.InvalidCast, LObject.Value.DataType.Name, FDataType.Name);
			return new DataVar(FDataType, LObject.Value);
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
			FDataType = APlan.Catalog.DataTypes.SystemBoolean;
		}
		
		public override void DetermineCharacteristics(Plan APlan)
		{
			IsLiteral = Nodes[0].IsLiteral;
			IsFunctional = Nodes[0].IsFunctional;
			IsDeterministic = Nodes[0].IsDeterministic;
			IsRepeatable = Nodes[0].IsRepeatable;
			IsNilable = false;
		}

		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			DataVar LObject = Nodes[0].Execute(AProcess);
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LObject.Value.DataType.Is(FTargetType)));
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
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			DateTime LStartTime = DateTime.Now;

			int LCount = AArguments[0].Value.AsInt32;
			StreamID[] LStreamIDs = null;
			bool LStepped = AArguments[1].Value.AsBoolean;
			if (LStepped)
				LStreamIDs = new StreamID[LCount];

			if (LStepped)				
			{
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					LStreamIDs[LIndex] = AProcess.StreamManager.Allocate();
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					AProcess.StreamManager.Deallocate(LStreamIDs[LIndex]);
			}
			else
				for (int LIndex = 0; LIndex < LCount; LIndex++)
					AProcess.StreamManager.Deallocate(AProcess.StreamManager.Allocate());
			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Now.Subtract(LStartTime)));
		}
	}

	public class SystemBenchmarkParserNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			DateTime LStartTime = DateTime.Now;

			Statement LStatement = new Parser().ParseScript(AArguments[0].Value.AsString, null);
						
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, DateTime.Now.Subtract(LStartTime)));
		}
	}

	public class SystemBenchmarkCompilerNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long LAccumulator = 0;
			#endif
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					PlanNode LNode = Compiler.Compile(LPlan.Plan, new Parser().ParseScript(AArguments[0].Value.AsString, null));
					#if ACCUMULATOR
					LAccumulator = LPlan.Plan.Accumulator;
					#endif
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
			}
			finally
			{
				LPlan.Dispose();
			}

			#if ACCUMULATOR			
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks - LAccumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond))));
			#else
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond))));
			#endif			
		}
	}
	
	public class SystemBenchmarkBindingNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			#if ACCUMULATOR
			long LAccumulator = 0;
			#endif
			ServerStatementPlan LPlan = new ServerStatementPlan(AProcess);
			try
			{
				AProcess.PushExecutingPlan(LPlan);
				try
				{
					PlanNode LNode = Compiler.Compile(LPlan.Plan, new Parser().ParseScript(AArguments[0].Value.AsString, null));
					LNode = Compiler.OptimizeNode(LPlan.Plan, LNode);
					LNode = Compiler.BindNode(LPlan.Plan, LNode);
					#if ACCUMULATOR
					LAccumulator = LPlan.Plan.Accumulator;
					#endif
				}
				finally
				{
					AProcess.PopExecutingPlan(LPlan);
				}
			}
			finally
			{
				LPlan.Dispose();
			}

			#if ACCUMULATOR						
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks - LAccumulator)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond))));
			#else
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond))));
			#endif
		}
	}
	
	public class SystemBenchmarkExecuteNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			long LStartTicks = TimingUtility.CurrentTicks;
			SystemExecuteNode.ExecuteScript(AProcess, AArguments[0].Value.AsString);
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, new TimeSpan((long)((((double)(TimingUtility.CurrentTicks - LStartTicks)) / TimingUtility.TicksPerSecond) * TimeSpan.TicksPerSecond))));
		}
	}
	
	public class SystemLoadStringFromFileNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			using (System.IO.FileStream LStream = new System.IO.FileStream(AArguments[0].Value.AsString, System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				using (System.IO.StreamReader LReader = new System.IO.StreamReader(LStream))
				{
					return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, LReader.ReadToEnd()));
				}
			}
		}
	}
	
	public class SystemStreamCountNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.StreamManager.Count()));
		}
	}
	
	public class SystemLockCountNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.LockManager.Count()));
		}
	}
	
	public class SystemRowCountNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if USEROWMANAGER
			return new DataVar(FDataType, new Scalar(AProcess, FDataType, AProcess.RowManager.RowCount));
			#else
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, 0));			
			#endif
		}
	}
	
	public class SystemUsedRowCountNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			#if USEROWMANAGER
			return new DataVar(FDataType, new Scalar(AProcess, FDataType, AProcess.RowManager.UsedRowCount));
			#else
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, 0));
			#endif
		}
	}
	
	public class SystemSetLibraryNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.CurrentLibrary = AProcess.CatalogDeviceSession.ResolveLoadedLibrary(AArguments[0].Value.AsString);
			return null;
		}
	}
	
	// create operator System.StreamsOpen() : System.String class "System.SystemStreamsOpenNode";
	public class SystemStreamsOpenNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.StreamManager.StreamOpensAsString()));
		}
	}

	// create operator System.LockEvents() : System.String class "System.SystemLockEventsNode";
	public class SystemLockEventsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			return new DataVar(FDataType, new Scalar(AProcess, (Schema.ScalarType)FDataType, AProcess.ServerSession.Server.LockManager.LockEventsAsString()));
		}
	}
	
	// create operator ClearOperatorResolutionCache();
	public class SystemClearOperatorResolutionCacheNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
			AProcess.Plan.Catalog.OperatorResolutionCache.Clear();
			return null;
		}
	}

	// create operator ClearConversionPathCache();
	public class SystemClearConversionPathCacheNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.User.ID != AProcess.ServerSession.Server.AdminUser.ID)
				throw new ServerException(ServerException.Codes.UnauthorizedUser, ErrorSeverity.Environment, AProcess.ServerSession.User.ID);
			AProcess.Plan.Catalog.ConversionPathCache.Clear();
			return null;
		}
	}
}
