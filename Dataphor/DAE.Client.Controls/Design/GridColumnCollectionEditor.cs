/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel.Design;

namespace Alphora.Dataphor.DAE.Client.Design
{
	/// <summary> Provides an interface to edit GridColumns </summary>
	public class GridColumnCollectionEditor : CollectionEditor
	{
		public GridColumnCollectionEditor(Type type) : base(type) {}

		protected override Type[] CreateNewItemTypes()
		{
			Type[] LItemTypes = {CollectionItemType, typeof(Alphora.Dataphor.DAE.Client.Controls.CheckBoxGridColumn)};
			return LItemTypes;
		}

		private int NonDefaultGridColumnCount(Alphora.Dataphor.DisposableList AColumnsArray)
		{
			int LObjectCount = 0;
			for (int i = 0; i < AColumnsArray.Count; i++)
				if (!((Alphora.Dataphor.DAE.Client.Controls.GridColumn)AColumnsArray[i]).IsDefaultGridColumn)
					LObjectCount++;
			return LObjectCount;
		}

		protected override object[] GetItems(object AEditValue)
		{
			Alphora.Dataphor.DisposableList LArray = (Alphora.Dataphor.DisposableList)AEditValue;
			int LObjectCount = NonDefaultGridColumnCount(LArray);
			if (LObjectCount > 0)
			{
				object[] LNonDefaultColumns = new object[LObjectCount];
				int LInsertIndex = 0;
				for (int i = 0; i < LArray.Count; i++)
					if (!((Alphora.Dataphor.DAE.Client.Controls.GridColumn)LArray[i]).IsDefaultGridColumn)
						LNonDefaultColumns[LInsertIndex++] = LArray[i];
				return LNonDefaultColumns;
			}
			else
				return new object[] {};
			
		}

	}

	public class StringCollectionEditor : CollectionEditor
	{
		public StringCollectionEditor(Type type) : base(type) {}
		protected override object CreateInstance(Type AType)
		{
			if (AType == typeof(string))
				return String.Empty;
			else
				return base.CreateInstance(AType);
		}
	}
}
