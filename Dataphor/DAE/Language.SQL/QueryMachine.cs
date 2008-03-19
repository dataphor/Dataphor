/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language.SQL
{
    using System;
    using System.Text;
    
    using Alphora.Dataphor;
    using Alphora.Dataphor.DAE.Language;
    
    public abstract class QueryMachine : SimpleMachine
    {   
        public override bool IsLogicalOperator(string AInstruction)
        {
            return
                base.IsLogicalOperator(AInstruction) || 
                (String.Compare(AInstruction, "iIn", true) == 0) ||
                (String.Compare(AInstruction, "iExists", true) == 0);
        }
        
        public override bool IsEquivalenceOperator(string AInstruction)
        {
            return base.IsEquivalenceOperator(AInstruction) || IsNullEquivalenceOperator(AInstruction);
        }
        
        public override bool IsStringComparisonOperator(string AInstruction)
        {
            return base.IsStringComparisonOperator(AInstruction) || (String.Compare(AInstruction, "iLike", true) == 0);
        }
        
        public virtual bool IsNullEquivalenceOperator(string AInstruction)
        {
            return (String.Compare(AInstruction, "iIsNull", true) == 0) || (String.Compare(AInstruction, "iIsNotNull", true) == 0);
        }

        /// <returns>
        ///     True if the given operator is normally a boolean valued operator, but must compensate
        ///     for the possibility of the unknown truth value when nulls are present.
        /// </returns>        
        public virtual bool IsThreeValuedOperator(string AInstruction)
        {
            return
                (String.Compare(AInstruction, "iAnd", true) == 0) ||
                (String.Compare(AInstruction, "iOr", true) == 0) ||
                (String.Compare(AInstruction, "iXor", true) == 0) ||
                (String.Compare(AInstruction, "iNot", true) == 0) ||
                (String.Compare(AInstruction, "iEqual", true) == 0) ||
                (String.Compare(AInstruction, "iNotEqual", true) == 0) ||
                (String.Compare(AInstruction, "iLess", true) == 0) ||
                (String.Compare(AInstruction, "iInclusiveLess", true) == 0) ||
                (String.Compare(AInstruction, "iGreater", true) == 0) ||
                (String.Compare(AInstruction, "iInclusiveGreater", true) == 0) ||
                (String.Compare(AInstruction, "iLike", true) == 0);
        }
        
        public abstract void iLike();
        public abstract void iIn();
        public abstract void iIsNull(); // !!! SQLs is null not MSSQLs IsNull() 
        public abstract void iIsNotNull();
        public abstract void iNullValue(); // !!! MSSQLs IsNull() 
        public abstract void iExists();
        public abstract void iSelect();
        public abstract void iInsert();
        public abstract void iUpdate();
        public abstract void iDelete();
        public abstract void iFilter();
        public abstract void iInnerJoin();
        public abstract void iLeftOuterJoin();
        public abstract void iRightOuterJoin();
        public abstract void iFullOuterJoin();
        public abstract void iCrossJoin();
        public abstract void iGroup();
        public abstract void iAddTable();
        public abstract void iGetTable();
        public abstract void iGetValues();
        public abstract void iGetField();
        public abstract void iBeginSelect();
        public abstract void iEndSelect();
        public abstract void iSum();
        public abstract void iCount();
        public abstract void iAvg();
        public abstract void iMin();
        public abstract void iMax();
        public abstract void iDistinct();
        public abstract void iUnion();
        public abstract void iOrder();
        public abstract void iGetValue();
        public abstract void iSetValue();
        public abstract void iClearValue();
        public abstract void iGetRecordValue();
        public abstract void iSetRecord();
        public abstract void iClearRecord();
        public abstract void iCreateTable();
        public abstract void iAlterTable();
        public abstract void iDropTable();
        public abstract void iCreateIndex();
        public abstract void iDropIndex();
        public abstract void iBatch();
        public abstract void iCase();
        public abstract void iCall();
    }
    
    public abstract class AnsiNullMachine : QueryMachine
    {
        public bool IsNull(object AValue)
        {
            return (AValue == null) || (AValue == DBNull.Value);
        }
        
        public override object ArithmeticOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            if (IsNull(ALeftObject) || IsNull(ARightObject))
                return DBNull.Value;
            else
                return base.ArithmeticOperator(ARightObject, ALeftObject, AInstruction);
        }
        
        public override object BinaryLogicalOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            if (IsNull(ALeftObject) || IsNull(ARightObject))
                return DBNull.Value;
            else
                return base.BinaryLogicalOperator(ARightObject, ALeftObject, AInstruction);
        }
        
        public override object UnaryLogicalOperator(object AObject, string AInstruction)
        {
            if (IsNull(AObject))
                return DBNull.Value;
            else
                return base.UnaryLogicalOperator(AObject, AInstruction);
        }
        
        public override object ComparisonOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            if (IsNull(ALeftObject) || IsNull(ARightObject))
                return DBNull.Value;
            else
                return base.ComparisonOperator(ARightObject, ALeftObject, AInstruction);
        }

        public override void iIsNull()
        {
            Push(IsNull(Pop()));
        }
        
        public override void iIsNotNull()
        {
            Push(!(IsNull(Pop())));
        }
        
        public override void iNullValue()
        {
            object LNullValue = Pop();
            object LValue = Pop();
            Push((IsNull(LValue) ? LNullValue : LValue));
        }
        
        public override void iIn()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iIn");
        }
        
        public override void iExists()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iExists");
        }
        
        public override void iLike()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iLike");
        }
        
        public override void iSelect()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iSelect");
        }
        
        public override void iInsert()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iInsert");
        }
        
        public override void iUpdate()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iUpdate");
        }
        
        public override void iDelete()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iDelete"); 
        }
        
        public override void iFilter()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iFilter");
        }
        
        public override void iInnerJoin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iInnerJoin");
        }
        
        public override void iLeftOuterJoin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iLeftOuterJoin");
        }
        
        public override void iRightOuterJoin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iRightOuterJoin");
        }
        
        public override void iFullOuterJoin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iFullOuterJoin");
        }
        
        public override void iCrossJoin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iCrossJoin");
        }
        
        public override void iGroup()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGroup");
        }
        
        public override void iAddTable()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iAddTable");
        }
        
        public override void iGetTable()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGetTable");
        }
        
        public override void iGetValues()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGetValues");
        }
        
        public override void iGetField()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGetField");
        }
        
        public override void iBeginSelect()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iBeginSelect");
        }
        
        public override void iEndSelect()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iEndSelect");
        }
        
        public override void iSum()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iSum");
        }
        
        public override void iCount()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iCount");
        }
        
        public override void iAvg()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iAvg");
        }
        
        public override void iMin()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iMin");
        }
        
        public override void iMax()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iMax");
        }
        
        public override void iDistinct()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iDistinct");
        }
        
        public override void iUnion()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iUnion");
        }
        
        public override void iOrder()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iOrder");
        }
        
        public override void iGetValue()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGetValue");
        }
        
        public override void iSetValue()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iSetValue");
        }
        
        public override void iClearValue()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iClearValue");
        }
        
        public override void iGetRecordValue()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iGetRecordValue");
        }
        
        public override void iSetRecord()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iSetRecord");
        }
        
        public override void iClearRecord()
        {
            throw new MachineException(MachineException.Codes.InvalidInstruction, "iClearRecord");
        }
    }
    
    public class SQLMachine : QueryMachine
    {		
		protected string BuildBinaryExpression(string ALeftString, string AInstruction, string ARightString)
		{
			return String.Format("{0} {1} {2}", ALeftString, AInstruction, ARightString);
		}
		
		protected int FSelectDepth = 0;
		
		public override void iBitwiseNot()
		{
			Push(Keywords.BitwiseNot + Pop().ToString());
		}
		
		public override void iBitwiseAnd()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.BitwiseAnd, LRightString));
		}
		
		public override void iBitwiseOr()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.BitwiseOr, LRightString));
		}
		
		public override void iBitwiseXor()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.BitwiseXor, LRightString));
		}
		
		public override void iLeftShift()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.ShiftLeft, LRightString));
		}
		
		public override void iRightShift()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.ShiftRight, LRightString));
		}
		
		public override void iPower()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(String.Format("Power({0}, {1})", LLeftString, LRightString));
		}
		
		public override void iAnd()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.And, LRightString));
		}
		
		public override void iDivision()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Division, LRightString));
		}
		
		public override void iEqual()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Equal, LRightString));
		}
		
		public override void iGreater()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Greater, LRightString));
		}
 
 		public override void iInclusiveGreater()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.InclusiveGreater, LRightString));
		}

		public override void iLess()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Less, LRightString));
		}

		public override void iInclusiveLess()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.InclusiveLess, LRightString));
		}

		public override void iModulus()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Modulus, LRightString));
		}

		public override void iMultiplication()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Multiplication, LRightString));
		}
		
		public override void iNot()
		{
			Push(String.Format("{0} {1}{2}{3}", new object[]{Keywords.Not, Keywords.BeginGroup, Pop().ToString(), Keywords.EndGroup}));
		}
		
		public override void iNotEqual()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.NotEqual, LRightString));
		}

		public override void iOr()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Or, LRightString));
		}

		public override void iSubtraction()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Subtraction, LRightString));
		}

		public override void iAddition()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Addition, LRightString));
		}

		public override void iXor()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Xor, LRightString));
		}

		public override void iIsNull()
		{
			Push(String.Format("{0} is null", Pop().ToString()));
		}
		
		public override void iIsNotNull()
		{
		    Push(String.Format("{0} is not null", Pop().ToString()));
		}
		
		public override void iNullValue()
		{
		    string LNullValue = Pop().ToString();
		    string LValue = Pop().ToString();
		    Push(String.Format("IsNull({0}, {1})", LValue, LNullValue));
		}
		
		public override void iExists()
		{
			Push(String.Format("exists ({0})", Pop().ToString()));
		}
		
		public override void iIn()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.In, LRightString));
		}

		public override void iLike()
		{
			string LRightString = Pop().ToString();
			string LLeftString = Pop().ToString();
			Push(BuildBinaryExpression(LLeftString, Keywords.Like, LRightString));
		}
		
		public override void iBeginSelect()
		{
			FSelectDepth++;
		}
		
		public override void iEndSelect()
		{
			FSelectDepth--;
			if (FSelectDepth > 0)
    			Push(String.Format("{0}{1}{2}", Keywords.BeginGroup, Pop().ToString(), Keywords.EndGroup));
		}
		
		public override void iAddTable()
		{
			string LTableAlias = Pop().ToString();
			string LTableExpression = Pop().ToString();
			Push(String.Format("{0} as {1}", LTableExpression, LTableAlias));
		}
		
		public override void iGetTable()
		{
			string LOptimizerHints = Pop().ToString();
			string LTableName = Pop().ToString();
			Push(String.Format("{0} {1}", LTableName, LOptimizerHints));
		}
		
		public override void iGetField()
		{
			Expression LExpression = (Expression)Pop();
			if (LExpression is QualifiedFieldExpression)
			{
				string LField = ((QualifiedFieldExpression)LExpression).FieldName;
				if (((QualifiedFieldExpression)LExpression).TableAlias != String.Empty)
				    LField = ((QualifiedFieldExpression)LExpression).TableAlias + Keywords.Qualifier + LField;
				
//				if (LExpression is AggregateFieldExpression)
//				{
//					if (((AggregateFieldExpression)LExpression).AggregationType != AggregationType.None)
//					{
//						if (((AggregateFieldExpression)LExpression).Distinct)
//							LField = Keywords.Distinct + " " + LField;
//						LField = 
//							String.Format
//							(
//								"{0}{1}{2}{3}", 
//								new object[]
//								{
//									Enum.GetName(typeof(AggregationType), ((AggregateFieldExpression)LExpression).AggregationType).ToLower(), 
//									Keywords.BeginGroup, 
//									LField, 
//									Keywords.EndGroup
//								}
//							);
//					}
//				}
				Push(LField);
			}
			else
				throw new LanguageException(LanguageException.Codes.InvalidOperand, LExpression.GetType().Name);
		}

		public override void iSelect()
		{
		    // Pop the expression
		    string LSet = Pop().ToString();
		    
		    // Pop the select clause
		    SelectClause LSelectClause = (SelectClause)Pop();
		    
		    // Build the select list
		    string LSelectList = String.Empty;
		    if (LSelectClause.Columns.Count > 0)
		    {
		        string LField;
		        foreach (ColumnExpression LColumn in LSelectClause.Columns)
		        {
		            LColumn.Expression.Process(this);
		            LField = Pop().ToString();
		            
		            // TODO: figure out how to do null casting in this context

                    if (LColumn.ColumnAlias != String.Empty)
                        LField = String.Format("{0} as {1}", LField, LColumn.ColumnAlias);

		            LSelectList =
		                LSelectList + (LSelectList == String.Empty ? String.Empty : Keywords.ListSeparator + " ") + LField;
		        }
		    }
		    else
		        LSelectList = "*";
		        
		    if (LSelectClause.Distinct)
		        LSelectList = String.Format("{0} {1}", Keywords.Distinct, LSelectList);
		    
		    // Push the expression
		    Push(String.Format("{0} {1} {2} {3}", new object[]{Keywords.Select, LSelectList, "from", LSet}));
		}
		
		public override void iGroup()
		{
		    // Pop the group clause
		    GroupClause LGroupClause = (GroupClause)Pop();
		    
		    // Pop the expression
		    string LSet = Pop().ToString();
		    
		    string LGroupList = String.Empty;
		    foreach (QualifiedFieldExpression LColumn in LGroupClause.Columns)
		    {
		        LColumn.Process(this);
		        LGroupList = LGroupList + (LGroupList == String.Empty ? String.Empty : Keywords.ListSeparator + " ") + Pop().ToString();
		    }
		    
		    Push(String.Format("{0} group by {1}", LSet, LGroupList));
		}
		
		public override void iGetValues()
		{
			ValuesExpression LValues = (ValuesExpression)Pop();
			StringBuilder LResult = new StringBuilder();
			bool LFirst = true;
			foreach (Expression LExpression in LValues.Expressions)
			{
				LExpression.Process(this);
				
				if (!LFirst)
					LResult.Append(", ");
				else
					LFirst = false;
					
				LResult.Append(Pop().ToString());
			}
			
			Push(String.Format("values({0})", LResult.ToString()));
		}
		
		public override void iInsert()
		{
		    // insert into <table name>(<field name commalist>) <expression>
		    string LExpression = Pop().ToString();
		    InsertClause LInsertClause = (InsertClause)Pop();
		    StringBuilder LFieldList = new StringBuilder();
		    bool LFirst = true;
		    foreach (InsertFieldExpression LField in LInsertClause.Columns)
		    {
				if (!LFirst)
					LFieldList.Append(", ");
				else
					LFirst = false;
					
				LFieldList.Append(LField.FieldName);
		    }
		    Push(String.Format("insert into {0}({1}) {2}", LInsertClause.TableExpression.TableName, LFieldList.ToString(), LExpression));
		}
		
		public override void iUpdate()
		{
		    // update TableName set <update field commalist> where <expression>
		    string LFromClause = Pop().ToString();
		    UpdateClause LUpdateClause = (UpdateClause)Pop();
		    StringBuilder LFieldList = new StringBuilder();
		    bool LFirst = true;
		    foreach (UpdateFieldExpression LField in LUpdateClause.Columns)
		    {
				if (!LFirst)
					LFieldList.Append(", ");
				else
					LFirst = false;
					
				LFieldList.Append(LField.FieldName);
				LFieldList.Append(" = ");
				
				LField.Expression.Process(this);
				LFieldList.Append(Pop().ToString());
		    }
		    Push(String.Format("update {0} set {1} from {2}", LUpdateClause.TableAlias, LFieldList.ToString(), LFromClause));
		}
		
		public override void iDelete()
		{
			// delete from TableName [where <expression>]
			string LFromClause = Pop().ToString();
			DeleteClause LDeleteClause = (DeleteClause)Pop();
			Push(String.Format("delete from {0} from {1}", LDeleteClause.TableAlias, LFromClause));
		}
		
		protected string BuildJoinExpression(string ALeftTable, string AJoinOperator, string ARightTable, string AExpression)
		{
		    string LResult = String.Format("{0} {1} {2}", ALeftTable, AJoinOperator, ARightTable);
		    if (AExpression != string.Empty)
		        LResult = string.Format("{0} on {1}", LResult, AExpression);
		    return LResult;
		}
		
		public override void iCrossJoin()
		{
		    string LRightTable = Pop().ToString();
		    string LLeftTable = Pop().ToString();
		    Push(BuildJoinExpression(LLeftTable, "cross join", LRightTable, String.Empty));
		}
		
		public override void iInnerJoin()
		{
		    Expression LExpression = (Expression)Pop();
		    LExpression.Process(this);
		    string LExpressionString = Pop().ToString();
		    string LRightTable = Pop().ToString();
		    string LLeftTable = Pop().ToString();
		    Push(BuildJoinExpression(LLeftTable, "inner join", LRightTable, LExpressionString));
		}
		
		public override void iLeftOuterJoin()
		{
		    Expression LExpression = (Expression)Pop();
		    LExpression.Process(this);
		    string LExpressionString = Pop().ToString();
		    string LRightTable = Pop().ToString();
		    string LLeftTable = Pop().ToString();
		    Push(BuildJoinExpression(LLeftTable, "left outer join", LRightTable, LExpressionString));
		}

		public override void iRightOuterJoin()
		{
		    Expression LExpression = (Expression)Pop();
		    LExpression.Process(this);
		    string LExpressionString = Pop().ToString();
		    string LRightTable = Pop().ToString();
		    string LLeftTable = Pop().ToString();
		    Push(BuildJoinExpression(LLeftTable, "right outer join", LRightTable, LExpressionString));
		}

		public override void iFullOuterJoin()
		{
		    Expression LExpression = (Expression)Pop();
		    LExpression.Process(this);
		    string LExpressionString = Pop().ToString();
		    string LRightTable = Pop().ToString();
		    string LLeftTable = Pop().ToString();
		    Push(BuildJoinExpression(LLeftTable, "full outer join", LRightTable, LExpressionString));
		}
		
		public override void iFilter()
		{
		    FilterClause LClause = (FilterClause)Pop();
		    string LSet = Pop().ToString();
		    LClause.Expression.Process(this);
		    string LExpression = Pop().ToString();
		    Push
		    (
		        String.Format
		        (
		            "{0} {1} {2}", 
		            LSet, 
		            (LClause is WhereClause ? "where" : "having"), 
		            LExpression
		        )
		    );
		}
		
		public override void iOrder()
		{
		    OrderClause LClause = (OrderClause)Pop();
		    string LSet = Pop().ToString();
		    string LOrderList = string.Empty;
		    foreach (OrderFieldExpression LColumn in LClause.Columns)
		        LOrderList =
		            LOrderList + (LOrderList == String.Empty ? String.Empty : Keywords.ListSeparator + " ") +
		            String.Format("{0} {1}", LColumn.TableAlias == String.Empty ? LColumn.FieldName : LColumn.TableAlias + "." + LColumn.FieldName, LColumn.Ascending ? "asc" : "desc");
		    Push(String.Format("{0} order by {1}", LSet, LOrderList));
		}
		
		public override void iDistinct()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedDistinct);
		}
		
		public override void iUnion()
		{
		    bool LDistinct = (bool)Pop();
		    string LRightSet = Pop().ToString();
		    string LLeftSet = Pop().ToString();
		    Push(String.Format("{0} union {1} {2}", LLeftSet, LDistinct ? "distinct" : "all", LRightSet));
		}
		
		public override void iAvg()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedAvg);
		}
		
		public override void iCount()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedCount);
		}
		
		public override void iSum()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedSum);
		}
		
		public override void iMax()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedMax);
		}
		
		public override void iMin()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedMin);
		}
		
		public override void iGetValue()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedGetValue);
		}
		
		public override void iClearValue()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedClearValue);
		}
		
		public override void iSetValue()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedSetValue);
		}
		
		public override void iGetRecordValue()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedGetRecordValue);
		}
		
		public override void iSetRecord()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedSetRecord);
		}
		
		public override void iClearRecord()
		{
		    throw new MachineException(MachineException.Codes.UnimplementedClearRecord);
		}
		
		public override void iCreateTable()
		{
			ColumnDefinitions LColumns = (ColumnDefinitions)Pop();
			string LTableName = Pop().ToString();
			StringBuilder LColumnList = new StringBuilder();
			bool LFirst = true;
			foreach (ColumnDefinition LColumn in LColumns)
			{
				if (!LFirst)
					LColumnList.Append(", ");
				else
					LFirst = false;
					
				LColumnList.AppendFormat("{0} {1} ", LColumn.ColumnName, LColumn.DomainName);

				if (LColumn.IsNullable)
					LColumnList.Append("null");
				else
					LColumnList.Append("not null");
			}
			Push(String.Format("create table {0} ({1})", LTableName, LColumnList.ToString()));
		}
		
		public override void iAlterTable()
		{
			DropColumnDefinitions LDropColumns = (DropColumnDefinitions)Pop();
			ColumnDefinitions LAddColumns = (ColumnDefinitions)Pop();
			if ((LAddColumns.Count > 0) && (LDropColumns.Count > 0)) 
				throw new MachineException(MachineException.Codes.InvalidAlterTableStatement);
			string LTableName = Pop().ToString();
			StringBuilder LColumnList = new StringBuilder();
			bool LFirst = true;
			if (LAddColumns.Count > 0)
			{
				foreach (ColumnDefinition LColumn in LAddColumns)
				{
					if (!LFirst)
						LColumnList.Append(", ");
					else
						LFirst = false;
						
					LColumnList.AppendFormat("{0} {1} ", LColumn.ColumnName, LColumn.DomainName);

					if (LColumn.IsNullable)
						LColumnList.Append("nullable");
					else
						LColumnList.Append("not null");
				}
				Push(String.Format("alter table {0} add {1}", LTableName, LColumnList.ToString()));
			}
			else if (LDropColumns.Count > 0)
			{
				foreach (DropColumnDefinition LColumn in LDropColumns)
				{
					if (!LFirst)
						LColumnList.Append(", ");
					else
						LFirst = false;
						
					LColumnList.AppendFormat("column {0}", LColumn.ColumnName);
				}
				Push(String.Format("alter table {0} drop {1}", LTableName, LColumnList.ToString()));
			}
		}
		
		public override void iDropTable()
		{
			string LTableName = Pop().ToString();
			Push(String.Format("drop table {0}", LTableName));
		}
		
		public override void iCreateIndex()
		{
			bool LIsClustered = (bool)Pop();
			bool LIsUnique = (bool)Pop();
			OrderColumnDefinitions LColumns = (OrderColumnDefinitions)Pop();
			string LTableName = Pop().ToString();
			string LIndexName = Pop().ToString();
			StringBuilder LColumnList = new StringBuilder();
			bool LFirst = true;
			foreach (OrderColumnDefinition LColumn in LColumns)
			{
				if (!LFirst)
					LColumnList.Append(", ");
				else
					LFirst = false;
					
				LColumnList.AppendFormat("{0} {1}", LColumn.ColumnName, LColumn.Ascending ? "asc" : "desc");
			}
			Push(String.Format("create {0} {1} index {2} on {3} ({4})", new object[]{LIsUnique ? "unique" : "", LIsClustered ? "clustered" : "nonclustered", LIndexName, LTableName, LColumnList.ToString()}));
		}
		
		public override void iDropIndex()
		{
			string LTableName = Pop().ToString();
			string LIndexName = Pop().ToString();
			Push(String.Format("drop index {0}.{1}", LTableName, LIndexName));
		}
		
		public override void iBatch()
		{
			Statements LStatements = (Statements)Pop();
			StringBuilder LBatch = new StringBuilder();
			bool LFirst = true;
			foreach (Statement LStatement in LStatements)
			{
				if (!LFirst)
					LBatch.Append("\n");
				else
					LFirst = false;
					
				LStatement.Process(this);
				LBatch.Append(Pop().ToString());
			}
			//LBatch.Append("\ngo");
			Push(LBatch.ToString());
		}

		public override void iCase()
		{
			int LCaseCount = (int)Pop();
			List LList = new List();
			for (int LIndex = 0; LIndex < LCaseCount; LIndex++)
				LList.Insert(0, Pop());
				
			string LCaseList = String.Empty;
			foreach (object LObject in LList)
			{
				LCaseList = LCaseList + (LCaseList == String.Empty ? String.Empty : " ");
				if (!((LObject is CaseItemExpression) || (LObject is CaseElseExpression)))
				{
					((Expression)LObject).Process(this);
					LCaseList = LCaseList + Pop().ToString();
				}
				else if (LObject is CaseItemExpression)
				{
					((CaseItemExpression)LObject).WhenExpression.Process(this);
					LCaseList = String.Format("{0}{1} {2}", LCaseList, Keywords.When, Pop().ToString());
					((CaseItemExpression)LObject).ThenExpression.Process(this);
					LCaseList = String.Format("{0} {1} {2}", LCaseList, Keywords.Then, Pop().ToString());
				}
				else if (LObject is CaseElseExpression)
				{
					((CaseElseExpression)LObject).Process(this);
					LCaseList = String.Format("{0}{1} {2}", LCaseList, Keywords.Else, Pop().ToString());
				}
			}
			Push(String.Format("{0} {1} {2}", Keywords.Case, LCaseList, Keywords.End));
		}

		public override void iCall()
		{
			int LArgumentCount = (int)Pop();
			string LArgumentList = String.Empty;
			for (int LIndex = 0; LIndex < LArgumentCount; LIndex++)
				LArgumentList = 
					Pop().ToString() +
					(LArgumentList == String.Empty ? String.Empty : Keywords.ListSeparator + " ") +
					LArgumentList;
			Push(Pop().ToString() + Keywords.BeginGroup + LArgumentList + Keywords.EndGroup);
		}
    }
}

