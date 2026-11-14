/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Device.SQL
{
	using System;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Language;
	using D4 = Alphora.Dataphor.DAE.Language.D4;
	
	/// <summary>
	/// This is a mapping of DateTime and TimeSpan Operators.  The TimeSpan operators defined here
	/// will work for all of the devices, because none of them have a TimeSpan class.  We use a BigInt
	/// or similar type to simulate the TimeSpan datatype.
	/// </summary>
	public class SQLTimeSpanMilliseconds : DeviceOperator
	{
		public SQLTimeSpanMilliseconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(10000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanSeconds : DeviceOperator
	{
		public SQLTimeSpanSeconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(10000000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanMinutes : DeviceOperator
	{
		public SQLTimeSpanMinutes(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(600000000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanHours : DeviceOperator
	{
		public SQLTimeSpanHours(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanHours(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanHours(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(36000000000m, TokenType.Decimal));
		}
	}

	public class SQLTimeSpanDays : DeviceOperator
	{
		public SQLTimeSpanDays(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanDays(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanDays(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
		}
	}

	public class SQLTimeSpan5Operands : DeviceOperator
	{
		public SQLTimeSpan5Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpan5Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpan5Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression expression3 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false);
			Expression expression4 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[3], false);
			Expression expression5 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[4], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Third = new BinaryExpression(expression3, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			Expression Fourth = new BinaryExpression(expression4, "iMultiplication", new ValueExpression(10000000m,TokenType.Decimal));
			Expression Fifth = new BinaryExpression(expression5, "iMultiplication", new ValueExpression(10000m,TokenType.Decimal));
			Expression FirstSecond = new BinaryExpression(First, "iAddition", Second);
			Expression ThirdFourth = new BinaryExpression(Third, "iAddition", Fourth);
			Expression FirstFour = new BinaryExpression(FirstSecond, "iAddition", ThirdFourth);
			return new BinaryExpression(FirstFour, "iAddition", Fifth);
		}
	}

	public class SQLTimeSpan4Operands : DeviceOperator
	{
		public SQLTimeSpan4Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpan4Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpan4Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression expression3 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false);
			Expression expression4 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[3], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Third = new BinaryExpression(expression3, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			Expression Fourth = new BinaryExpression(expression4, "iMultiplication", new ValueExpression(10000000m,TokenType.Decimal));
			Expression FirstSecond = new BinaryExpression(First, "iAddition", Second);
			Expression ThirdFourth = new BinaryExpression(Third, "iAddition", Fourth);
			return new BinaryExpression(FirstSecond, "iAddition", ThirdFourth);
		}
	}

	public class SQLTimeSpan3Operands : DeviceOperator
	{
		public SQLTimeSpan3Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpan3Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpan3Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression expression3 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false);		
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Third = new BinaryExpression(expression3, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			Expression FirstSecond = new BinaryExpression(First, "iAddition", Second);
			return new BinaryExpression(FirstSecond, "iAddition", Third);
		}
	}

	public class SQLTimeSpan2Operands : DeviceOperator
	{
		public SQLTimeSpan2Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpan2Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpan2Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			return new BinaryExpression(First, "iAddition", Second);
		}
	}

	public class SQLTimeSpan1Operand : DeviceOperator
	{
		public SQLTimeSpan1Operand(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpan1Operand(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpan1Operand(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(864000000000m,TokenType.Decimal));
			return First;
		}
	}

	public class SQLTimeSpanTime4Operands : DeviceOperator
	{
		public SQLTimeSpanTime4Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanTime4Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanTime4Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression expression3 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false);
			Expression expression4 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[3], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			Expression Third = new BinaryExpression(expression3, "iMultiplication", new ValueExpression(10000000m,TokenType.Decimal));
			Expression Fourth = new BinaryExpression(expression4, "iMultiplication", new ValueExpression(10000m,TokenType.Decimal));
			Expression FirstSecond = new BinaryExpression(First, "iAddition", Second);
			Expression ThirdFourth = new BinaryExpression(Third, "iAddition", Fourth);
			return new BinaryExpression(FirstSecond, "iAddition", ThirdFourth);
		}
	}

	public class SQLTimeSpanTime3Operands : DeviceOperator
	{
		public SQLTimeSpanTime3Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanTime3Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanTime3Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression expression3 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false);		
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			Expression Third = new BinaryExpression(expression3, "iMultiplication", new ValueExpression(10000000m,TokenType.Decimal));
			Expression FirstSecond = new BinaryExpression(First, "iAddition", Second);
			return new BinaryExpression(FirstSecond, "iAddition", Third);
		}
	}

	public class SQLTimeSpanTime2Operands : DeviceOperator
	{
		public SQLTimeSpanTime2Operands(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanTime2Operands(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanTime2Operands(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			Expression Second = new BinaryExpression(expression2, "iMultiplication", new ValueExpression(600000000m,TokenType.Decimal));
			return new BinaryExpression(First, "iAddition", Second);
		}
	}

	public class SQLTimeSpanTime1Operand : DeviceOperator
	{
		public SQLTimeSpanTime1Operand(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanTime1Operand(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanTime1Operand(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression First = new BinaryExpression(expression1, "iMultiplication", new ValueExpression(36000000000m,TokenType.Decimal));
			return First;
		}
	}

	public class SQLTimeSpanReadDays : DeviceOperator
	{
		public SQLTimeSpanReadDays(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanReadDays(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanReadDays(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression Days = new BinaryExpression(expression, "iDivision", new ValueExpression(864000000000m, TokenType.Decimal));
			return Days;
		}
	}

	public class SQLTimeSpanWriteDays : DeviceOperator
	{
		public SQLTimeSpanWriteDays(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanWriteDays(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanWriteDays(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression Days = new BinaryExpression(expression, "iMultiplication", new ValueExpression(864000000000m, TokenType.Decimal));
			return Days;
		}
	}
	
	public class SQLTimeSpanReadHours : DeviceOperator
	{
		public SQLTimeSpanReadHours(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanReadHours(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanReadHours(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression Hours = new BinaryExpression(expression, "iDivision", new ValueExpression(36000000000m, TokenType.Decimal));
			return Hours;
		}
	}
	public class SQLTimeSpanWriteHours : DeviceOperator
	{
		public SQLTimeSpanWriteHours(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanWriteHours(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanWriteHours(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression Hours = new BinaryExpression(expression, "iMultiplication", new ValueExpression(36000000000m, TokenType.Decimal));
			return Hours;
		}
	}

	public class SQLTimeSpanReadMinutes : DeviceOperator
	{
		public SQLTimeSpanReadMinutes(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanReadMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanReadMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression Minutes = new BinaryExpression(expression, "iDivision", new ValueExpression(600000000m, TokenType.Decimal));
			return Minutes;
		}
	}

	public class SQLTimeSpanReadMilliseconds : DeviceOperator
	{
		public SQLTimeSpanReadMilliseconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanReadMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanReadMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iDivision", new ValueExpression(10000m,TokenType.Decimal));
		}
	}

	public class SQLTimeSpanWriteMilliseconds : DeviceOperator
	{
		public SQLTimeSpanWriteMilliseconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanWriteMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanWriteMilliseconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(10000m,TokenType.Decimal));
		}
	}

	public class SQLTimeSpanWriteSeconds : DeviceOperator
	{
		public SQLTimeSpanWriteSeconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanWriteSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanWriteSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(10000000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanReadSeconds : DeviceOperator
	{
		public SQLTimeSpanReadSeconds(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanReadSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanReadSeconds(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new BinaryExpression(expression, "iDivision", new ValueExpression(10000000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanWriteMinutes : DeviceOperator
	{
		public SQLTimeSpanWriteMinutes(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanWriteMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanWriteMinutes(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new BinaryExpression(expression, "iMultiplication", new ValueExpression(600000000,TokenType.Integer));
		}
	}

	public class SQLTimeSpanAddTicks : DeviceOperator
	{
		public SQLTimeSpanAddTicks(int iD, string name) : base(iD, name) {}
		//public SQLTimeSpanAddTicks(Operator AOperator, D4.ClassDefinition AClassDefinition) : base(AOperator, AClassDefinition){}
		//public SQLTimeSpanAddTicks(Operator AOperator, D4.ClassDefinition AClassDefinition, bool AIsSystem) : base(AOperator, AClassDefinition, AIsSystem){}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression expression1 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression expression2 = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new BinaryExpression(expression1, "iAddition", expression2);
		}
	}
}

	

