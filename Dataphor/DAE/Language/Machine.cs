/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Language
{
    using System;
    using System.ComponentModel;
        
    /// <summary>
    ///    Machine
    /// </summary>
    [ToolboxItem(false)]
    public abstract class Machine : Component
    {
        public abstract void Push(object AObject);

        public abstract object Pop();

        public abstract void Reset();

        public virtual void Execute(string AInstruction)
        {
            // find the method corresponding to AInstruction and invoke it
            this.GetType().GetMethod(AInstruction).Invoke(this, null);
        }
    }
    
    public class StackMachine : Machine
    {
        private System.Collections.Stack FStack;
        public StackMachine() : base()
        {
          FStack = new System.Collections.Stack();
        }

        public override void Push(object AObject)
        {
          FStack.Push(AObject);
        }

        public override object Pop()
        {
          return FStack.Pop();
        }

        public override void Reset()
        {
          FStack.Clear();
        }
    }
    
    public class SimpleMachine : StackMachine
    {

        public SimpleMachine() : base(){}

        // CaseSensitive
        private bool FCaseSensitive;
        public bool CaseSensitive
        {
            get
            {
                return FCaseSensitive;
            }
            set
            {
                FCaseSensitive = value;
            }
        }
        
        public virtual bool IsEquivalenceOperator(string AInstruction)
        {
            return (String.Compare(AInstruction, "iEqual", true) == 0) || (String.Compare(AInstruction, "iNotEqual", true) == 0);
        }

        public virtual bool IsComparisonOperator(string AInstruction)
        {
            return 
                IsEquivalenceOperator(AInstruction) ||
                (String.Compare(AInstruction, "iLess", true) == 0) ||
                (String.Compare(AInstruction, "iInclusiveLess", true) == 0) ||
                (String.Compare(AInstruction, "iGreater", true) == 0) ||
                (String.Compare(AInstruction, "iInclusiveGreater", true) == 0);
        }

        public virtual bool IsStringComparisonOperator(string AInstruction)
        {
            return IsComparisonOperator(AInstruction);
        }

        public virtual bool IsLogicalOperator(string AInstruction)
        {
            return
                (String.Compare(AInstruction, "iAnd", true) == 0) ||
                (String.Compare(AInstruction, "iOr", true) == 0) ||
                (String.Compare(AInstruction, "iXor", true) == 0) ||
                (String.Compare(AInstruction, "iNot", true) == 0);
        }

        public virtual bool IsBooleanOperator(string AInstruction)
        {
            return
                IsLogicalOperator(AInstruction) ||
                IsComparisonOperator(AInstruction) ||
                IsStringComparisonOperator(AInstruction);
        }

        public virtual bool IsArithmeticOperator(string AInstruction)
        {
            return
                (String.Compare(AInstruction, "iAddition", true) == 0) ||
                (String.Compare(AInstruction, "iSubtraction", true) == 0) ||
                (String.Compare(AInstruction, "iMultiplication", true) == 0) ||
                (String.Compare(AInstruction, "iDivision", true) == 0) ||
                (String.Compare(AInstruction, "iModulus", true) == 0);
        }

        public virtual bool IsNumericOperator(string AInstruction)
        {
            return
                IsComparisonOperator(AInstruction) ||
                IsArithmeticOperator(AInstruction);
        }

        public virtual bool IsStringOperator(string AInstruction)
        {
            return
                IsStringComparisonOperator(AInstruction) ||
                (String.Compare(AInstruction, "iAddition", true) == 0);
        }
        
        public virtual object BinaryLogicalOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            switch (AInstruction.ToLower())
            {
                case "iand": return (bool)ALeftObject && (bool)ARightObject;
                case "ior": return (bool)ALeftObject || (bool)ARightObject;
                case "ixor": return ((bool)ALeftObject || (bool)ARightObject) && !((bool)ALeftObject && (bool)ARightObject);
                default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
            }
        }
        
        public virtual object UnaryLogicalOperator(object AObject, string AInstruction)
        {
            switch (AInstruction.ToLower())
            {
                case "inot": return !(bool)AObject;
                default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
            }
        }
        
        // Logical Operators 
        public virtual void iAnd()
        {
            Push(BinaryLogicalOperator(Pop(), Pop(), "iAnd"));
        }

        public virtual void iOr()
        {
            Push(BinaryLogicalOperator(Pop(), Pop(), "iOr"));
        }

        public virtual void iXor()
        {
            Push(BinaryLogicalOperator(Pop(), Pop(), "iXor"));
        }

        public virtual void iNot()
        {
            Push(UnaryLogicalOperator(Pop(), "iNot"));
        }
        
        public virtual object ComparisonOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            switch (AInstruction.ToLower())
            {
                case "iequal": return ALeftObject.Equals(ARightObject);
                case "inotequal": return !(ALeftObject.Equals(ARightObject));
                case "igreater": return ((IComparable)ALeftObject).CompareTo(ARightObject) > 0;
                case "iinclusivegreater": return ((IComparable)ALeftObject).CompareTo(ARightObject) >= 0;
                case "iless": return ((IComparable)ALeftObject).CompareTo(ARightObject) < 0;
                case "iinclusiveless": return ((IComparable)ALeftObject).CompareTo(ARightObject) <= 0;
                default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
            }
        }
        
        // Comparison Operators 
        public virtual void iEqual()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iEqual"));
        }

        public virtual void iNotEqual()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iNotEqual"));
        }

        public virtual void iGreater()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iGreater"));
        }

        public virtual void iInclusiveGreater()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iInclusiveGreater"));
        }

        public virtual void iLess()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iLess"));
        }

        public virtual void iInclusiveLess()
        {
            Push(ComparisonOperator(Pop(), Pop(), "iInclusiveLess"));
        }
        
        public virtual object ValueTypeArithmeticOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            if ((ALeftObject is DateTime) || (ARightObject is DateTime))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return new DateTime(((DateTime)ALeftObject).Ticks + ((DateTime)ARightObject).Ticks);
                    case "isubtraction": return (DateTime)ALeftObject - (DateTime)ARightObject;
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is string) || (ARightObject is string))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return (string)ALeftObject + (string)ARightObject;
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is char) || (ARightObject is char))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return (char)ALeftObject + (char)ARightObject; 
                    case "isubtraction": return (char)ALeftObject - (char)ARightObject; 
                    case "imultiplication": return (char)ALeftObject * (char)ARightObject; 
                    case "idivision": return (char)ALeftObject / (char)ARightObject; 
                    case "imodulus": return (char)ALeftObject % (char)ARightObject; 
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is decimal) || (ARightObject is decimal))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return (decimal)ALeftObject + (decimal)ARightObject; 
                    case "isubtraction": return (decimal)ALeftObject - (decimal)ARightObject; 
                    case "imultiplication": return (decimal)ALeftObject * (decimal)ARightObject; 
                    case "idivision": return (decimal)ALeftObject / (decimal)ARightObject; 
                    case "imodulus": return (decimal)ALeftObject % (decimal)ARightObject; 
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is double) || (ARightObject is double))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return (double)ALeftObject + (double)ARightObject; 
                    case "isubtraction": return (double)ALeftObject - (double)ARightObject; 
                    case "imultiplication": return (double)ALeftObject * (double)ARightObject; 
                    case "idivision": return (double)ALeftObject / (double)ARightObject; 
                    case "imodulus": return (double)ALeftObject % (double)ARightObject; 
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is float) || (ARightObject is float))
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return (float)ALeftObject + (float)ARightObject; 
                    case "isubtraction": return (float)ALeftObject - (float)ARightObject; 
                    case "imultiplication": return (float)ALeftObject * (float)ARightObject; 
                    case "idivision": return (float)ALeftObject / (float)ARightObject; 
                    case "imodulus": return (float)ALeftObject % (float)ARightObject; 
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                }
            else if ((ALeftObject is long) || (ALeftObject is ulong) || (ARightObject is long) || (ARightObject is ulong))
            {
                if (((ALeftObject is long) && (ARightObject is ulong)) || ((ALeftObject is ulong) && (ARightObject is long)))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (decimal)ALeftObject + (decimal)ARightObject; 
                        case "isubtraction": return (decimal)ALeftObject - (decimal)ARightObject; 
                        case "imultiplication": return (decimal)ALeftObject * (decimal)ARightObject; 
                        case "idivision": return (decimal)ALeftObject / (decimal)ARightObject; 
                        case "imodulus": return (decimal)ALeftObject % (decimal)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is ulong) || (ARightObject is ulong))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (ulong)ALeftObject + (ulong)ARightObject; 
                        case "isubtraction": return (ulong)ALeftObject - (ulong)ARightObject; 
                        case "imultiplication": return (ulong)ALeftObject * (ulong)ARightObject; 
                        case "idivision": return (ulong)ALeftObject / (ulong)ARightObject; 
                        case "imodulus": return (ulong)ALeftObject % (ulong)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is long) || (ARightObject is long))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (long)ALeftObject + (long)ARightObject; 
                        case "isubtraction": return (long)ALeftObject - (long)ARightObject; 
                        case "imultiplication": return (long)ALeftObject * (long)ARightObject; 
                        case "idivision": return (long)ALeftObject / (long)ARightObject; 
                        case "imodulus": return (long)ALeftObject % (long)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else
                    throw new MachineException(MachineException.Codes.UnknownValueType, ALeftObject.GetType().Name);
            }
            else if ((ALeftObject is int) || (ALeftObject is uint) || (ARightObject is int) || (ARightObject is uint))
            {
                if (((ALeftObject is int) && (ARightObject is uint)) || ((ALeftObject is uint) && (ARightObject is int)))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (long)ALeftObject + (long)ARightObject; 
                        case "isubtraction": return (long)ALeftObject - (long)ARightObject; 
                        case "imultiplication": return (long)ALeftObject * (long)ARightObject; 
                        case "idivision": return (long)ALeftObject / (long)ARightObject; 
                        case "imodulus": return (long)ALeftObject % (long)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is uint) || (ARightObject is uint))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (uint)ALeftObject + (uint)ARightObject; 
                        case "isubtraction": return (uint)ALeftObject - (uint)ARightObject; 
                        case "imultiplication": return (uint)ALeftObject * (uint)ARightObject; 
                        case "idivision": return (uint)ALeftObject / (uint)ARightObject; 
                        case "imodulus": return (uint)ALeftObject % (uint)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is int) || (ARightObject is int))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (int)ALeftObject + (int)ARightObject; 
                        case "isubtraction": return (int)ALeftObject - (int)ARightObject; 
                        case "imultiplication": return (int)ALeftObject * (int)ARightObject; 
                        case "idivision": return (int)ALeftObject / (int)ARightObject; 
                        case "imodulus": return (int)ALeftObject % (int)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else
                    throw new MachineException(MachineException.Codes.UnknownValueType, ALeftObject.GetType().Name);
            }
            else if ((ALeftObject is short) || (ALeftObject is ushort) || (ARightObject is short) || (ARightObject is ushort))
            {
                if (((ALeftObject is short) && (ARightObject is ushort)) || ((ALeftObject is ushort) && (ARightObject is short)))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (int)ALeftObject + (int)ARightObject; 
                        case "isubtraction": return (int)ALeftObject - (int)ARightObject; 
                        case "imultiplication": return (int)ALeftObject * (int)ARightObject; 
                        case "idivision": return (int)ALeftObject / (int)ARightObject; 
                        case "imodulus": return (int)ALeftObject % (int)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is ushort) || (ARightObject is ushort))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (ushort)ALeftObject + (ushort)ARightObject; 
                        case "isubtraction": return (ushort)ALeftObject - (ushort)ARightObject; 
                        case "imultiplication": return (ushort)ALeftObject * (ushort)ARightObject; 
                        case "idivision": return (ushort)ALeftObject / (ushort)ARightObject; 
                        case "imodulus": return (ushort)ALeftObject % (ushort)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is short) || (ARightObject is short))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (short)ALeftObject + (short)ARightObject; 
                        case "isubtraction": return (short)ALeftObject - (short)ARightObject; 
                        case "imultiplication": return (short)ALeftObject * (short)ARightObject; 
                        case "idivision": return (short)ALeftObject / (short)ARightObject; 
                        case "imodulus": return (short)ALeftObject % (short)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else
                    throw new MachineException(MachineException.Codes.UnknownValueType, ALeftObject.GetType().Name);
            }
            else if ((ALeftObject is byte) || (ALeftObject is sbyte) || (ARightObject is byte) || (ARightObject is sbyte))
            {
                if (((ALeftObject is byte) && (ARightObject is sbyte)) || ((ALeftObject is sbyte) && (ARightObject is byte)))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (short)ALeftObject + (short)ARightObject; 
                        case "isubtraction": return (short)ALeftObject - (short)ARightObject; 
                        case "imultiplication": return (short)ALeftObject * (short)ARightObject; 
                        case "idivision": return (short)ALeftObject / (short)ARightObject; 
                        case "imodulus": return (short)ALeftObject % (short)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ARightObject is byte) || (ARightObject is byte))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (byte)ALeftObject + (byte)ARightObject; 
                        case "isubtraction": return (byte)ALeftObject - (byte)ARightObject; 
                        case "imultiplication": return (byte)ALeftObject * (byte)ARightObject; 
                        case "idivision": return (byte)ALeftObject / (byte)ARightObject; 
                        case "imodulus": return (byte)ALeftObject % (byte)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else if ((ALeftObject is sbyte) || (ARightObject is sbyte))
                    switch (AInstruction.ToLower())
                    {
                        case "iaddition": return (sbyte)ALeftObject + (sbyte)ARightObject; 
                        case "isubtraction": return (sbyte)ALeftObject - (sbyte)ARightObject; 
                        case "imultiplication": return (sbyte)ALeftObject * (sbyte)ARightObject; 
                        case "idivision": return (sbyte)ALeftObject / (sbyte)ARightObject; 
                        case "imodulus": return (sbyte)ALeftObject % (sbyte)ARightObject; 
                        default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction);
                    }
                else
                    throw new MachineException(MachineException.Codes.UnknownValueType, ALeftObject.GetType().Name);
            }
            else
                throw new MachineException(MachineException.Codes.UnknownValueType, ALeftObject.GetType().Name);
        }
        
        public virtual object ArithmeticOperator(object ARightObject, object ALeftObject, string AInstruction)
        {
            if ((ALeftObject is ValueType) || (ARightObject is ValueType))
                return ValueTypeArithmeticOperator(ALeftObject, ARightObject, AInstruction);
            else
            {
                // perform a lookup for the appropriate operator overloading method 
                Type[] LTypes = {ALeftObject.GetType(), ARightObject.GetType()};
                object[] LObjects = {ALeftObject, ARightObject};
                switch (AInstruction.ToLower())
                {
                    case "iaddition": return ALeftObject.GetType().GetMethod("op_Addition", LTypes).Invoke(ALeftObject, LObjects);
                    case "isubtraction": return ALeftObject.GetType().GetMethod("op_Subtraction", LTypes).Invoke(ALeftObject, LObjects);
                    case "imultiplication": return ALeftObject.GetType().GetMethod("op_Multiply", LTypes).Invoke(ALeftObject, LObjects);
                    case "idivision": return ALeftObject.GetType().GetMethod("op_Division", LTypes).Invoke(ALeftObject, LObjects);
                    case "imodulus": return ALeftObject.GetType().GetMethod("op_Modulus", LTypes).Invoke(ALeftObject, LObjects);
                    default: throw new MachineException(MachineException.Codes.InvalidInstruction, AInstruction); 
                }
            }
        }
        
        // Arithmetic Operators 
        public virtual void iAddition()
        {
            Push(ArithmeticOperator(Pop(), Pop(), "iAddition"));
        }

        public virtual void iSubtraction()
        {   
            Push(ArithmeticOperator(Pop(), Pop(), "iSubtraction"));
        }

        public virtual void iMultiplication()
        {
            Push(ArithmeticOperator(Pop(), Pop(), "iMultiplication"));
        }

        public virtual void iDivision()
        {
            Push(ArithmeticOperator(Pop(), Pop(), "iDivision"));
        }

        public virtual void iModulus()
        {
            Push(ArithmeticOperator(Pop(), Pop(), "iModulus"));
        }
        
        public virtual void iPower(){}
        public virtual void iBitwiseNot(){}
        public virtual void iBitwiseAnd(){}
        public virtual void iBitwiseOr(){}
        public virtual void iBitwiseXor(){}
        public virtual void iLeftShift(){}
        public virtual void iRightShift(){}
        
