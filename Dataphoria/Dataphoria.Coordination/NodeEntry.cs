/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	public class NodeEntry
	{
		public string HostName { get; set; }

		private InstanceEntryList _instances;
		public InstanceEntryList Instances { get { return _instances; } }
	}


	public class NodeEntryList : HashtableList<string, NodeEntry>
	{
		public NodeEntryList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object tempValue)
		{
			NodeEntry entry = (NodeEntry)tempValue;
			Add(entry.HostName, entry);
			return IndexOf(entry.HostName);
		}
		
		public new NodeEntry this[string hostName]
		{
			get
			{
				NodeEntry entry = null;
				TryGetValue(hostName, out entry);
				return entry;
			}
			set
			{
				base[hostName] = value;
			}
		}

		public NodeEntry GetEntry(string hostName)
		{
			NodeEntry entry;
			if (!TryGetValue(hostName, out entry))
				throw new ArgumentException(String.Format("An entry for node {0} was not found.", hostName));
			else
				return entry;
		}
		
		public bool HasEntry(string hostName)
		{
			return ContainsKey(hostName);
		}
	}
}
