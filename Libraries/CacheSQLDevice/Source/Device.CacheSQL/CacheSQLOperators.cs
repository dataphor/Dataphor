/*
	Alphora Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.CacheSQL
{

	#region Operators

	public class CacheSQLToday : SQLDeviceOperator
	{
		public CacheSQLToday(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			return new CallExpression
			(
				"Round",
				new Expression[]
				{
					new CallExpression("GetDate", new Expression[] {}), new ValueExpression(0),
					new ValueExpression(1)
				}
			);
		}
	}

	public class CacheSQLSubString : SQLDeviceOperator
	{
		public CacheSQLSubString(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Substring",
					new[]
					{
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
						new BinaryExpression
						(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
							"iAddition",
							new ValueExpression(1, TokenType.Integer)
						),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[2], false)
					}
				);
		}
	}

	// Pos(ASubString, AString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
	public class CacheSQLPos : SQLDeviceOperator
	{
		public CacheSQLPos(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new BinaryExpression
					(
					new CaseExpression
						(
						new[]
						{
							new CaseItemExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iEqual",
									new ValueExpression(String.Empty, TokenType.String)
								),
								new ValueExpression(1, TokenType.Integer)
							)
						},
						new CaseElseExpression
						(
							new CallExpression
							(
								"CharIndex",
								new[]
								{
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
								}
							)
						)
					),
					"iSubtraction",
					new ValueExpression(1, TokenType.Integer)
				);
		}
	}

	// IndexOf(AString, ASubString) ::= case when ASubstring = '' then 1 else CharIndex(ASubstring, AString) end - 1
	public class CacheSQLIndexOf : SQLDeviceOperator
	{
		public CacheSQLIndexOf(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new BinaryExpression
				(
					new CaseExpression
					(
						new[]
						{
							new CaseItemExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
									"iEqual",
									new ValueExpression(String.Empty, TokenType.String)
								),
								new ValueExpression(1, TokenType.Integer)
							)
						},
						new CaseElseExpression
						(
							new CallExpression
							(
								"CharIndex",
								new[]
								{
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false),
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
								}
							)
						)
					),
					"iSubtraction",
					new ValueExpression(1, TokenType.Integer)
				);
		}
	}

	// CompareText(ALeftValue, ARightValue) ::= case when Upper(ALeftValue) = Upper(ARightValue) then 0 when Upper(ALeftValue) < Upper(ARightValue) then -1 when Upper(ALeftValue) > Upper(ARightValue) then 1 else null end
	public class CacheSQLCompareText : SQLDeviceOperator
	{
		public CacheSQLCompareText(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CaseExpression
				(
					new[]
					{
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[0],
											false
										)
									}
								),
								"iEqual",
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[1],
											false
										)
									}
								)
							),
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[0],
											false
										)
									}
								),
								"iLess",
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[1],
											false
										)
									}
								)
							),
							new ValueExpression(-1)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[0],
											false
										)
									}
								),
								"iGreater",
								new CallExpression
								(
									"Upper",
									new[]
									{
										localDevicePlan.Device.TranslateExpression
										(
											localDevicePlan,
											planNode.Nodes[1],
											false
										)
									}
								)
							),
							new ValueExpression(1)
						)
					},
					new CaseElseExpression(new ValueExpression(null, TokenType.Nil))
				);
		}
	}


	// ToString(AValue) ::= Convert(varchar, AValue)
	public class CacheSQLToString : SQLDeviceOperator
	{
		public CacheSQLToString(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("varchar"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	public class CacheSQLToBit : SQLDeviceOperator
	{
		public CacheSQLToBit(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("bit"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	public class CacheSQLToTinyInt : SQLDeviceOperator
	{
		public CacheSQLToTinyInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("tinyint"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// ToByte(AValue) ::= convert(tinyint, AValue & (power(2, 8) - 1))	
	public class CacheSQLToByte : SQLDeviceOperator
	{
		public CacheSQLToByte(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("tinyint"),
						new BinaryExpression
						(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(8, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}

	public class CacheSQLToSmallInt : SQLDeviceOperator
	{
		public CacheSQLToSmallInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("smallint"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// ToSByte(AValue) ::= convert(smallint, ((AValue & (power(2, 8) - 1) & ~power(2, 7)) - (power(2, 7) & AValue)))
	public class CacheSQLToSByte : SQLDeviceOperator
	{
		public CacheSQLToSByte(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("smallint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new ValueExpression(2, TokenType.Integer),
												new ValueExpression(8, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(7, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(7, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	// ToShort(AValue) ::= convert(smallint, ((AValue & (power(2, 16) - 1) & ~power(2, 15)) - (power(2, 15) & AValue)))
	public class CacheSQLToShort : SQLDeviceOperator
	{
		public CacheSQLToShort(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("smallint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new ValueExpression(2, TokenType.Integer),
												new ValueExpression(16, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(15, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(15, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class CacheSQLToInt : SQLDeviceOperator
	{
		public CacheSQLToInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("int"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// ToUShort(AValue) ::= convert(int, AValue & (power(2, 16) - 1))	
	public class CacheSQLToUShort : SQLDeviceOperator
	{
		public CacheSQLToUShort(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("int"),
						new BinaryExpression
						(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(16, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}

	// ToInteger(AValue) ::= convert(int, ((AValue & ((power(convert(bigint, 2), 32) - 1) & ~(power(convert(bigint, 2), 31)) - (power(convert(bigint, 2), 31) & AValue)))
	public class CacheSQLToInteger : SQLDeviceOperator
	{
		public CacheSQLToInteger(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("int"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new CallExpression
										(
											"Power",
											new Expression[]
											{
												new CallExpression
												(
													"Convert",
													new Expression[]
													{
														new IdentifierExpression("bigint"),
														new ValueExpression(2, TokenType.Integer),
													}
												),
												new ValueExpression(32, TokenType.Integer)
											}
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new CallExpression
											(
												"Convert",
												new Expression[]
												{
													new IdentifierExpression("bigint"),
													new ValueExpression(2, TokenType.Integer)
												}
											),
											new ValueExpression(31, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new CallExpression
										(
											"Convert",
											new Expression[]
											{
												new IdentifierExpression("bigint"),
												new ValueExpression(2, TokenType.Integer)
											}
										),
										new ValueExpression(31, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class CacheSQLToBigInt : SQLDeviceOperator
	{
		public CacheSQLToBigInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("bigint"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// ToUInteger(AValue) ::= convert(bigint, AValue & (power(convert(bigint, 2), 32) - 1))	
	public class CacheSQLToUInteger : SQLDeviceOperator
	{
		public CacheSQLToUInteger(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bigint"),
						new BinaryExpression
						(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new CallExpression
										(
											"Convert",
											new Expression[]
											{
												new IdentifierExpression("bigint"),
												new ValueExpression(2, TokenType.Integer)
											}
										),
										new ValueExpression(32, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}

	// ToLong(AValue) ::= convert(bigint, ((AValue & ((power(2, 64) * 1) - 1) & ~power(2, 63)) - (power(2, 63) & AValue)))
	public class CacheSQLToLong : SQLDeviceOperator
	{
		public CacheSQLToLong(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("bigint"),
						new BinaryExpression
						(
							new BinaryExpression
							(
								new BinaryExpression
								(
									localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
									"iBitwiseAnd",
									new BinaryExpression
									(
										new BinaryExpression
										(
											new CallExpression
											(
												"Power",
												new Expression[]
												{
													new ValueExpression(2, TokenType.Integer),
													new ValueExpression(64, TokenType.Integer)
												}
											),
											"iMultiplication",
											new ValueExpression(1, TokenType.Integer)
										),
										"iSubtraction",
										new ValueExpression(1, TokenType.Integer)
									)
								),
								"iBitwiseAnd",
								new UnaryExpression
								(
									"iBitwiseNot",
									new CallExpression
									(
										"Power",
										new Expression[]
										{
											new ValueExpression(2, TokenType.Integer),
											new ValueExpression(63, TokenType.Integer)
										}
									)
								)
							),
							"iSubtraction",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(63, TokenType.Integer)
									}
								),
								"iBitwiseAnd",
								localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
							)
						)
					}
				);
		}
	}

	public class CacheSQLToDecimal20 : SQLDeviceOperator
	{
		public CacheSQLToDecimal20(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("decimal(20, 0)"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	public class CacheSQLToDecimal288 : SQLDeviceOperator
	{
		public CacheSQLToDecimal288(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("decimal(28, 8)"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// ToULong(AValue) ::= convert(decimal(20, 0), AValue & (power(2, 64) - 1))	
	public class CacheSQLToULong : SQLDeviceOperator
	{
		public CacheSQLToULong(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new Expression[]
					{
						new IdentifierExpression("decimal(20, 0)"),
						new BinaryExpression
						(
							localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
							"iBitwiseAnd",
							new BinaryExpression
							(
								new CallExpression
								(
									"Power",
									new Expression[]
									{
										new ValueExpression(2, TokenType.Integer),
										new ValueExpression(64, TokenType.Integer)
									}
								),
								"iSubtraction",
								new ValueExpression(1, TokenType.Integer)
							)
						)
					}
				);
		}
	}

	public class CacheSQLToDecimal : SQLDeviceOperator
	{
		public CacheSQLToDecimal(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("decimal"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	public class CacheSQLToMoney : SQLDeviceOperator
	{
		public CacheSQLToMoney(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("money"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	public class CacheSQLToUniqueIdentifier : SQLDeviceOperator
	{
		public CacheSQLToUniqueIdentifier(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Convert",
					new[]
					{
						new IdentifierExpression("uniqueidentifier"),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false)
					}
				);
		}
	}

	// Class to put all of the static math operators that will be reused.
	public class CacheSQLMath
	{
		public static Expression Truncate(Expression expression)
		{
			return new CallExpression("Round", new[] { expression, new ValueExpression(0), new ValueExpression(1) });
		}

		public static Expression Frac(Expression expression, Expression expressionCopy)
		// note that it takes two different refrences to the same value
		{
			Expression rounded = 
				new CallExpression
				(
					"Round",
					new[]
					{
						expressionCopy, new ValueExpression(0),
						new ValueExpression(1)
					}
				);
			return new BinaryExpression(expression, "iSubtraction", rounded);
		}
	}

	public class CacheSQLTimeSpan
	{
		public static Expression ReadMillisecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression fromFrac = CacheSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(1000));
			return CacheSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadSecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression fromFrac = CacheSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return CacheSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadMinute(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression fromFrac = CacheSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return CacheSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadHour(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression fromFrac = CacheSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(24));
			return CacheSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadDay(Expression tempValue)
		{
			Expression toTrunc = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			return CacheSQLMath.Truncate(toTrunc);
		}
	}

	public class CacheSQLDateTimeFunctions
	{
		public static Expression WriteMonth(Expression dateTime, Expression dateTimeCopy, Expression part)
		{
			string partString = "mm";
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTimeCopy });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}

		public static Expression WriteDay(Expression dateTime, Expression dateTimeCopy, Expression part)
		//pass the DateTime twice
		{
			string partString = "dd";
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTimeCopy });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}

		public static Expression WriteYear(Expression dateTime, Expression dateTimeCopy, Expression part)
		//pass the DateTime twice
		{
			string partString = "yyyy";
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTimeCopy });
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}
	}

	// Operators that CacheSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

	// Math
	public class CacheSQLPower : SQLDeviceOperator
	{
		public CacheSQLPower(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return
				new CallExpression
				(
					"Power",
					new[]
					{
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false),
						localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false)
					}
				);
		}
	}

	public class CacheSQLTruncate : SQLDeviceOperator
	{
		public CacheSQLTruncate(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLMath.Truncate(tempValue);
		}
	}

	public class CacheSQLFrac : SQLDeviceOperator
	{
		public CacheSQLFrac(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression valueCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLMath.Frac(tempValue, valueCopy);
		}
	}

	public class CacheSQLLogB : SQLDeviceOperator
	{
		public CacheSQLLogB(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression baseValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			tempValue = new CallExpression("Log", new[] { tempValue });
			baseValue = new CallExpression("Log", new[] { baseValue });
			return new BinaryExpression(tempValue, "iDivision", baseValue);
		}
	}

	// TimeSpan
	public class CacheSQLTimeSpanReadMillisecond : SQLDeviceOperator
	{
		public CacheSQLTimeSpanReadMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLTimeSpan.ReadMillisecond(tempValue);
		}
	}

	public class CacheSQLTimeSpanReadSecond : SQLDeviceOperator
	{
		public CacheSQLTimeSpanReadSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLTimeSpan.ReadSecond(tempValue);
		}
	}

	public class CacheSQLTimeSpanReadMinute : SQLDeviceOperator
	{
		public CacheSQLTimeSpanReadMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLTimeSpan.ReadMinute(tempValue);
		}
	}

	public class CacheSQLTimeSpanReadHour : SQLDeviceOperator
	{
		public CacheSQLTimeSpanReadHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLTimeSpan.ReadHour(tempValue);
		}
	}

	public class CacheSQLTimeSpanReadDay : SQLDeviceOperator
	{
		public CacheSQLTimeSpanReadDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return CacheSQLTimeSpan.ReadDay(tempValue);
		}
	}

	public class CacheSQLTimeSpanWriteMillisecond : SQLDeviceOperator
	{
		public CacheSQLTimeSpanWriteMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = CacheSQLTimeSpan.ReadMillisecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class CacheSQLTimeSpanWriteSecond : SQLDeviceOperator
	{
		public CacheSQLTimeSpanWriteSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = CacheSQLTimeSpan.ReadSecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class CacheSQLTimeSpanWriteMinute : SQLDeviceOperator
	{
		public CacheSQLTimeSpanWriteMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = CacheSQLTimeSpan.ReadMinute(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(600000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class CacheSQLTimeSpanWriteHour : SQLDeviceOperator
	{
		public CacheSQLTimeSpanWriteHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = CacheSQLTimeSpan.ReadHour(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(36000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class CacheSQLTimeSpanWriteDay : SQLDeviceOperator
	{
		public CacheSQLTimeSpanWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = CacheSQLTimeSpan.ReadDay(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(864000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}


	public class CacheSQLAddMonths : SQLDeviceOperator
	{
		public CacheSQLAddMonths(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression months = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new CallExpression("DateAdd", new[] { new ValueExpression("mm", TokenType.Symbol), months, dateTime });
		}
	}

	public class CacheSQLAddYears : SQLDeviceOperator
	{
		public CacheSQLAddYears(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression months = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return new CallExpression("DateAdd", new[] { new ValueExpression("yyyy", TokenType.Symbol), months, dateTime });
		}
	}

	public class CacheSQLDayOfWeek : SQLDeviceOperator
	// TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
	{
		public CacheSQLDayOfWeek(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("dw", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDayOfYear : SQLDeviceOperator
	{
		public CacheSQLDayOfYear(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("dy", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeReadHour : SQLDeviceOperator
	{
		public CacheSQLDateTimeReadHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("hh", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeReadMinute : SQLDeviceOperator
	{
		public CacheSQLDateTimeReadMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("mi", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeReadSecond : SQLDeviceOperator
	{
		public CacheSQLDateTimeReadSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("ss", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeReadMillisecond : SQLDeviceOperator
	{
		public CacheSQLDateTimeReadMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return new CallExpression("DatePart", new[] { new ValueExpression("ms", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeWriteMillisecond : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			string partString = "ms";
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTime });
			dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}
	}

	public class CacheSQLDateTimeWriteSecond : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			string partString = "ss";
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTime });
			dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}
	}

	public class CacheSQLDateTimeWriteMinute : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			string partString = "mi";
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTime });
			dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime }); 
		}
	}

	public class CacheSQLDateTimeWriteHour : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			string partString = "hh";
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression oldPart = new CallExpression("DatePart", new[] { new ValueExpression(partString, TokenType.Symbol), dateTime });
			dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression parts = new BinaryExpression(part, "iSubtraction", oldPart);
			return new CallExpression("DateAdd", new[] { new ValueExpression(partString, TokenType.Symbol), parts, dateTime });
		}
	}

	public class CacheSQLDateTimeWriteDay : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return CacheSQLDateTimeFunctions.WriteDay(dateTime, dateTimeCopy, part);
		}
	}

	public class CacheSQLDateTimeWriteMonth : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteMonth(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return CacheSQLDateTimeFunctions.WriteMonth(dateTime, dateTimeCopy, part);
		}
	}

	public class CacheSQLDateTimeWriteYear : SQLDeviceOperator
	{
		public CacheSQLDateTimeWriteYear(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return CacheSQLDateTimeFunctions.WriteYear(dateTime, dateTimeCopy, part);
		}
	}

	public class CacheSQLDateTimeDatePart : SQLDeviceOperator
	{
		public CacheSQLDateTimeDatePart(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression fromConvert = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromMath = new CallExpression("Floor", new[] { fromConvert });
			return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), dateTime });
		}
	}

	public class CacheSQLDateTimeTimePart : SQLDeviceOperator
	{
		public CacheSQLDateTimeTimePart(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression fromConvert = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromConvertCopy = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromMath = CacheSQLMath.Frac(fromConvert, fromConvertCopy);
			return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), dateTime });
		}
	}


	/// <summary>
	///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
	///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
	/// </summary>
	public class CacheSQLDateTimeSelector : SQLDeviceOperator
	{
		public CacheSQLDateTimeSelector(int iD, string name)
			: base(iD, name)
		{
		}

		public static Expression DateTimeSelector(Expression year, Expression month, Expression day, Expression hours, Expression minutes, Expression seconds, Expression milliseconds)
		{
			Expression expression = new BinaryExpression(new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), year }), "+", new ValueExpression("-"));
			expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), month }));
			expression = new BinaryExpression(expression, "+", new ValueExpression("-"));
			expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), day }));
			if (hours != null)
			{
				expression = new BinaryExpression(expression, "+", new ValueExpression(" "));
				expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), hours }));
				expression = new BinaryExpression(expression, "+", new ValueExpression(":"));
				expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), minutes }));
				if (seconds != null)
				{
					expression = new BinaryExpression(expression, "+", new ValueExpression(":"));
					expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), seconds }));
					if (milliseconds != null)
					{
						expression = new BinaryExpression(expression, "+", new ValueExpression("."));
						expression = new BinaryExpression(expression, "+", new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), milliseconds }));
					}
				}
			}

			return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), expression, new ValueExpression(121) });
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			var arguments = new Expression[planNode.Nodes.Count];
			for (int index = 0; index < planNode.Nodes.Count; index++)
				arguments[index] = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[index], false);
			switch (planNode.Nodes.Count)
			{
				case 7:
					return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6]);
				case 6:
					return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], null);
				case 5:
					return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], null, null);
				case 3:
					return DateTimeSelector(arguments[0], arguments[1], arguments[2], null, null, null, null);
				default:
					return null;
			}
		}
	}

	#endregion
}