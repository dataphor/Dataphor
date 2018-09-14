/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public abstract class NativeRowIndex : System.Object
	{
		public NativeRowIndex(Schema.RowType keyRowType, Schema.RowType dataRowType)
		{
			_keyRowType = keyRowType;
			_dataRowType = dataRowType;
		}

		protected Schema.RowType _keyRowType;
		/// <summary>The row type of the key for the index.</summary>
		public Schema.RowType KeyRowType { get { return _keyRowType; } }
		
		protected Schema.RowType _dataRowType;
		/// <summary>The row type for data for the index.</summary>
		public Schema.RowType DataRowType { get { return _dataRowType; } }

		public abstract void Drop(IValueManager manager);

		public abstract void Insert(IValueManager manager, NativeRow key, NativeRow data);

		public abstract void Update(IValueManager manager, NativeRow oldKey, NativeRow newKey, NativeRow newData);

		public abstract void Delete(IValueManager manager, NativeRow key);

		public NativeRow CopyKey(IValueManager manager, NativeRow sourceKey)
		{
			return (NativeRow)DataValue.CopyNative(manager, KeyRowType, sourceKey);
		}
		
		public NativeRow CopyData(IValueManager manager, NativeRow sourceData)
		{
			return (NativeRow)DataValue.CopyNative(manager, DataRowType, sourceData);
		}
		
		public void DisposeKey(IValueManager manager, NativeRow key)
		{
			DataValue.DisposeNative(manager, KeyRowType, key);
		}
		
		public void DisposeData(IValueManager manager, NativeRow data)
		{
			DataValue.DisposeNative(manager, DataRowType, data);
		}
	}
	
/*
	#if USETYPEDLIST
	public class NativeRowIndexList : TypedList
	{
		public NativeRowIndexList() : base(typeof(NativeRowIndex)) {}
		
		public new NativeRowIndex this[int AIndex]
		{
			get { return (NativeRowIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class NativeRowIndexList : BaseList<NativeRowIndex> { }
	#endif
*/
}
