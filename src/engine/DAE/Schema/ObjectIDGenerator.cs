/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;

using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	/// <summary>Hands out the surrogate keys of the objects of one catalog.</summary>
	/// <remarks>
	/// <para>
	/// Every object in a catalog carries an integer identifier, used as its surrogate key in the catalog and in
	/// the catalog store. Identifiers belong to the catalog, not to the program: two catalogs number their objects
	/// independently, and an identifier is only meaningful against the catalog that handed it out.
	/// </para>
	/// <para>
	/// NOTE: a generator belongs to an <c>Engine</c> and not to a <c>Schema.Catalog</c>. <c>Engine.ClearCatalog</c>
	/// replaces the whole catalog, so a generator held on the catalog would start again from one on every catalog
	/// rebuild and hand the rebuilt catalog identifiers the engine had already used.
	/// </para>
	/// <para>
	/// An engine that persists its catalog creates a handful of built-in objects programmatically before it reads
	/// its store, and those must land on the same identifiers the store already holds for them; that is what
	/// <see cref="Reset"/> is for, and why it is called at a fixed point in catalog initialization.
	/// </para>
	/// </remarks>
	public sealed class ObjectIDGenerator
	{
		/// <summary>The last identifier allocated; zero before the first allocation, so zero is never allocated.</summary>
		private int _lastID;

		/// <summary>Serializes the methods that set the counter outright against each other.</summary>
		/// <remarks>
		/// Allocation stays lock-free, because the processes of one engine compile concurrently and every schema
		/// object they create allocates. The methods that set the counter run during engine start-up and catalog
		/// rebuild, when nothing is allocating, so they are deliberately not serialized against <see cref="Allocate"/>.
		/// </remarks>
		private readonly object _syncRoot = new object();

		/// <summary>Returns an identifier no object in this catalog has been given.</summary>
		public int Allocate()
		{
			int iD = Interlocked.Increment(ref _lastID);

			// Past Int32.MaxValue the increment wraps negative, and -1 is the sentinel for "no parent object"
			// (Schema.Object.ParentObjectID), so a wrap would corrupt the catalog silently. Exhaustion is not a
			// practical concern - at the few dozen identifiers a statement consumes it needs on the order of a
			// hundred million statements in one catalog - but it must not be silent.
			if (iD < 1)
			{
				// Hold the counter at the top rather than let it climb back through the identifiers already in use:
				// an increment that wrapped once would otherwise reach 1 again after another two billion calls and
				// hand out identifiers the catalog already holds, which is the silent corruption this refuses.
				Interlocked.Exchange(ref _lastID, Int32.MaxValue);
				throw new SchemaException(SchemaException.Codes.ObjectIDsExhausted);
			}

			return iD;
		}

		/// <summary>Honours a DAE.ObjectID tag in the given meta data, removing it; allocates when there is none.</summary>
		/// <remarks>
		/// NOTE: a tag-supplied identifier does not raise the generator, as it never has. Every path that loads
		/// objects carrying these tags today is a catalog store load, and <c>Server.LoadServerState</c> raises the
		/// generator past the store's maximum afterwards. A new load path that does not would let the generator
		/// reissue an identifier a loaded object already holds, so it must raise the generator itself.
		/// </remarks>
		public int Allocate(MetaData metaData)
		{
			Tag tag = MetaData.RemoveTag(metaData, "DAE.ObjectID");
			if (tag != Tag.None)
				return Int32.Parse(tag.Value);
			return Allocate();
		}

		/// <summary>Starts again from one, for an engine whose built-in objects must be numbered the same on every load of its catalog store.</summary>
		public void Reset()
		{
			lock (_syncRoot)
			{
				_lastID = 0;
			}
		}

		/// <summary>Sets the last allocated identifier outright, which is the one operation that can move the counter backwards.</summary>
		/// <remarks>
		/// NOTE: for the transition only. It exists because <c>Schema.Object.SetNextObjectID</c> is still on the
		/// public surface and two tests save the counter and put it back; nothing else may move the counter down.
		/// It goes with that method, in bug-object-id-counter-is-process-wide.
		/// </remarks>
		internal void SetLastAllocated(int iD)
		{
			lock (_syncRoot)
			{
				_lastID = iD;
			}
		}

		/// <summary>Ensures no identifier at or below the given one is allocated, for a catalog loaded from a store.</summary>
		/// <remarks>
		/// This raises the generator and never lowers it: a store whose maximum identifier is below the count of
		/// built-in objects the engine has already created - an empty or first-run catalog - must leave the
		/// generator above those objects.
		/// </remarks>
		public void EnsureAllocatedThrough(int iD)
		{
			lock (_syncRoot)
			{
				if (_lastID < iD)
					_lastID = iD;
			}
		}
	}
}
