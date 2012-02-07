/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define USESTRICTNAMERESOLUTIONCACHECLEARING

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
		public NameResolutionCache(int cacheSize)
		{
			_size = cacheSize;
			if (_size > 0)
				_cache = new FixedSizeCache<string, Schema.CatalogObjectHeaders>(_size);
		}

		private int _size;
		public int Size { get { return _size; } }
		
		public void Resize(int cacheSize)
		{
			lock (this)
			{
				if (_size != cacheSize)
				{
					_size = cacheSize;
					_cache = null;
					if (_size > 0)
						_cache = new FixedSizeCache<string, Schema.CatalogObjectHeaders>(_size);
				}
			}
		}
		
		private FixedSizeCache<string, Schema.CatalogObjectHeaders> _cache; 
		
		public void Add(string name, Schema.CatalogObjectHeaders headers)
		{
			lock (this)
			{
				if (_cache != null)
					_cache.Reference(name, headers);
			}
		}
		
		public Schema.CatalogObjectHeaders Resolve(string name)
		{
			lock (this)
			{
				if (_cache != null)
				{
					Schema.CatalogObjectHeaders headers;
					if (_cache.TryGetValue(name, out headers))
						_cache.Reference(name, headers);
					return headers;
				}
			}
			
			return null;
		}

		/// <summary> Remove all entries from the cache. </summary>
		public void Clear()
		{
			lock (this)
			{
				if (_cache != null)
					_cache.Clear();
			}
		}

		/// <summary> Removes entries that could potentially be affected by the given object name. </summary>
		public void Clear(string name)
		{
			#if !USESTRICTNAMERESOLUTIONCACHECLEARING 
			Clear();
			#else
			lock (this)
			{
				while (true)
				{
					FCache.Remove(AName);
					if (Schema.Object.IsQualified(AName))
						AName = Schema.Object.Dequalify(AName);
					else
						break;
				}
			}
			#endif
		}
	}
}
