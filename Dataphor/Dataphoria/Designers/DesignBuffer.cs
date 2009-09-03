/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Text;
using System.Reflection;

using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public abstract class DesignBuffer
	{
		public DesignBuffer(IDataphoria ADataphoria)
		{
			FDataphoria = ADataphoria;
		}

		// Dataphoria

		private IDataphoria FDataphoria;
		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
		}

		public abstract string GetDescription();

		// Data

		public abstract void SaveData(string AData);

		public abstract void SaveBinaryData(Stream AData);

		public abstract string LoadData();

		public abstract void LoadData(Stream AData);

		public abstract string GetLocatorName();

		public abstract bool LocatorNameMatches(string AName);
	}
}
