/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// Which operator nodes sit outside the engine's own operand cleanup, checked rather than counted by hand.
	/// </summary>
	/// <remarks>
	/// An instruction node whose arguments are cleaned up for it does so through one of the five argument shapes -
	/// InstructionNode and its nilary, unary, binary and ternary siblings - each of which calls
	/// InstructionNodeBase.CleanupOperand from its own InternalExecute(Program). A node that overrides that entry point
	/// itself is never reached by it and owes the const-operand rule by hand: release the value an argument node gave
	/// it, leave alone the value an argument node lent it. The rule, and what each node on the list below does about
	/// it, are in docs/architecture/operand-ownership.md.
	///
	/// Nothing used to check the set. It was counted by hand into that page, counted wrong, and the two nodes the
	/// count missed were leaking a managed stream per stream-backed column per side on every row comparison
	/// (bug-row-equality-leaks-the-rows-it-compares). The nine that re-override the entry point from one of the five
	/// shapes are the dangerous ones: they read as ordinary instruction nodes while skipping their own shape's call to
	/// the cleanup. So the set is reflected over the engine assembly and compared with the list below, and a node that
	/// joins or leaves it has to say which here.
	///
	/// NOTE: the engine assembly is all this reaches, and a library's nodes are compiled into their own assembly, which
	/// the engine loads only when that library is registered. Three nodes of this shape live outside it today -
	/// InsertNode, CopyNode and CopyTablesNode in libraries/System.Integration/Source/IntegrationNodes.cs - and none of
	/// them has anything stream-backed to release: their operands are tables the node never evaluates as values, plus
	/// String and Boolean arguments, whose natives the garbage collector reclaims. Widen this to the library assemblies
	/// when one of those nodes takes an operand of a type with a stream behind it - a Binary or a Graphic, or a row or
	/// list holding one.
	///
	/// No server and no base class: this reads types, not behaviour.
	/// </remarks>
	[TestFixture]
	public class OperandOwnershipShapeTest
	{
		/// <summary>
		/// Every non-table node that declares InternalExecute(Program) itself, reviewed against the const-operand rule.
		/// The five argument shapes are on it because the entry point is theirs to declare - they are where the cleanup
		/// is called from - and the twelve after them because each was read and found to honour the rule or to have no
		/// operand modifiers in play.
		/// </summary>
		private static readonly string[] ReviewedNodes =
		[
			"BinaryInstructionNode",
			"CompoundScalarEqualNode",
			"ExistsNode",
			"IfNilNode",
			"InstructionNode",
			"NilaryInstructionNode",
			"RowEqualNode",
			"RowExtendNode",
			"TableEqualNode",
			"TableGreaterNode",
			"TableInclusiveGreaterNode",
			"TableInclusiveLessNode",
			"TableLessNode",
			"TableNotEqualNode",
			"TableToListNode",
			"TernaryInstructionNode",
			"UnaryInstructionNode"
		];

		[Test]
		public void TheNodesOutsideOperandCleanupAreTheOnesReviewedForIt()
		{
			var found = NodesDeclaringTheExecuteEntryPoint().Select(type => type.Name).ToList();

			var unlisted = found.Where(name => !ReviewedNodes.Contains(name)).OrderBy(name => name).ToList();
			var stale = ReviewedNodes.Where(name => !found.Contains(name)).OrderBy(name => name).ToList();
			var shared = found.GroupBy(name => name).Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(name => name).ToList();

			Assert.Multiple
			(
				() =>
				{
					Assert.That
					(
						unlisted,
						Is.Empty,
						String.Join(", ", unlisted)
							+ ": overriding InternalExecute(Program) puts a node outside InstructionNodeBase.CleanupOperand, so it"
							+ " owes the const-operand rule by hand - release the value an argument node gave it, leave alone the"
							+ " value an argument node lent it, and reach base.DetermineDataType so the question is asked at all."
							+ " The rule is in docs/architecture/operand-ownership.md. Apply it, then add the node to"
							+ " ReviewedNodes in this fixture and to that page."
					);

					// The list is of simple names, so that it reads as the set rather than as seventeen namespace-qualified
					// strings. Two engine nodes sharing one simple name would then match the list while only one of them had
					// been reviewed, which is what this catches.
					Assert.That
					(
						shared,
						Is.Empty,
						String.Join(", ", shared)
							+ ": named twice among the nodes declaring InternalExecute(Program), so this fixture's list of simple"
							+ " names cannot say which of them was reviewed. Compare full names instead."
					);

					Assert.That
					(
						stale,
						Is.Empty,
						String.Join(", ", stale)
							+ ": on this fixture's reviewed list but no longer declaring InternalExecute(Program), so the rule now"
							+ " applies to it through its argument shape rather than by hand. Remove it from ReviewedNodes and from"
							+ " docs/architecture/operand-ownership.md."
					);
				}
			);
		}

		/// <summary>
		/// The instruction nodes that take the execute entry point over from their argument shape, which is what skips
		/// the cleanup. Table nodes are left out: one overrides that entry point because returning a cursor is what a
		/// table node's execute does, and a table node has no operand modifiers in play.
		/// </summary>
		private static IEnumerable<Type> NodesDeclaringTheExecuteEntryPoint()
		{
			return
				typeof(InstructionNodeBase).Assembly.GetTypes()
					.Where
					(
						type =>
							typeof(InstructionNodeBase).IsAssignableFrom(type)
								&& !typeof(TableNode).IsAssignableFrom(type)
								&& type.GetMethod
									(
										"InternalExecute",
										BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
										null,
										new Type[] { typeof(Program) },
										null
									) != null
					);
		}
	}
}
