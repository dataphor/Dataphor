/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Alphora.Dataphor.DAE.Server
{
	// Catalog Caches
	public class CatalogCache : System.Object
	{
		public CatalogCache(string ACacheName, long ACacheTimeStamp, Schema.Catalog ADefaultCachedObjects) : base()
		{
			FCacheName = ACacheName;
			FCacheTimeStamp = ACacheTimeStamp;
			PopulateDefaultCachedObjects(ADefaultCachedObjects);
		}
		
		private string FCacheName;
		public string CacheName { get { return FCacheName; } }
		
		// CacheTimeStamp
		// The time stamp of the server-side catalog when this cache was built
		private long FCacheTimeStamp;
		public long CacheTimeStamp { get { return FCacheTimeStamp; } }
		
		// TimeStamp
		// A logical clock used to coordinate changes to the cache with the client
		// When the client receives the results of a prepare, the cache time stamp and the time stamp
		// are used to decide whether the client-side cache should be recreated, or just modified. If it is to be modified,
		// the client-side must wait until the client-side cache timestamp is timestamp - 1 in order for the cache to be ready
		// to apply the changes in the deserialization script. A timeout waiting for the timestamp to update will result in an
		// UpdateTimeStamps call to the server to reset the caching system.
		private long FTimeStamp = 1;
		public long TimeStamp { get { return FTimeStamp; } }
		
		public void UpdateTimeStamp()
		{
			FTimeStamp += 1;
		}
		
		public bool EnsureConsistent(long ACacheTimeStamp, Schema.Catalog ADefaultCachedObjects)
		{
			if (ACacheTimeStamp > FCacheTimeStamp)
			{
				FCachedObjects.Clear();
				FCacheTimeStamp = ACacheTimeStamp;
				PopulateDefaultCachedObjects(ADefaultCachedObjects);
				UpdateTimeStamp();
				return true;
			}
			return false;
		}
		
		private void PopulateDefaultCachedObjects(Schema.Catalog ADefaultCachedObjects)
		{
			foreach (Schema.Object LObject in ADefaultCachedObjects)
				FCachedObjects.Add(LObject);
		}
		
		private Schema.Objects FCachedObjects = new Schema.Objects();
		public Schema.Objects CachedObjects { get { return FCachedObjects; } }
		
		private RemoteServerSessions FSessions = new RemoteServerSessions(false);
		public RemoteServerSessions Sessions { get { return FSessions; } }
	}
	
	public class CatalogCaches : System.Object
	{
		private Hashtable FCaches = new Hashtable();
		private Schema.Catalog FDefaultCachedObjects = new Schema.Catalog();
		
		public void AddSession(RemoteServerSession ASession)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ASession.CatalogCacheName];
				if (LCache == null)
				{
					LCache = new CatalogCache(ASession.CatalogCacheName, ASession.Server.CacheTimeStamp, FDefaultCachedObjects);
					FCaches.Add(LCache.CacheName, LCache);
				}
				
				LCache.Sessions.Add(ASession);
			}
		}
		
		public void RemoveSession(RemoteServerSession ASession)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ASession.CatalogCacheName];
				if (LCache != null)
					LCache.Sessions.Remove(ASession);
			}
		}
		
		public void RemoveCache(string ACatalogCacheName)
		{
			lock (this)
			{
				CatalogCache LCache = (CatalogCache)FCaches[ACatalogCacheName];
				if (LCache != null)
					FCaches.Remove(ACatalogCacheName);
			}
		}
		
		public string[] GetRequiredObjects(RemoteServerSession ASession, Schema.Catalog ACatalog, long ACacheTimeStamp, out long AClientCacheTimeStamp)
		{
			StringCollection LRequiredObjects = new StringCollection();
			CatalogCache LCache = (CatalogCache)FCaches[ASession.CatalogCacheName];

			lock (LCache)
			{
				bool LCacheChanged = LCache.EnsureConsistent(ACacheTimeStamp, FDefaultCachedObjects);
				foreach (Schema.Object LObject in ACatalog)
					if (!LCache.CachedObjects.ContainsName(LObject.Name))
					{
						if (!((LObject is Schema.DerivedTableVar) && (LObject.Name == ((Schema.DerivedTableVar)LObject).SessionObjectName)))
							LCache.CachedObjects.Add(LObject);
						LRequiredObjects.Add(LObject.Name);
					}
					
				if (!LCacheChanged)
					LCache.UpdateTimeStamp();
					
				AClientCacheTimeStamp = LCache.TimeStamp;
			}

			string[] LResult = new string[LRequiredObjects.Count];
			LRequiredObjects.CopyTo(LResult, 0);

			#if LOGCACHEEVENTS
			ASession.Server.LogMessage(String.Format("Session {0} cache timestamp updated to {1} with required objects: {2}", ASession.SessionID.ToString(), AClientCacheTimeStamp.ToString(), ExceptionUtility.StringsToCommaList(LRequiredObjects)));
			#endif

			return LResult;
		}

		public void RemovePlanDescriptor(RemoteServerSession ASession, string ACatalogObjectName)
		{
			CatalogCache LCache = (CatalogCache)FCaches[ASession.CatalogCacheName];
			if (LCache != null)
			{
				lock (LCache)
				{
					int LIndex = LCache.CachedObjects.IndexOfName(ACatalogObjectName);
					if (LIndex >= 0)
						LCache.CachedObjects.RemoveAt(LIndex);
				}
			}
		}
		
		public void GatherDefaultCachedObjects(Schema.Objects ABaseObjects)
		{
			for (int LIndex = 0; LIndex < ABaseObjects.Count; LIndex++)
				FDefaultCachedObjects.Add(ABaseObjects[LIndex]);
		}
	}
}
