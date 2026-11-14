/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Device.Catalog;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
/*
	IDataType
		|- IScalarType
		|- IRowType
		|- ITableType
		|- IListType
		|- ICursorType
*/

	public interface IDataType
	{
		string Name { get; set; }
		bool IsNil { get; } // True if the type is known to be the constant nil at compile-time
		bool IsGeneric { get; set; }
		bool IsDisposable { get; set; }
		bool Equivalent(IDataType ADataType);
		bool Equals(IDataType ADataType);
		bool Is(IDataType ADataType);
		bool Compatible(IDataType ADataType);
		TypeSpecifier EmitSpecifier(EmitMode AMode);
		void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode);
		#if !NATIVEROW
		int StaticByteSize { get; set; }
		#endif
	}
}