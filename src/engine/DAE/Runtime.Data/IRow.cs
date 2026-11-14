/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE.Streams;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public interface IRow : IDataValue
	{
		new Schema.IRowType DataType { get; }
		bool HasNonNativeValue(int index);
		StreamID GetNonNativeStreamID(int index);
		IDataValue GetValue(int index);
		void SetValue(int index, IDataValue value);
		object GetNativeValue(int index);
		object this[int index] { get; set; }
		void BeginModifiedContext();
		BitArray EndModifiedContext();
		int IndexOfColumn(string columnName);
		object this[string columnName] { get; set; }
		IDataValue GetValue(string columnName);
		void SetValue(string columnName, IDataValue value);
		bool HasValue(int index);
		bool HasValue(string columnName);
		bool HasNils();
		bool HasAnyNoValues();
		bool HasNonNativeValues();
		void ClearValue(int index);
		void ClearValue(string columnName);
		void ClearValues();
		BitArray GetValueFlags();
		void CopyTo(IRow row);
	}
}
