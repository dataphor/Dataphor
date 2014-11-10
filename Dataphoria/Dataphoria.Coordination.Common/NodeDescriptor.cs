/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Coordination.Common
{
	[DataContract]
	public class NodeDescriptor
	{
		[DataMember]
		public string HostName { get; set; }

		[DataMember]
		public bool IsEnabled { get; set; }

		[DataMember]
		public IntegerIntervalList PortPool { get; set; }

		[DataMember]
		public int MaxInstances { get; set; }

		[DataMember]
		public TimeSpan KeepAliveTime { get; set; }

		public NodeDescriptor Copy()
		{
			return 
				new NodeDescriptor 
				{ 
					HostName = this.HostName, 
					IsEnabled = this.IsEnabled, 
					PortPool = this.PortPool.Copy(), 
					MaxInstances = this.MaxInstances,
					KeepAliveTime = this.KeepAliveTime
				};
		}

		public void CopyFrom(NodeDescriptor other)
		{
			this.HostName = other.HostName;
			this.IsEnabled = other.IsEnabled;
			this.PortPool = other.PortPool.Copy();
			this.MaxInstances = other.MaxInstances;
			this.KeepAliveTime = other.KeepAliveTime;
		}
	}
}
