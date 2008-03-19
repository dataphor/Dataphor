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
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
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
		bool IsGeneric { get; set; }
		bool IsDisposable { get; set; }
		bool Equivalent(IDataType ADataType);
		bool Equals(IDataType ADataType);
		bool Is(IDataType ADataType);
		bool Compatible(IDataType ADataType);
		TypeSpecifier EmitSpecifier(EmitMode AMode);
		void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode);
		#if !NATIVEROW
		int StaticByteSize { get; set; }
		#endif
	}
}