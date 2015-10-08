/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using TableExpression = Alphora.Dataphor.DAE.Language.TSQL.TableExpression;

namespace Alphora.Dataphor.DAE.Device.MSSQL
{

	#region Operators

	public class MSSQLAggregateOperator : SQLAggregateOperator
	{
		public MSSQLAggregateOperator(int iD, string name) : base(iD, name) { }
		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			AggregateCallNode node = (AggregateCallNode)planNode;
			if (node.SourceNode.GetType() == typeof(OrderNode))
				localDevicePlan.IsSupported = false;
			return base.Translate(devicePlan, planNode);
		}
	}

	public class MSSQLRetrieve : SQLDeviceOperator
	{
		public MSSQLRetrieve(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			var tableVarNode = (TableVarNode)planNode;
			TableVar tableVar = tableVarNode.TableVar;

			if (tableVar is BaseTableVar)
			{
				var rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				foreach (TableVarColumn column in tableVar.Columns)
					rangeVar.Columns.Add(new SQLRangeVarColumn(column, rangeVar.Name,
						localDevicePlan.Device.ToSQLIdentifier(column),
						localDevicePlan.Device.ToSQLIdentifier(column.Name)));
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				var selectExpression = new SelectExpression();
				// TODO: Load-time binding resolution of updlock optimizer hint: The current assumption is that if no cursor isolation level is specified and the cursor is updatable then udpate locks should be taken. 
				// If we had a load-time binding step then the decision to take update locks could be deferred until we are certain that the query will run in an isolated transaction.
				selectExpression.FromClause =
					new AlgebraicFromClause
					(
						new TableSpecifier
						(
							new TableExpression
							(
								MetaData.GetTag(tableVar.MetaData, "Storage.Schema", localDevicePlan.Device.Schema),
								localDevicePlan.Device.ToSQLIdentifier(tableVar),
#if USEFASTFIRSTROW
								(
									tableVarNode.Supports(CursorCapability.Updateable) && 
									(
										(SQLTable.CursorIsolationToIsolationLevel(tableVarNode.CursorIsolation, ADevicePlan.Plan.ServerProcess.CurrentIsolationLevel()) == DAE.IsolationLevel.Isolated)
									) ? 
									"(fastfirstrow, updlock)" : 
									"(fastfirstrow)"
								)
#else
								(
									tableVarNode.Supports(CursorCapability.Updateable) &&
									(
										(SQLTable.CursorIsolationToIsolationLevel(tableVarNode.CursorIsolation, devicePlan.Plan.ServerProcess.CurrentIsolationLevel()) ==
											 IsolationLevel.Isolated)
									)
										?
											"(updlock)"
										:
											""
								)
#endif
							),
							rangeVar.Name
						)
					);

				selectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn column in tableVar.Columns)
					selectExpression.SelectClause.Columns.Add(
						localDevicePlan.GetRangeVarColumn(column.Name, true).GetColumnExpression());

				selectExpression.SelectClause.Distinct =
					(tableVar.Keys.Count == 1) &&
					Convert.ToBoolean(MetaData.GetTag(tableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));
				// TODO: Fix this in the DB2 and base SQL devices !!!

				return selectExpression;
			}
			else
				return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
		}
	}

	public class MSSQLToday : SQLDeviceOperator
	{
		public MSSQLToday(int iD, string name)
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

	public class MSSQLSubString : SQLDeviceOperator
	{
		public MSSQLSubString(int iD, string name)
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
	public class MSSQLPos : SQLDeviceOperator
	{
		public MSSQLPos(int iD, string name)
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
	public class MSSQLIndexOf : SQLDeviceOperator
	{
		public MSSQLIndexOf(int iD, string name)
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
	public class MSSQLCompareText : SQLDeviceOperator
	{
		public MSSQLCompareText(int iD, string name)
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
	public class MSSQLToString : SQLDeviceOperator
	{
		public MSSQLToString(int iD, string name)
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

	public class MSSQLToBit : SQLDeviceOperator
	{
		public MSSQLToBit(int iD, string name)
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

	public class MSSQLToTinyInt : SQLDeviceOperator
	{
		public MSSQLToTinyInt(int iD, string name)
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
	public class MSSQLToByte : SQLDeviceOperator
	{
		public MSSQLToByte(int iD, string name)
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

	public class MSSQLToSmallInt : SQLDeviceOperator
	{
		public MSSQLToSmallInt(int iD, string name)
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
	public class MSSQLToSByte : SQLDeviceOperator
	{
		public MSSQLToSByte(int iD, string name)
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
	public class MSSQLToShort : SQLDeviceOperator
	{
		public MSSQLToShort(int iD, string name)
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

	public class MSSQLToInt : SQLDeviceOperator
	{
		public MSSQLToInt(int iD, string name)
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
	public class MSSQLToUShort : SQLDeviceOperator
	{
		public MSSQLToUShort(int iD, string name)
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
	public class MSSQLToInteger : SQLDeviceOperator
	{
		public MSSQLToInteger(int iD, string name)
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

	public class MSSQLToBigInt : SQLDeviceOperator
	{
		public MSSQLToBigInt(int iD, string name)
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
	public class MSSQLToUInteger : SQLDeviceOperator
	{
		public MSSQLToUInteger(int iD, string name)
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
	public class MSSQLToLong : SQLDeviceOperator
	{
		public MSSQLToLong(int iD, string name)
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

	public class MSSQLToDecimal20 : SQLDeviceOperator
	{
		public MSSQLToDecimal20(int iD, string name)
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

	public class MSSQLToDecimal288 : SQLDeviceOperator
	{
		public MSSQLToDecimal288(int iD, string name)
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
	public class MSSQLToULong : SQLDeviceOperator
	{
		public MSSQLToULong(int iD, string name)
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

	public class MSSQLToDecimal : SQLDeviceOperator
	{
		public MSSQLToDecimal(int iD, string name)
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

	public class MSSQLToMoney : SQLDeviceOperator
	{
		public MSSQLToMoney(int iD, string name)
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

	public class MSSQLToUniqueIdentifier : SQLDeviceOperator
	{
		public MSSQLToUniqueIdentifier(int iD, string name)
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
	public class MSSQLMath
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

	public class MSSQLTimeSpan
	{
		public static Expression ReadMillisecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression fromFrac = MSSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(1000));
			return MSSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadSecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression fromFrac = MSSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return MSSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadMinute(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression fromFrac = MSSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return MSSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadHour(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression fromFrac = MSSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(24));
			return MSSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadDay(Expression tempValue)
		{
			Expression toTrunc = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			return MSSQLMath.Truncate(toTrunc);
		}
	}

	public class MSSQLDateTimeFunctions
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

	// Operators that MSSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

	// Math
	public class MSSQLPower : SQLDeviceOperator
	{
		public MSSQLPower(int iD, string name)
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

	public class MSSQLTruncate : SQLDeviceOperator
	{
		public MSSQLTruncate(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLMath.Truncate(tempValue);
		}
	}

	public class MSSQLFrac : SQLDeviceOperator
	{
		public MSSQLFrac(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression valueCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLMath.Frac(tempValue, valueCopy);
		}
	}

	public class MSSQLLogB : SQLDeviceOperator
	{
		public MSSQLLogB(int iD, string name)
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
	public class MSSQLTimeSpanReadMillisecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanReadMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadMillisecond(tempValue);
		}
	}

	public class MSSQLTimeSpanReadSecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanReadSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadSecond(tempValue);
		}
	}

	public class MSSQLTimeSpanReadMinute : SQLDeviceOperator
	{
		public MSSQLTimeSpanReadMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadMinute(tempValue);
		}
	}

	public class MSSQLTimeSpanReadHour : SQLDeviceOperator
	{
		public MSSQLTimeSpanReadHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadHour(tempValue);
		}
	}

	public class MSSQLTimeSpanReadDay : SQLDeviceOperator
	{
		public MSSQLTimeSpanReadDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return MSSQLTimeSpan.ReadDay(tempValue);
		}
	}

	public class MSSQLTimeSpanWriteMillisecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = MSSQLTimeSpan.ReadMillisecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class MSSQLTimeSpanWriteSecond : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = MSSQLTimeSpan.ReadSecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class MSSQLTimeSpanWriteMinute : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = MSSQLTimeSpan.ReadMinute(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(600000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class MSSQLTimeSpanWriteHour : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = MSSQLTimeSpan.ReadHour(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(36000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class MSSQLTimeSpanWriteDay : SQLDeviceOperator
	{
		public MSSQLTimeSpanWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = MSSQLTimeSpan.ReadDay(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(864000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}


	public class MSSQLAddMonths : SQLDeviceOperator
	{
		public MSSQLAddMonths(int iD, string name)
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

	public class MSSQLAddYears : SQLDeviceOperator
	{
		public MSSQLAddYears(int iD, string name)
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

	public class MSSQLDayOfWeek : SQLDeviceOperator
	// TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
	{
		public MSSQLDayOfWeek(int iD, string name)
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

	public class MSSQLDayOfYear : SQLDeviceOperator
	{
		public MSSQLDayOfYear(int iD, string name)
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

	public class MSSQLDateTimeReadHour : SQLDeviceOperator
	{
		public MSSQLDateTimeReadHour(int iD, string name)
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

	public class MSSQLDateTimeReadMinute : SQLDeviceOperator
	{
		public MSSQLDateTimeReadMinute(int iD, string name)
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

	public class MSSQLDateTimeReadSecond : SQLDeviceOperator
	{
		public MSSQLDateTimeReadSecond(int iD, string name)
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

	public class MSSQLDateTimeReadMillisecond : SQLDeviceOperator
	{
		public MSSQLDateTimeReadMillisecond(int iD, string name)
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

	public class MSSQLDateTimeWriteMillisecond : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMillisecond(int iD, string name)
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

	public class MSSQLDateTimeWriteSecond : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteSecond(int iD, string name)
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

	public class MSSQLDateTimeWriteMinute : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMinute(int iD, string name)
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

	public class MSSQLDateTimeWriteHour : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteHour(int iD, string name)
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

	public class MSSQLDateTimeWriteDay : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteDay(dateTime, dateTimeCopy, part);
		}
	}

	public class MSSQLDateTimeWriteMonth : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteMonth(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteMonth(dateTime, dateTimeCopy, part);
		}
	}

	public class MSSQLDateTimeWriteYear : SQLDeviceOperator
	{
		public MSSQLDateTimeWriteYear(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return MSSQLDateTimeFunctions.WriteYear(dateTime, dateTimeCopy, part);
		}
	}

	public class MSSQLDateTimeDatePart : SQLDeviceOperator
	{
		public MSSQLDateTimeDatePart(int iD, string name)
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

	public class MSSQLDateTimeTimePart : SQLDeviceOperator
	{
		public MSSQLDateTimeTimePart(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression fromConvert = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromConvertCopy = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromMath = MSSQLMath.Frac(fromConvert, fromConvertCopy);
			return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), dateTime });
		}
	}


	/// <summary>
	///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
	///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
	/// </summary>
	public class MSSQLDateTimeSelector : SQLDeviceOperator
	{
		public MSSQLDateTimeSelector(int iD, string name)
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