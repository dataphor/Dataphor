/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public class NameResolutionCache : System.Object
	{
		public NameResolutionCache(int ACacheSize)
		{
			FSize = ACacheSize;
			if (FSize > 0)
				FCache = new FixedSizeCache<string, Schema.CatalogObjectHeaders>(FSize);
		}

		private int FSize;
		public int Size { get { return FSize; } }
		
		public void Resize(int ACacheSize)
		{
			lock (this)
			{
				if (FSize != ACacheSize)
				{
					FSize = ACacheSize;
					FCache = null;
					if (FSize > 0)
						FCache = new FixedSizeCache<string, Schema.CatalogObjectHeaders>(FSize);
				}
			}
		}
		
		private FixedSizeCache<string, Schema.CatalogObjectHeaders> FCache; 
		
		public void Add(string AName, Schema.CatalogObjectHeaders AHeaders)
		{
			lock (this)
			{
				if (FCache != null)
					FCache.Reference(AName, AHeaders);
			}
		}
		
		public Schema.CatalogObjectHeaders Resolve(string AName)
		{
			lock (this)
			{
				if (FCache != null)
				{
					Schema.CatalogObjectHeaders LHeaders;
					if (FCache.TryGetValue(AName, out LHeaders))
						FCache.Reference(AName, LHeaders);
					return LHeaders;
				}
			}
			
			return null;
		}
		
		public void Clear()
		{
			lock (this)
			{
				// TODO: Could potentially only remove affected entries?
				if (FCache != null)
					FCache.Clear();
			}
		}
	}
}
