using System;
using Alphora.Dataphor.DAE.Device.SQL;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Language.SQL;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Language.PGSQL;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Device.PGSQL
{
	/// <summary> Works around the fact that as of PostgreSQL 9.1, UNION is not optimized at all, but UNION ALL is.</summary>
	public class PostgreSQLUnion : SQLUnion
	{
		public PostgreSQLUnion(int iD, string name) : base(iD, name) { }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			var unionStatement = (QueryExpression)base.Translate(devicePlan, planNode);
			if (localDevicePlan.IsSupported)
			{
				// Use "union all" rather than "union"
				foreach (TableOperatorExpression tableOperatorExpression in unionStatement.TableOperators)
					tableOperatorExpression.Distinct = false;

				// Nest union and make it distinct
				var wrapper = localDevicePlan.Device.NestQueryExpression(localDevicePlan, ((TableNode)planNode.Nodes[0]).TableVar, unionStatement);
				wrapper.SelectClause.Distinct = true;

				return wrapper;
			}
			else
				return unionStatement;
		}
	}

	public class PostgreSQLRetrieve : SQLRetrieve
	{
		public PostgreSQLRetrieve(int iD, string name) : base(iD, name) { }

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			SQLDevicePlan localDevicePlan = (SQLDevicePlan)devicePlan;
			var tableVarNode = (TableVarNode)planNode;
			var tableVar = (tableVarNode).TableVar;

			if (tableVar is BaseTableVar)
			{
				SQLRangeVar rangeVar = new SQLRangeVar(localDevicePlan.GetNextTableAlias());
				localDevicePlan.CurrentQueryContext().RangeVars.Add(rangeVar);
				var selectExpression = new Language.PGSQL.SelectExpression();
				string sqlIdentifier = localDevicePlan.Device.ToSQLIdentifier(tableVar);
				string tag = D4.MetaData.GetTag(tableVar.MetaData, "Storage.Schema", localDevicePlan.Device.Schema);
				var tableExpression = new TableExpression(tag, sqlIdentifier);
				var tableSpecifier = new TableSpecifier(tableExpression, rangeVar.Name);
				selectExpression.FromClause = new AlgebraicFromClause(tableSpecifier);
				selectExpression.SelectClause = new SelectClause();
				foreach (TableVarColumn column in tableVar.Columns)
				{
					SQLRangeVarColumn rangeVarColumn = new SQLRangeVarColumn(column, rangeVar.Name, localDevicePlan.Device.ToSQLIdentifier(column), localDevicePlan.Device.ToSQLIdentifier(column.Name));
					rangeVar.Columns.Add(rangeVarColumn);
					selectExpression.SelectClause.Columns.Add(rangeVarColumn.GetColumnExpression());
				}
				if
				(
					tableVarNode.Supports(CursorCapability.Updateable)
						&&
						(
							SQLTable.CursorIsolationToIsolationLevel(tableVarNode.CursorIsolation, devicePlan.Plan.ServerProcess.CurrentIsolationLevel())
								== IsolationLevel.Isolated
						)
				)
					selectExpression.ForSpecifier = ForSpecifier.Update;

				selectExpression.SelectClause.Distinct =
					(tableVar.Keys.Count == 1) &&
					Convert.ToBoolean(D4.MetaData.GetTag(tableVar.Keys[0].MetaData, "Storage.IsImposedKey", "false"));

				return selectExpression;
			}
			else
				return localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			// return base.Translate(ADevicePlan, APlanNode);
		}

	}

	public class PostgreSQLToday : SQLDeviceOperator
	{
		public PostgreSQLToday(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			return new CallExpression("Round", new Expression[] { new CallExpression("GetDate", new Expression[] {}), new ValueExpression(0), new ValueExpression(1) });
		}
	}

	public class PostgreSQLSubString : SQLDeviceOperator
	{
		public PostgreSQLSubString(int iD, string name)
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
	public class PostgreSQLPos : SQLDeviceOperator
	{
		public PostgreSQLPos(int iD, string name)
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
	public class PostgreSQLIndexOf : SQLDeviceOperator
	{
		public PostgreSQLIndexOf(int iD, string name)
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

	// CompareText(ALeftValue, ARightValue) ::= case when Upper(ALeftValue) = Upper(ARightValue) then 0 when Upper(ALeftValue) < Upper(ARightValue) then -1 else 1 end
	public class PostgreSQLCompareText : SQLDeviceOperator
	{
		public PostgreSQLCompareText(int iD, string name)
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
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) }),
								"iEqual",
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false) })
							),
							new ValueExpression(0)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) }),
								"iLess",
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false) })
							),
							new ValueExpression(-1)
						),
						new CaseItemExpression
						(
							new BinaryExpression
							(
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) }),
								"iGreater",
								new CallExpression("Upper", new[] { localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false) })
							),
							new ValueExpression(1)
						)
					},
					new CaseElseExpression(new ValueExpression(null, TokenType.Nil))
				);
		}
	}


	// ToString(AValue) ::= Convert(varchar, AValue)
	public class PostgreSQLToString : SQLDeviceOperator
	{
		public PostgreSQLToString(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return new CallExpression("Convert", new[] { new IdentifierExpression("varchar"), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) });
		}
	}

	public class PostgreSQLToBit : SQLDeviceOperator
	{
		public PostgreSQLToBit(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return new CallExpression("Convert", new[] { new IdentifierExpression("bit"), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) });
		}
	}

	public class PostgreSQLToTinyInt : SQLDeviceOperator
	{
		public PostgreSQLToTinyInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return new CallExpression("Convert", new[] { new IdentifierExpression("tinyint"), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) });
		}
	}

	// ToByte(AValue) ::= convert(tinyint, AValue & (power(2, 8) - 1))	
	public class PostgreSQLToByte : SQLDeviceOperator
	{
		public PostgreSQLToByte(int iD, string name)
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

	public class PostgreSQLToSmallInt : SQLDeviceOperator
	{
		public PostgreSQLToSmallInt(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			return new CallExpression("Convert", new[] { new IdentifierExpression("smallint"), localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false) });
		}
	}

	// ToSByte(AValue) ::= convert(smallint, ((AValue & (power(2, 8) - 1) & ~power(2, 7)) - (power(2, 7) & AValue)))
	public class PostgreSQLToSByte : SQLDeviceOperator
	{
		public PostgreSQLToSByte(int iD, string name)
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
	public class PostgreSQLToShort : SQLDeviceOperator
	{
		public PostgreSQLToShort(int iD, string name)
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

	public class PostgreSQLToInt : SQLDeviceOperator
	{
		public PostgreSQLToInt(int iD, string name)
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
	public class PostgreSQLToUShort : SQLDeviceOperator
	{
		public PostgreSQLToUShort(int iD, string name)
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
	public class PostgreSQLToInteger : SQLDeviceOperator
	{
		public PostgreSQLToInteger(int iD, string name)
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

	public class PostgreSQLToBigInt : SQLDeviceOperator
	{
		public PostgreSQLToBigInt(int iD, string name)
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
	public class PostgreSQLToUInteger : SQLDeviceOperator
	{
		public PostgreSQLToUInteger(int iD, string name)
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
	public class PostgreSQLToLong : SQLDeviceOperator
	{
		public PostgreSQLToLong(int iD, string name)
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

	public class PostgreSQLToDecimal20 : SQLDeviceOperator
	{
		public PostgreSQLToDecimal20(int iD, string name)
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

	public class PostgreSQLToDecimal288 : SQLDeviceOperator
	{
		public PostgreSQLToDecimal288(int iD, string name)
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
	public class PostgreSQLToULong : SQLDeviceOperator
	{
		public PostgreSQLToULong(int iD, string name)
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

	public class PostgreSQLToDecimal : SQLDeviceOperator
	{
		public PostgreSQLToDecimal(int iD, string name)
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

	public class PostgreSQLToMoney : SQLDeviceOperator
	{
		public PostgreSQLToMoney(int iD, string name)
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

	public class PostgreSQLToUniqueIdentifier : SQLDeviceOperator
	{
		public PostgreSQLToUniqueIdentifier(int iD, string name)
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
	public class PostgreSQLMath
	{
		public static Expression Truncate(Expression expression)
		{
			return new CallExpression("Round", new[] { expression, new ValueExpression(0), new ValueExpression(1) });
		}

		public static Expression Frac(Expression expression, Expression expressionCopy)
		// note that it takes two different refrences to the same value
		{
			Expression rounded = new CallExpression("Round", new[] { expressionCopy, new ValueExpression(0), new ValueExpression(1) });
			return new BinaryExpression(expression, "iSubtraction", rounded);
		}
	}

	public class PostgreSQLTimeSpan
	{
		public static Expression ReadMillisecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(10000000));
			Expression fromFrac = PostgreSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(1000));
			return PostgreSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadSecond(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(600000000));
			Expression fromFrac = PostgreSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return PostgreSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadMinute(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(36000000000));
			Expression fromFrac = PostgreSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(60));
			return PostgreSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadHour(Expression tempValue)
		{
			Expression toFrac = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression toFracCopy = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			Expression fromFrac = PostgreSQLMath.Frac(toFrac, toFracCopy);
			Expression toTrunc = new BinaryExpression(fromFrac, "iMultiplication", new ValueExpression(24));
			return PostgreSQLMath.Truncate(toTrunc);
		}

		public static Expression ReadDay(Expression tempValue)
		{
			Expression toTrunc = new BinaryExpression(tempValue, "iDivision", new ValueExpression(864000000000));
			return PostgreSQLMath.Truncate(toTrunc);
		}
	}

	public class PostgreSQLDateTimeFunctions
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

	// Operators that PostgreSQL doesn't have.  7.0 doesn't support user-defined functions, so they will be inlined here.

	// Math
	public class PostgreSQLPower : SQLDeviceOperator
	{
		public PostgreSQLPower(int iD, string name)
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

	public class PostgreSQLTruncate : SQLDeviceOperator
	{
		public PostgreSQLTruncate(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLMath.Truncate(tempValue);
		}
	}

	public class PostgreSQLFrac : SQLDeviceOperator
	{
		public PostgreSQLFrac(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression valueCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLMath.Frac(tempValue, valueCopy);
		}
	}

	public class PostgreSQLLogB : SQLDeviceOperator
	{
		public PostgreSQLLogB(int iD, string name)
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
	public class PostgreSQLTimeSpanReadMillisecond : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanReadMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLTimeSpan.ReadMillisecond(tempValue);
		}
	}

	public class PostgreSQLTimeSpanReadSecond : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanReadSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLTimeSpan.ReadSecond(tempValue);
		}
	}

	public class PostgreSQLTimeSpanReadMinute : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanReadMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLTimeSpan.ReadMinute(tempValue);
		}
	}

	public class PostgreSQLTimeSpanReadHour : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanReadHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLTimeSpan.ReadHour(tempValue);
		}
	}

	public class PostgreSQLTimeSpanReadDay : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanReadDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression tempValue = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			return PostgreSQLTimeSpan.ReadDay(tempValue);
		}
	}

	public class PostgreSQLTimeSpanWriteMillisecond : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanWriteMillisecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = PostgreSQLTimeSpan.ReadMillisecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class PostgreSQLTimeSpanWriteSecond : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanWriteSecond(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = PostgreSQLTimeSpan.ReadSecond(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(10000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class PostgreSQLTimeSpanWriteMinute : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanWriteMinute(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = PostgreSQLTimeSpan.ReadMinute(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(600000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class PostgreSQLTimeSpanWriteHour : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanWriteHour(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = PostgreSQLTimeSpan.ReadHour(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(36000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}

	public class PostgreSQLTimeSpanWriteDay : SQLDeviceOperator
	{
		public PostgreSQLTimeSpanWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression timeSpan = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression timeSpanCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			Expression fromPart = PostgreSQLTimeSpan.ReadDay(timeSpanCopy);
			Expression parts = new BinaryExpression(part, "iSubtraction", fromPart);
			part = new BinaryExpression(parts, "iMultiplication", new ValueExpression(864000000000));
			return new BinaryExpression(timeSpan, "iAddition", part);
		}
	}


	public class PostgreSQLAddMonths : SQLDeviceOperator
	{
		public PostgreSQLAddMonths(int iD, string name)
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

	public class PostgreSQLAddYears : SQLDeviceOperator
	{
		public PostgreSQLAddYears(int iD, string name)
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

	public class PostgreSQLDayOfWeek : SQLDeviceOperator
	// TODO: do for removal as replaced with Storage.TranslationString in SystemCatalog.d4
	{
		public PostgreSQLDayOfWeek(int iD, string name)
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

	public class PostgreSQLDayOfYear : SQLDeviceOperator
	{
		public PostgreSQLDayOfYear(int iD, string name)
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

	public class PostgreSQLDateTimeReadHour : SQLDeviceOperator
	{
		public PostgreSQLDateTimeReadHour(int iD, string name)
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

	public class PostgreSQLDateTimeReadMinute : SQLDeviceOperator
	{
		public PostgreSQLDateTimeReadMinute(int iD, string name)
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

	public class PostgreSQLDateTimeReadSecond : SQLDeviceOperator
	{
		public PostgreSQLDateTimeReadSecond(int iD, string name)
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

	public class PostgreSQLDateTimeReadMillisecond : SQLDeviceOperator
	{
		public PostgreSQLDateTimeReadMillisecond(int iD, string name)
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

	public class PostgreSQLDateTimeWriteMillisecond : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteMillisecond(int iD, string name)
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

	public class PostgreSQLDateTimeWriteSecond : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteSecond(int iD, string name)
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

	public class PostgreSQLDateTimeWriteMinute : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteMinute(int iD, string name)
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

	public class PostgreSQLDateTimeWriteHour : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteHour(int iD, string name)
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

	public class PostgreSQLDateTimeWriteDay : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteDay(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return PostgreSQLDateTimeFunctions.WriteDay(dateTime, dateTimeCopy, part);
		}
	}

	public class PostgreSQLDateTimeWriteMonth : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteMonth(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return PostgreSQLDateTimeFunctions.WriteMonth(dateTime, dateTimeCopy, part);
		}
	}

	public class PostgreSQLDateTimeWriteYear : SQLDeviceOperator
	{
		public PostgreSQLDateTimeWriteYear(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression dateTimeCopy = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression part = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[1], false);
			return PostgreSQLDateTimeFunctions.WriteYear(dateTime, dateTimeCopy, part);
		}
	}

	public class PostgreSQLDateTimeDatePart : SQLDeviceOperator
	{
		public PostgreSQLDateTimeDatePart(int iD, string name)
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

	public class PostgreSQLDateTimeTimePart : SQLDeviceOperator
	{
		public PostgreSQLDateTimeTimePart(int iD, string name)
			: base(iD, name)
		{
		}

		public override Statement Translate(DevicePlan devicePlan, PlanNode planNode)
		{
			var localDevicePlan = (SQLDevicePlan)devicePlan;
			Expression dateTime = localDevicePlan.Device.TranslateExpression(localDevicePlan, planNode.Nodes[0], false);
			Expression fromConvert = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromConvertCopy = new CallExpression("Convert", new[] { new ValueExpression("Float", TokenType.Symbol), dateTime });
			Expression fromMath = PostgreSQLMath.Frac(fromConvert, fromConvertCopy);
			return new CallExpression("Convert", new[] { new ValueExpression("DateTime", TokenType.Symbol), dateTime });
		}
	}


	/// <summary>
	///  DateTime selector is done by constructing a string representation of the value and converting it to a datetime using style 121 (ODBC Canonical)	
	///  Convert(DateTime, Convert(VarChar, AYear) + '-' + Convert(VarChar, AMonth) + '-' + Convert(VarChar, ADay) + ' ' + Convert(VarChar, AHours) + ':' + Convert(VarChar, AMinutes) + ':' + Convert(VarChar, ASeconds) + '.' + Convert(VarChar, AMilliseconds), 121)
	/// </summary>
	public class PostgreSQLDateTimeSelector : SQLDeviceOperator
	{
		public PostgreSQLDateTimeSelector(int iD, string name)
			: base(iD, name)
		{
		}

		public static Expression DateTimeSelector(Expression year, Expression month, Expression day, Expression hours, Expression minutes, Expression seconds, Expression milliseconds)
		{
			Expression expression = new BinaryExpression( new CallExpression("Convert", new[] { new ValueExpression("VarChar", TokenType.Symbol), year }), "+", new ValueExpression("-")); 
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
				case 7: return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6]);
				case 6: return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], null);
				case 5: return DateTimeSelector(arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], null, null);
				case 3: return DateTimeSelector(arguments[0], arguments[1], arguments[2], null, null, null, null);
				default:
					return null;
			}
		}
	}

}
