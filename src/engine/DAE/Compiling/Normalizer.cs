/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Compiling
{
	public static class Normalizer
	{
		/// <summary>
		/// Converts the given node to an equivalent logical expression in disjunctive normal form.
		/// </summary>
		/// <param name="node">The input expression.</param>
		/// <returns>A plan node representing an equivalent logical expression in disjunctive normal form.</returns>
		/// <remarks>
		/// <para>
		/// NOTE: The runtime of this algorithm is known to be exponential in the number of elementary propositions involved in
		/// the input expression. There are known methods for improving this based on the notions of equisatisfiability, rather
		/// than pure equivalence, however, these methods would have to be reviewed for applicability to 3VL logic. The methods
		/// are described in the following references:
		/// </para>
		/// <para>
		/// Paul Jackson, Daniel Sheridan: Clause Form Conversions for Boolean Circuits. In: Holger H. Hoos, David G. Mitchell (Eds.): Theory and Applications of Satisfiability Testing, 7th International Conference, SAT 2004, Vancouver, BC, Canada, May 10–13, 2004, Revised Selected Papers. Lecture Notes in Computer Science 3542, Springer 2005, pp. 183–198
		/// G.S. Tseitin: On the complexity of derivation in propositional calculus. In: Slisenko, A.O. (ed.) Structures in Constructive Mathematics and Mathematical Logic, Part II, Seminars in Mathematics (translated from Russian), pp. 115–125. Steklov Mathematical Institute (1968)
		/// </para>
		/// </remarks>
		public static PlanNode DisjunctiveNormalize(Plan plan, PlanNode node)
		{
			node = DistributeNot(plan, node);

			var instructionNode = node as InstructionNodeBase;
			if (instructionNode != null && instructionNode.Operator != null)
			{
				switch (Schema.Object.Unqualify(instructionNode.Operator.OperatorName))
				{
					case Instructions.Or : 
						instructionNode.Nodes[0] = DisjunctiveNormalize(plan, instructionNode.Nodes[0]);
						instructionNode.Nodes[1] = DisjunctiveNormalize(plan, instructionNode.Nodes[1]);
						return instructionNode;

					case Instructions.And :
						var left = DisjunctiveNormalize(plan, instructionNode.Nodes[0]);
						var right = DisjunctiveNormalize(plan, instructionNode.Nodes[1]);
						var leftClauses = CollectClauses(left, Instructions.Or);
						var rightClauses = CollectClauses(right, Instructions.Or);
						var clausesUsed = false;
						PlanNode result = null;
						foreach (var leftClause in leftClauses)
						{
							foreach (var rightClause in rightClauses)
							{
								result =
									Compiler.AppendNode
									(
										plan, 
										result, 
										Instructions.Or, 
										Compiler.AppendNode
										(
											plan, 
											clausesUsed ? leftClause.Clone() : leftClause, 
											Instructions.And, 
											clausesUsed ? rightClause.Clone() : rightClause
										)
									);

								clausesUsed = true;
							}
						}

						return result;
				}
			}

			return node;
		}

		/// <summary>
		/// Converts the given node to an equivalent logical expression in conjunctive normal form.
		/// </summary>
		/// <param name="node">The input expression.</param>
		/// <returns>A plan node representing an equivalent logical expression in conjunctive normal form.</returns>
		/// <remarks>
		/// <para>
		/// NOTE: The runtime of this algorithm is known to be exponential in the number of elementary propositions involved in
		/// the input expression. There are known methods for improving this based on the notions of equisatisfiability, rather
		/// than pure equivalence, however, these methods would have to be reviewed for applicability to 3VL logic. The methods
		/// are described in the following references:
		/// </para>
		/// <para>
		/// Paul Jackson, Daniel Sheridan: Clause Form Conversions for Boolean Circuits. In: Holger H. Hoos, David G. Mitchell (Eds.): Theory and Applications of Satisfiability Testing, 7th International Conference, SAT 2004, Vancouver, BC, Canada, May 10–13, 2004, Revised Selected Papers. Lecture Notes in Computer Science 3542, Springer 2005, pp. 183–198
		/// G.S. Tseitin: On the complexity of derivation in propositional calculus. In: Slisenko, A.O. (ed.) Structures in Constructive Mathematics and Mathematical Logic, Part II, Seminars in Mathematics (translated from Russian), pp. 115–125. Steklov Mathematical Institute (1968)
		/// </para>
		/// </remarks>
		public static PlanNode ConjunctiveNormalize(Plan plan, PlanNode node)
		{
			node = DistributeNot(plan, node);

			var instructionNode = node as InstructionNodeBase;
			if (instructionNode != null && instructionNode.Operator != null)
			{
				switch (Schema.Object.Unqualify(instructionNode.Operator.OperatorName))
				{
					case Instructions.And : 
						instructionNode.Nodes[0] = ConjunctiveNormalize(plan, instructionNode.Nodes[0]);
						instructionNode.Nodes[1] = ConjunctiveNormalize(plan, instructionNode.Nodes[1]);
						return instructionNode;

					case Instructions.Or :
						var left = ConjunctiveNormalize(plan, instructionNode.Nodes[0]);
						var right = ConjunctiveNormalize(plan, instructionNode.Nodes[1]);
						var leftClauses = CollectClauses(left, Instructions.And);
						var rightClauses = CollectClauses(right, Instructions.And);
						var clausesUsed = false;
						PlanNode result = null;
						foreach (var leftClause in leftClauses)
						{
							foreach (var rightClause in rightClauses)
							{
								result =
									Compiler.AppendNode
									(
										plan, 
										result, 
										Instructions.And, 
										Compiler.EmitBinaryNode
										(
											plan, 
											clausesUsed ? leftClause.Clone() : leftClause, 
											Instructions.Or, 
											clausesUsed ? rightClause.Clone() : rightClause
										)
									);
							}
						}

						return result;
				}
			}

			return node;
		}

		/// <summary>
		/// Distributes logical negation through conjunction or disjunction by applying DeMorgan's laws, recursively.
		/// </summary>
		/// <param name="notNode">The input logical negation.</param>
		/// <returns>A logically equivalent expression with negation distributed.</returns>
		public static PlanNode DistributeNot(Plan plan, PlanNode node)
		{
			var instructionNode = node as InstructionNodeBase;
			if (instructionNode != null && instructionNode.Operator != null && Schema.Object.Unqualify(instructionNode.Operator.OperatorName) == Instructions.Not)
			{
				var operand = instructionNode.Nodes[0] as InstructionNodeBase;
				if (operand != null && operand.Operator != null)
				{
					switch (Schema.Object.Unqualify(operand.Operator.OperatorName))
					{
						case Instructions.Not : return operand.Nodes[0];

						case Instructions.Or :
							return 
								Compiler.EmitBinaryNode
								(
									plan, 
									DistributeNot(plan, Compiler.EmitUnaryNode(plan, Instructions.Not, operand.Nodes[0])), 
									Instructions.And, 
									DistributeNot(plan, Compiler.EmitUnaryNode(plan, Instructions.Not, operand.Nodes[1]))
								);

						case Instructions.And :
							return
								Compiler.EmitBinaryNode
								(
									plan,
									DistributeNot(plan, Compiler.EmitUnaryNode(plan, Instructions.Not, operand.Nodes[0])),
									Instructions.Or,
									DistributeNot(plan, Compiler.EmitUnaryNode(plan, Instructions.Not, operand.Nodes[1]))
								);
					}
				}
			}

			return node;
		}

		/// <summary>
		/// Collects all and or or clauses in the given node.
		/// </summary>
		/// <param name="node">The node from which the clauses will be collected.</param>
		/// <param name="instruction">The instruction, and or or, to be collected.</param>
		/// <returns>A list of clauses of the given instruction.</returns>
		/// <remarks>
		/// If the given node is a binary operator of the given instruction, the left and right operands to
		/// the node will be collected, recursively. Otherwise, the node is returned as a clause.
		/// </remarks>
		public static IEnumerable<PlanNode> CollectClauses(PlanNode node, string instruction)
		{
			var clauses = new List<PlanNode>();
			CollectClauses(node, instruction, clauses);
			return clauses;
		}

		private static void CollectClauses(PlanNode node, string instruction, List<PlanNode> clauses)
		{
			var instructionNode = node as InstructionNodeBase;
			if (instructionNode != null && instructionNode.Operator != null && Schema.Object.Unqualify(instructionNode.Operator.OperatorName) == instruction)
			{
				CollectClauses(instructionNode.Nodes[0], instruction, clauses);
				CollectClauses(instructionNode.Nodes[1], instruction, clauses);
			}
			else
			{
				clauses.Add(node);
			}
		}
	}
}
