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
	public class IntegerInterval
	{
		[DataMember]
		public int Begin { get; set; }

		[DataMember]
		public int End { get; set; }

		public IntegerInterval Copy()
		{
			return new IntegerInterval { Begin = this.Begin, End = this.End };
		}
	}
}