/*
        // Conditional Operators
        public virtual void iIf()
        {
			Expression LFalseExpression = (Expression)Pop();
			Expression LTrueExpression = (Expression)Pop();
			bool LValue = (bool)Pop();
			if ((bool)Pop())
				LTrueExpression.Process(this);
			else
				LFalseExpression.Process(this);
        }
        
        public virtual void iCase()
        {
			List LList = new List();
			try
			{
				object LObject;
				Expression LCaseExpression = null;
				Expression LElseExpression = null;
				do
				{
					LObject = Pop();
					if (LObject is CaseElseExpression)
						LElseExpression = (CaseElseExpression)LObject;
					else if (LObject is CaseItemExpression)
						LList.Add(LObject);
					else 
					{
						LCaseExpression = (Expression)LObject;
						break;
					}
				} while (true);
				
				if (LCaseExpression != null)
				{
					LCaseExpression.Process(this);
					object LValue = Pop();
					object LCaseValue = null;
					foreach (object LCaseItem in LList)
					{
						((CaseItemExpression)LCaseItem).WhenExpression.Process(this);
						LCaseValue = Pop();
						if (LValue.Equals(LCaseValue))
						{
							((CaseItemExpression)LCaseItem).ThenExpression.Process(this);
							return;
						}
					}
				}
				else
				{
					foreach (object LCaseItem in LList)
					{
						((CaseItemExpression)LCaseItem).WhenExpression.Process(this);
						if ((bool)Pop())
						{
							((CaseItemExpression)LCaseItem).ThenExpression.Process(this);
							return;
						}
					}
				}

				if (LElseExpression != null)
				{
					LElseExpression.Process(this);
					return;
				}

				throw new MachineException(MachineException.Codes.IndeterminateCaseExpression);
			}
			finally
			{
				LList.Dispose();
			}
        }
*/
    }
}
