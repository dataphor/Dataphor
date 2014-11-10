/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Coordination.Common
{
	public class IntegerIntervalList : BaseList<IntegerInterval>
	{
		public IntegerIntervalList Copy()
		{
			var result = new IntegerIntervalList();
			foreach (var i in this)
			{
				result.Add(i.Copy());
			}

            return result;
		}
	}
}
