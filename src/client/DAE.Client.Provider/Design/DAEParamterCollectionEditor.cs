/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel.Design;

namespace Alphora.Dataphor.DAE.Client.Design
{
	/// <summary> Provides an interface to edit DAEParameters </summary>
	public class DAEParameterCollectionEditor : CollectionEditor
	{
		public DAEParameterCollectionEditor(Type type) : base(type) {}

		protected override Type CreateCollectionItemType()
		{
			return typeof(Alphora.Dataphor.DAE.Client.Provider.DAEParameter);
		}
	}
}
