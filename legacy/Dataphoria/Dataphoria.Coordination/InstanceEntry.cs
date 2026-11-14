/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.Dataphoria.Instancing.Common;

namespace Alphora.Dataphor.Dataphoria.Coordination
{
	public class InstanceEntry
	{
		public string InstanceName { get; set; }

		public string HostName { get; set; }

		public int PortNumber { get; set; }

		public string InstanceUri { get; set; }

		public int ProcessId { get; set; }

		public DateTime LastRequested { get; set; }

		public InstanceState Status { get; set; }
	}

	public class InstanceEntryList : HashtableList<string, InstanceEntry>
	{
		public InstanceEntryList() : base(StringComparer.OrdinalIgnoreCase) {}

		public override int Add(object tempValue)
		{
			InstanceEntry entry = (InstanceEntry)tempValue;
			Add(entry.InstanceName, entry);
			return IndexOf(entry.InstanceName);
		}

		public new InstanceEntry this[string instanceName]
		{
			get
			{
				InstanceEntry entry = null;
				TryGetValue(instanceName, out entry);
				return entry;
			}
			set
			{
				base[instanceName] = value;
			}
		}

		public InstanceEntry GetEntry(string instanceName)
		{
			InstanceEntry entry;
			if (!TryGetValue(instanceName, out entry))
				throw new ArgumentException(String.Format("An entry for instance {0} was not found.", instanceName));
			else
				return entry;
		}
		
		public bool HasEntry(string instanceName)
		{
			return ContainsKey(instanceName);
		}
	}
}
