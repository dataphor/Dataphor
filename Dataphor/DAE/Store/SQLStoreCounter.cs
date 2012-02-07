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

using Alphora.Dataphor.DAE.Connection;

namespace Alphora.Dataphor.DAE.Store
{
	public class SQLStoreCounter
	{
		public SQLStoreCounter(string operation, string tableName, string indexName, bool isMatched, bool isRanged, bool isUpdatable, TimeSpan duration)
		{
			_operation = operation;
			_tableName = tableName;
			_indexName = indexName;
			_isMatched = isMatched;
			_isRanged = isRanged;
			_isUpdatable = isUpdatable;
			_duration = duration;
		}
		
		private string _operation;
		public string Operation { get { return _operation; } }
		
		private string _tableName;
		public string TableName { get { return _tableName; } }
		
		private string _indexName;
		public string IndexName { get { return _indexName; } }
		
		private bool _isMatched;
		public bool IsMatched { get { return _isMatched; } }
		
		private bool _isRanged;
		public bool IsRanged { get { return _isRanged; } }
		
		private bool _isUpdatable;
		public bool IsUpdatable { get { return _isUpdatable; } }
		
		private TimeSpan _duration;
		public TimeSpan Duration { get { return _duration; } }
	}
	
	public class SQLStoreCounters : List<SQLStoreCounter> {}
}
