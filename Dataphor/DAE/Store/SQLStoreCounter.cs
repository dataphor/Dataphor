/*
	Alphora Dataphor
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
using System.Data;
using System.Data.Common;

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public class SQLStoreCounter
	{
		public SQLStoreCounter(string AOperation, string ATableName, string AIndexName, bool AIsMatched, bool AIsRanged, bool AIsUpdatable, TimeSpan ADuration)
		{
			FOperation = AOperation;
			FTableName = ATableName;
			FIndexName = AIndexName;
			FIsMatched = AIsMatched;
			FIsRanged = AIsRanged;
			FIsUpdatable = AIsUpdatable;
			FDuration = ADuration;
		}
		
		private string FOperation;
		public string Operation { get { return FOperation; } }
		
		private string FTableName;
		public string TableName { get { return FTableName; } }
		
		private string FIndexName;
		public string IndexName { get { return FIndexName; } }
		
		private bool FIsMatched;
		public bool IsMatched { get { return FIsMatched; } }
		
		private bool FIsRanged;
		public bool IsRanged { get { return FIsRanged; } }
		
		private bool FIsUpdatable;
		public bool IsUpdatable { get { return FIsUpdatable; } }
		
		private TimeSpan FDuration;
		public TimeSpan Duration { get { return FDuration; } }
	}
	
	public class SQLStoreCounters : List<SQLStoreCounter> {}
}
