/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> An in-memory, fixed sized cache of images. </summary>
	public class ImageCache
	{
		public ImageCache(int ASize)
		{
			FCache = new FixedSizeCache(ASize);
			FValues = new Dictionary<string, byte[]>(ASize);
		}

		private FixedSizeCache FCache;
		private Dictionary<string, byte[]> FValues;

		public byte[] this[string AKey]
		{
			get 
			{ 
				byte[] LResult;
				if (FValues.TryGetValue(AKey, out LResult))
					Remove((string)FCache.Reference(AKey, AKey));	// Refrence the item and remove the overflow item if applicable
				return LResult;
			}
		}

		public void Add(string AKey, byte[] AValue)
		{
			if (AValue != null)
			{
				FValues[AKey] = AValue;
				Remove((string)FCache.Reference(AKey, AKey));
			}
		}

		private void Remove(string AKey)
		{
			if (AKey != null)
				FValues.Remove(AKey);
		}
	}
}