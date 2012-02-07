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
		public Set(IEnumerable<T> enumerable)
		{
			foreach (T item in enumerable)
				Add(item);
		}
		
		private Dictionary<T, bool> _dictionary = new Dictionary<T,bool>();
		
		public void Add(T item)
		{
			_dictionary.Add(item, false);
		}

		public void Clear()
		{
			_dictionary.Clear();
		}

		public bool Contains(T item)
		{
			return _dictionary.ContainsKey(item);
		}

		public void CopyTo(T[] array, int index)
		{
			_dictionary.Keys.CopyTo(array, index);
		}

		public int Count
		{
			get { return _dictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return _dictionary.Remove(item);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _dictionary.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _dictionary.Keys.GetEnumerator();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _dictionary.Keys.GetEnumerator();
		}

		public T[] ToArray()
		{
			T[] result = new T[Count];
			var index = 0;
			foreach (T item in _dictionary.Keys)
			{
				result[index] = item;
				index++;
			}
			return result;
		}
	}

	internal class SetDebugView<T>
	{
		public SetDebugView(Set<T> set)
		{
			if (set == null)
				throw new ArgumentNullException("set");
			_set = set;
		}

		private Set<T> _set;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get { return _set.ToArray(); }
		}
	}
}
