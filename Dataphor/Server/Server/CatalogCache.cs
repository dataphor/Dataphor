/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Server
{
	// Catalog Caches
	public class CatalogCache : System.Object
	{
		public CatalogCache(string cacheName, long cacheTimeStamp, Schema.Catalog defaultCachedObjects) : base()
		{
			_cacheName = cacheName;
			_cacheTimeStamp = cacheTimeStamp;
			PopulateDefaultCachedObjects(defaultCachedObjects);
		}
		
		private string _cacheName;
		public string CacheName { get { return _cacheName; } }
		
		// CacheTimeStamp
		// The time stamp of the server-side catalog when this cache was built
		private long _cacheTimeStamp;
		public long CacheTimeStamp { get { return _cacheTimeStamp; } }
		
		// TimeStamp
		// A logical clock used to coordinate changes to the cache with the client
		// When the client receives the results of a prepare, the cache time stamp and the time stamp
		// are used to decide whether the client-side cache should be recreated, or just modified. If it is to be modified,
		// the client-side must wait until the client-side cache timestamp is timestamp - 1 in order for the cache to be ready
		// to apply the changes in the deserialization script. A timeout waiting for the timestamp to update will result in an
		// UpdateTimeStamps call to the server to reset the caching system.
		private long _timeStamp = 1;
		public long TimeStamp { get { return _timeStamp; } }
		
		public void UpdateTimeStamp()
		{
			_timeStamp += 1;
		}
		
		public bool EnsureConsistent(long cacheTimeStamp, Schema.Catalog defaultCachedObjects)
		{
			if (cacheTimeStamp > _cacheTimeStamp)
			{
				_cachedObjects.Clear();
				_cacheTimeStamp = cacheTimeStamp;
				PopulateDefaultCachedObjects(defaultCachedObjects);
				UpdateTimeStamp();
				return true;
			}
			return false;
		}
		
		private void PopulateDefaultCachedObjects(Schema.Catalog defaultCachedObjects)
		{
			foreach (Schema.Object objectValue in defaultCachedObjects)
				_cachedObjects.Add(objectValue);
		}
		
		private Schema.Objects _cachedObjects = new Schema.Objects();
		public Schema.Objects CachedObjects { get { return _cachedObjects; } }
		
		private RemoteServerSessions _sessions = new RemoteServerSessions(false);
		public RemoteServerSessions Sessions { get { return _sessions; } }
	}
	
	public class CatalogCaches : System.Object
	{
		private Hashtable _caches = new Hashtable();
		private Schema.Catalog _defaultCachedObjects = new Schema.Catalog();
		
		public void AddSession(RemoteServerSession session)
		{
			lock (this)
			{
				CatalogCache cache = (CatalogCache)_caches[session.CatalogCacheName];
				if (cache == null)
				{
					cache = new CatalogCache(session.CatalogCacheName, session.Server.CacheTimeStamp, _defaultCachedObjects);
					_caches.Add(cache.CacheName, cache);
				}
				
				cache.Sessions.Add(session);
			}
		}
		
		public void RemoveSession(RemoteServerSession session)
		{
			lock (this)
			{
				CatalogCache cache = (CatalogCache)_caches[session.CatalogCacheName];
				if (cache != null)
					cache.Sessions.Remove(session);
			}
		}
		
		public void RemoveCache(string catalogCacheName)
		{
			lock (this)
			{
				CatalogCache cache = (CatalogCache)_caches[catalogCacheName];
				if (cache != null)
					_caches.Remove(catalogCacheName);
			}
		}
		
		public string[] GetRequiredObjects(RemoteServerSession session, Schema.Catalog catalog, long cacheTimeStamp, out long clientCacheTimeStamp)
		{
			List<string> requiredObjects = new List<string>();
			CatalogCache cache = (CatalogCache)_caches[session.CatalogCacheName];

			lock (cache)
			{
				bool cacheChanged = cache.EnsureConsistent(cacheTimeStamp, _defaultCachedObjects);
				foreach (Schema.Object objectValue in catalog)
					if (!cache.CachedObjects.ContainsName(objectValue.Name))
					{
						if (!((objectValue is Schema.DerivedTableVar) && (objectValue.Name == ((Schema.DerivedTableVar)objectValue).SessionObjectName)))
							cache.CachedObjects.Add(objectValue);
						requiredObjects.Add(objectValue.Name);
					}
					
				if (!cacheChanged)
					cache.UpdateTimeStamp();
					
				clientCacheTimeStamp = cache.TimeStamp;
			}

			string[] result = new string[requiredObjects.Count];
			requiredObjects.CopyTo(result, 0);

			#if LOGCACHEEVENTS
			ASession.Server.LogMessage(String.Format("Session {0} cache timestamp updated to {1} with required objects: {2}", ASession.SessionID.ToString(), AClientCacheTimeStamp.ToString(), ExceptionUtility.StringsToCommaList(requiredObjects)));
			#endif

			return result;
		}

		public void RemovePlanDescriptor(RemoteServerSession session, string catalogObjectName)
		{
			CatalogCache cache = (CatalogCache)_caches[session.CatalogCacheName];
			if (cache != null)
			{
				lock (cache)
				{
					int index = cache.CachedObjects.IndexOfName(catalogObjectName);
					if (index >= 0)
						cache.CachedObjects.RemoveAt(index);
				}
			}
		}
		
		public void GatherDefaultCachedObjects(Schema.Objects baseObjects)
		{
			for (int index = 0; index < baseObjects.Count; index++)
				_defaultCachedObjects.Add(baseObjects[index]);
		}
	}
}
