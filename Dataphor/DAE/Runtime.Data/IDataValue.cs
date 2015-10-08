/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USEDATATYPESINNATIVEROW

using System;
using System.IO;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public interface IDataValue	: IDisposable // TODO: Not all value types need to be disposable, need to correct this
	{
		IValueManager Manager { get; }
		Schema.IDataType DataType { get; }
		bool ValuesOwned { get; set; }
		bool IsNil { get; }
		bool IsNative { get; }
		object AsNative { get; set; }
		byte[] AsPhysical { get; set; }
	}
}
