/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using Alphora.Dataphor.DAE.Client.Controls;
using System.ComponentModel.Design.Serialization;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	/// <summary> Excludes default columns from serialization. </summary>
	public class GridColumnsSerializer : CodeDomSerializer
	{
		public GridColumnsSerializer() {}

		public override object Serialize(IDesignerSerializationManager manager, object tempValue)
		{
			CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer
				(
				typeof(System.Collections.ArrayList),
				typeof(CodeDomSerializer)
				);
			GridColumns columns = (GridColumns)tempValue;
			if ((columns.Count > 0) && (columns.HasDefaultColumn()))
			{
				ArrayList serializeColumns = new ArrayList();
				foreach (GridColumn column in columns)
					if (!(column is Alphora.Dataphor.DAE.Client.Controls.DataColumn))
						serializeColumns.Add(column);
				return baseSerializer.Serialize(manager, serializeColumns);
			}
			else
				return baseSerializer.Serialize(manager, tempValue);
		}

		public override object Deserialize(IDesignerSerializationManager manager, object tempValue) 
		{
			CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer
				(
				typeof(Alphora.Dataphor.DAE.Client.Controls.GridColumns).BaseType,
				typeof(CodeDomSerializer)
				);
			return baseSerializer.Deserialize(manager, tempValue);
		}
	}
}
