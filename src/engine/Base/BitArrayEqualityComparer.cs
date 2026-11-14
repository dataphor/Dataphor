using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	public class BitArrayEqualityComparer : IEqualityComparer<BitArray>
	{
		private static BitArrayEqualityComparer _default;
		public static BitArrayEqualityComparer Default
		{
			get
			{
				if (_default == null)
					_default = new BitArrayEqualityComparer();
				return _default;
			}	
		}

		#region IEqualityComparer<BitArray> Members

		public bool Equals(BitArray x, BitArray y)
		{
			if (x == y)
				return true;

			if ((x == null) || (y == null))
				return false;

			if (x.Length != y.Length)
				return false;

			for (int i = 0; i < x.Length; i++)
				if (x[i] != y[i])
					return false;

			return true;
		}

		public int GetHashCode(BitArray obj)
		{
			var hc = 0;
			for (int i = 0; i < obj.Length; i++)
				hc = (hc * 83) + (obj[i] ? 2 : 1);

			return hc;
		}

		#endregion
	}
}
