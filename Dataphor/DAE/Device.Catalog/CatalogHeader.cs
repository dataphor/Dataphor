/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Data;
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
using Alphora.Dataphor.DAE.Connection;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Catalog
{
	public enum CatalogCacheLevel 
	{ 
		/// <summary> Indicates that the table variable will be completely re-populated on each request. </summary>
		None, 
		
		/// <summary> Indicates that the table variable is populated when necessary by comparing the timestamp stored with the last populate against the catalog timestamp. This is the default cache level. </summary>
		Normal, 
		
		/// <summary> Indicates that the table variable is a normal table buffer, maintained by standard insert, update, and delete statements through D4. </summary>
		Cached, 
		
		/// <summary> Indicates that the table variable is populated when initially requested, and maintained thereafter. </summary>
		Maintained,
		
		/// <summary> Indicates that the table variable is a base table variable in the catalog store. </summary>
		StoreTable,
		
		/// <summary> Indicates that the table variable is a derived table variable in the catalog store. </summary>
		StoreView
	}	

	public class CatalogHeader : System.Object
	{
		public CatalogHeader(Schema.TableVar ATableVar, NativeTable ANativeTable, long ATimeStamp, CatalogCacheLevel ACacheLevel) : base()
		{
			FTableVar = ATableVar;
			FNativeTable = ANativeTable;
			TimeStamp = ATimeStamp;
			FCacheLevel = ACacheLevel;
		}
		
		private Schema.TableVar FTableVar;
		public Schema.TableVar TableVar { get { return FTableVar; } }
		
		private NativeTable FNativeTable;
		public NativeTable NativeTable { get { return FNativeTable; } }
		
		public long TimeStamp;
		
		private CatalogCacheLevel FCacheLevel;
		public CatalogCacheLevel CacheLevel { get { return FCacheLevel; } }
		
		/// <summary>Indicates that the table buffer for this header has been populated and should be maintained. Only used for the Maintained catalog cache level. </summary>
		public bool Cached;
	}
	
	public class CatalogHeaders : Hashtable
	{		
		public CatalogHeaders() : base(){}
		
		public CatalogHeader this[Schema.TableVar ATableVar]
		{
			get
			{
				CatalogHeader LResult = (CatalogHeader)base[ATableVar];
				if (LResult == null)
					throw new CatalogException(CatalogException.Codes.CatalogHeaderNotFound, ATableVar.Name);
				return LResult;
			}
		}
		
		public void Add(CatalogHeader AHeader)
		{
			Add(AHeader.TableVar, AHeader);
		}
	}
}
