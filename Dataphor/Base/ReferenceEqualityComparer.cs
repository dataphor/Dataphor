/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	public class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		#region IEqualityComparer<object> Members
		
		public new bool Equals(object x, object y)
		{
			return Object.ReferenceEquals(x, y);
		}

		public int GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}

		#endregion
	}
}
