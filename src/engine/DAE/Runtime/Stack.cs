/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;

	public class Stack : Stack<object>
	{
		public Stack() : base() { }
		public Stack(int maxStackDepth, int maxCallDepth) : base(maxStackDepth, maxCallDepth) { }

		/// <summary>Stores a value in a slot, releasing the value it displaces.</summary>
		/// <remarks>
		/// <para>
		/// A stack slot owns the value in it. The engine's large and structured values - a Binary or a Graphic, which
		/// live in a stream the server holds, and rows, lists and tables - are not reclaimed by the garbage collector:
		/// something has to release them, and for a variable that something is its slot. Overwriting a slot with a
		/// plain Poke therefore orphans whatever was in it, which for a stream is memory held until the session ends.
		/// Use this wherever a slot's own value is being replaced, so that the store and the release cannot drift
		/// apart the way they had: assignment used a plain Poke and leaked every value it replaced
		/// (bug-assigning-a-binary-variable-leaks-its-stream).
		/// </para>
		/// <para>
		/// Not every Poke is a replacement, and the sites that are not say so where they are: one filling a slot it
		/// has just pushed as null, one handing a value back to a slot that does not own it (the var operand
		/// write-back in InstructionNodeBase.CleanupOperand), and the iteration slots of the foreach, list and
		/// aggregate nodes, which borrow a row the iterator owns and release nothing.
		/// </para>
		/// <para>
		/// Nothing is released when the slot already holds the value being stored, so that a slot cannot release the
		/// value it is being given. StreamID is a struct, so two boxes can carry one stream identifier and reference
		/// equality alone would miss that.
		/// </para>
		/// <para>
		/// The whole picture this rule is one rule of - slots, rows and lists - is in docs/architecture/value-lifetime.md;
		/// operands of each modifier, and how a plan node says it is lending a value rather than giving it, are in
		/// docs/architecture/operand-ownership.md.
		/// </para>
		/// </remarks>
		public void PokeOwned(int offset, object value, IValueManager manager)
		{
			object displaced = this[offset];
			Poke(offset, value);
			if (!IsSameValue(displaced, value))
				DataValue.DisposeValue(manager, displaced);
		}

		/// <summary>Releases the values of the slots above the given depth, leaving each slot null.</summary>
		/// <remarks>
		/// <para>
		/// The rule a scope follows on the way out: it releases what it finds above the depth the stack had when it was
		/// entered. Count is window-relative and is read the same way at entry and at exit, so the two depths are
		/// comparable; a scope therefore needs no list of its own variables and no agreement with the compiler about
		/// where they sit. That is what makes this right for a block that raised before it had declared all of its
		/// variables, and for an operator body whose compile-time symbol frame does not line up with its run-time frame.
		/// </para>
		/// <para>
		/// Each slot is nulled before its value is disposed, so a dispose that throws cannot leave a disposed value
		/// reachable from the stack, and a second pass over the same region releases nothing. See PokeOwned for the
		/// ownership rule this is the other half of: a slot owns its value, and whatever takes a value out of a slot -
		/// an assignment, or the scope that ends - releases it.
		/// </para>
		/// <para>
		/// NOTE: the entry depth and this call must be read with AllowExtraWindowAccess in the same state, because Count
		/// is window-relative only while it is off. Every caller today reads and releases with it off; one that recorded a
		/// depth outside an extra-window-access region and released inside one would walk below its own window and release
		/// another window's values.
		/// </para>
		/// </remarks>
		public void ReleaseAbove(int depth, IValueManager manager)
		{
			int count = Count;
			for (int offset = 0; offset < count - depth; offset++)
			{
				object value = this[offset];
				if (value != null)
				{
					Poke(offset, null);
					DataValue.DisposeValue(manager, value);
				}
			}
		}

		/// <summary>Whether two slot values name the same underlying value, comparing stream identifiers by value and everything else by reference.</summary>
		private static bool IsSameValue(object left, object right)
		{
			if ((left is StreamID) && (right is StreamID))
				return ((StreamID)left).Value == ((StreamID)right).Value;
			return Object.ReferenceEquals(left, right);
		}

		public override void PushWindow(int count)
		{
			_windows.Push(new RuntimeStackWindow(_count - count));
		}
		
		public void PushWindow(int count, PlanNode originator, DebugLocator locator)
		{
			_windows.Push(new RuntimeStackWindow(_count - count, originator, locator));
		}
		
		protected object _errorVar;
		public object ErrorVar
		{
			get { return _errorVar; }
			set { _errorVar = value; }
		}
	}
}

