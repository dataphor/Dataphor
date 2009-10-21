/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	public sealed class IndexedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public new TValue this [TKey AIndex]
		{
			get
			{
				TValue LReturn = default(TValue);
				TryGetValue(AIndex, out LReturn);
				return LReturn;
			}
			set { base[AIndex] = value; }			
		}
	}
}
