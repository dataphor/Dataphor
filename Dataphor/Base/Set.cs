using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using System;

// TODO: Reduce overhead; replace internal hashtable

namespace Alphora.Dataphor
{
	/// <summary> Hash table based unique set. </summary>
	/// <remarks> Useful for quick insertion, removal, and inclusion queries. </remarks>
	[DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(SetDebugView<>))]
	public class Set<T> : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		public Set() { }
		public Set(IEnumerable<T> AEnumerable)
		{
			foreach (T LItem in AEnumerable)
				Add(LItem);
		}
		
		private Dictionary<T, bool> FDictionary = new Dictionary<T,bool>();
		
		public void Add(T AItem)
		{
			FDictionary.Add(AItem, false);
		}

		public void Clear()
		{
			FDictionary.Clear();
		}

		public bool Contains(T AItem)
		{
			return FDictionary.ContainsKey(AItem);
		}

		public void CopyTo(T[] AArray, int AIndex)
		{
			FDictionary.Keys.CopyTo(AArray, AIndex);
		}

		public int Count
		{
			get { return FDictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T AItem)
		{
			return FDictionary.Remove(AItem);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return FDictionary.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return FDictionary.Keys.GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return FDictionary.Keys.GetEnumerator();
		}

		public T[] ToArray()
		{
			T[] LResult = new T[Count];
			var LIndex = 0;
			foreach (T LItem in FDictionary.Keys)
			{
				LResult[LIndex] = LItem;
				LIndex++;
			}
			return LResult;
		}
	}

	internal class SetDebugView<T>
	{
		public SetDebugView(Set<T> ASet)
		{
			if (ASet == null)
				throw new ArgumentNullException("set");
			FSet = ASet;
		}

		private Set<T> FSet;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get { return FSet.ToArray(); }
		}
	}
}
