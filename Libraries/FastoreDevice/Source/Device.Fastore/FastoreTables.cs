/*
	Dataphor
	© Copyright 2000-2012 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;
using System.Collections.Generic;
using Alphora.Fastore.Client;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
	public class FastoreTables : List
	{
		public FastoreTables() : base() { }

		public new FastoreTable this[int index]
		{
			get { lock (this) { return (FastoreTable)base[index]; } }
			set { lock (this) { base[index] = value; } }
		}

		public int IndexOf(Schema.TableVar tableVar)
		{
			lock (this)
			{
				for (int index = 0; index < Count; index++)
					if (this[index].TableVar == tableVar)
						return index;
				return -1;
			}
		}

		public bool Contains(Schema.TableVar tableVar)
		{
			return IndexOf(tableVar) >= 0;
		}

		public FastoreTable this[Schema.TableVar tableVar]
		{
			get
			{
				lock (this)
				{
					int index = IndexOf(tableVar);
					if (index < 0)
						throw new RuntimeException(RuntimeException.Codes.NativeTableNotFound, tableVar.DisplayName);
					return this[index];
				}
			}
		}
	}
}
