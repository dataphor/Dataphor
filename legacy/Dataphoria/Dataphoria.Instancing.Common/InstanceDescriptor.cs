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

namespace Alphora.Dataphor.Dataphoria.Instancing.Common
{
    [DataContract]
	public class InstanceDescriptor
	{
		private string _name;
        [DataMember]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		
		private int _portNumber;
        [DataMember]
		public int PortNumber
		{
			get { return _portNumber; }
			set { _portNumber = value; }
		}
	}
}
