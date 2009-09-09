/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	
	/// <summary>
	/// Provides value management services for processes within the DAE.
	/// </summary>
	public interface IValueManager
	{
		Schema.DataTypes DataTypes { get; }
		IStreamManager StreamManager { get; }
		Conveyor GetConveyor(Schema.ScalarType AScalarType);
		Schema.IDataType CompileTypeSpecifier(string ATypeSpecifier);
		Schema.Order FindClusteringOrder(Schema.TableVar ATableVar);
		Schema.Order OrderFromKey(Schema.Key AKey);
		bool OrderIncludesKey(Schema.Order AIncludingOrder, Schema.Key AIncludedKey);
		bool OrderIncludesOrder(Schema.Order AIncludingOrder, Schema.Order AIncludedOrder);
		Schema.Sort GetUniqueSort(Schema.IDataType ADataType);
		int EvaluateSort(Schema.OrderColumn AOrderColumn, object AIndexValue, object ACompareValue);
		DataValue GetAsDataValue(Schema.Representation ARepresentation, object AValue);
		object GetAsNative(Schema.Representation ARepresentation, object AValue);
		object SetAsNative(Schema.Representation ARepresentation, object AValue, object ANewValue);
	}
	
	/// <summary>
	/// Provides value management services for processes within the DAE.
	/// </summary>
	public class ValueManager : IValueManager
	{
		public ValueManager(ServerProcess AServerProcess, IServerProcess AServerProcessInterface)
		{
			FServerProcess = AServerProcess;
			FServerProcessInterface = AServerProcessInterface;
			FPlan = new Plan(FServerProcess);
		}
		
		private ServerProcess FServerProcess;
		private IServerProcess FServerProcessInterface;

		private Plan FPlan;
		
		public Schema.DataTypes DataTypes { get { return FServerProcess.DataTypes; } }
		
		public IStreamManager StreamManager { get { return (IStreamManager)FServerProcessInterface; } }
		
		private Dictionary<string, Conveyor> FConveyors = new Dictionary<string, Conveyor>();
		
		public Conveyor GetConveyor(Schema.ScalarType AScalarType)
		{
			Conveyor LConveyor;
			if (!FConveyors.TryGetValue(AScalarType.Name, out LConveyor))
			{
				LConveyor = (Conveyor)FServerProcessInterface.CreateObject(AScalarType.ClassDefinition, null);
				FConveyors.Add(AScalarType.Name, LConveyor);
			}
			return LConveyor;
		}
		
		public Schema.IDataType CompileTypeSpecifier(string ATypeSpecifier)
		{
			return Compiler.CompileTypeSpecifier(FPlan, new Parser().ParseTypeSpecifier(ATypeSpecifier));
		}
		
		public Schema.Order FindClusteringOrder(Schema.TableVar ATableVar)
		{
			return Compiler.FindClusteringOrder(FPlan, ATableVar);
		}
		
		public bool OrderIncludesKey(Schema.Order AIncludingOrder, Schema.Key AIncludedKey)
		{
			return Compiler.OrderIncludesKey(FPlan, AIncludingOrder, AIncludedKey);
		}
		
		public bool OrderIncludesOrder(Schema.Order AIncludingOrder, Schema.Order AIncludedOrder)
		{
			return Compiler.OrderIncludesOrder(FPlan, AIncludingOrder, AIncludedOrder);
		}
		
		public Schema.Order OrderFromKey(Schema.Key AKey)
		{
			return Compiler.OrderFromKey(FPlan, AKey);
		}
		
		public Schema.Sort GetUniqueSort(Schema.IDataType AType)
		{
			return Compiler.GetUniqueSort(FPlan, AType);
		}
		
		public int EvaluateSort(Schema.OrderColumn AOrderColumn, object AIndexValue, object ACompareValue)
		{
			#if USEICOMPARABLE
			IComparable LIndexComparable = AIndexValue as IComparable;
			if (LIndexComparable != null)
				return LIndexComparable.CompareTo(ACompareValue) * (AOrderColumn.Ascending ? 1 : -1);
			else
			{
			#endif
				// NOTE: Use currently executing program because the whole point is that this is inner loop sort code.
				// We don't want to have to use a new program, or
				Program LProgram = FServerProcess.ExecutingProgram;
				//LProgram.Stack.PushWindow(0);
				//try
				//{
					LProgram.Stack.Push(AIndexValue);
					LProgram.Stack.Push(ACompareValue);
					try
					{
						return ((int)AOrderColumn.Sort.CompareNode.Execute(LProgram)) * (AOrderColumn.Ascending ? 1 : -1);
					}
					finally
					{
						LProgram.Stack.Pop();
						LProgram.Stack.Pop();
					}
				//}
				//finally
				//{
				//	LProgram.Stack.PopWindow();
				//}
			#if USEICOMPARABLE
			} 
			#endif
		}
		
        private void EnsureReadNode(Plan APlan, Schema.Representation ARepresentation)
        {
			if (ARepresentation.ReadNode == null)
			{
				if (ARepresentation.Properties.Count > 1)
					throw new SchemaException(SchemaException.Codes.InvalidConversionRepresentation, ARepresentation.Name, ARepresentation.ScalarType.Name);
					
				APlan.Symbols.Push(new Symbol("AValue", ARepresentation.ScalarType));
				try
				{
					ARepresentation.ReadNode = Compiler.Bind(APlan, Compiler.EmitPropertyReadNode(APlan, new StackReferenceNode("AValue", ARepresentation.ScalarType, 0, true), ARepresentation.ScalarType, ARepresentation.Properties[0]));
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
        }
        
        public DataValue GetAsDataValue(Schema.Representation ARepresentation, object AValue)
        {
			EnsureReadNode(FPlan, ARepresentation);
			Program LProgram = FServerProcess.ExecutingProgram;
			LProgram.Stack.Push(AValue);
			try
			{
				return DataValue.FromNative(this, ARepresentation.ScalarType, ARepresentation.ReadNode.Execute(LProgram));
			}
			finally
			{
				LProgram.Stack.Pop();
			}
        }
        
        public object GetAsNative(Schema.Representation ARepresentation, object AValue)
        {
			EnsureReadNode(FPlan, ARepresentation);
			Program LProgram = FServerProcess.ExecutingProgram;
			LProgram.Stack.Push(AValue);
			try
			{
				return ARepresentation.ReadNode.Execute(LProgram);
			}
			finally
			{
				LProgram.Stack.Pop();
			}
        }

        private void EnsureWriteNode(Plan APlan, Schema.Representation ARepresentation)
        {
			if (ARepresentation.WriteNode == null)
			{
				if (ARepresentation.Properties.Count > 1)
					throw new SchemaException(SchemaException.Codes.InvalidConversionRepresentation, ARepresentation.Name, ARepresentation.ScalarType.Name);
					
				APlan.Symbols.Push(new Symbol("AValue", ARepresentation.ScalarType));
				try
				{
					APlan.Symbols.Push(new Symbol("ANewValue", ARepresentation.Properties[0].DataType));
					try
					{
						ARepresentation.WriteNode = Compiler.Bind(APlan, Compiler.EmitPropertyWriteNode(APlan, null, ARepresentation.Properties[0], new StackReferenceNode("ANewValue", ARepresentation.Properties[0].DataType, 0, true), new StackReferenceNode("AValue", ARepresentation.ScalarType, 1, true))).Nodes[1];
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}					
			}
        }
        
        public object SetAsNative(Schema.Representation ARepresentation, object AValue, object ANewValue)
        {
			EnsureWriteNode(FPlan, ARepresentation);
			Program LProgram = FServerProcess.ExecutingProgram;
			LProgram.Stack.Push(AValue);
			try
			{
				LProgram.Stack.Push(ANewValue);
				try
				{
					return ARepresentation.WriteNode.Execute(LProgram);
				}
				finally
				{
					LProgram.Stack.Pop();
				}
			}
			finally
			{
				LProgram.Stack.Pop();
			}
        }
	}
}
