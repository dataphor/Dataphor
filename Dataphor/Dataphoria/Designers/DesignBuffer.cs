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
		public DesignBuffer(IDataphoria dataphoria, DebugLocator locator)
		{
			_dataphoria = dataphoria;
			_locator = locator;
		}

		// Dataphoria

		private IDataphoria _dataphoria;
		public IDataphoria Dataphoria
		{
			get { return _dataphoria; }
		}

		// Locator
		
		private DebugLocator _locator;
		public DebugLocator Locator
		{
			get { return _locator; }
		}

		public abstract string GetDescription();

		public abstract void SaveData(string data);

		public abstract void SaveBinaryData(Stream data);

		public abstract string LoadData();

		public abstract void LoadData(Stream data);

		public abstract bool LocatorNameMatches(string name);
	}
}
