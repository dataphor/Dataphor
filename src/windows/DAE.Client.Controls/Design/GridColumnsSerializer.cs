/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using Alphora.Dataphor.DAE.Client.Controls;
using System.ComponentModel.Design.Serialization;

namespace Alphora.Dataphor.DAE.Client.Design
{
	/// <summary> Excludes default columns from serialization. </summary>
	public class GridColumnsSerializer : CodeDomSerializer
	{
		public GridColumnsSerializer() {}

		public override object Serialize(IDesignerSerializationManager AManager, object AValue)
		{
			CodeDomSerializer LBaseSerializer = (CodeDomSerializer)AManager.GetSerializer
				(
				typeof(System.Collections.ArrayList),
				typeof(CodeDomSerializer)
				);
			GridColumns LColumns = (GridColumns)AValue;
			if ((LColumns.Count > 0) && (LColumns[0].IsDefaultGridColumn))
			{
				ArrayList LSerializeColumns = new ArrayList();
				return LBaseSerializer.Serialize(AManager, LSerializeColumns);
			}
			else
				return LBaseSerializer.Serialize(AManager, AValue);
		}

		public override object Deserialize(IDesignerSerializationManager AManager, object AValue) 
		{
			CodeDomSerializer LBaseSerializer = (CodeDomSerializer)AManager.GetSerializer
				(
				typeof(Alphora.Dataphor.DAE.Client.Controls.GridColumns).BaseType,
				typeof(CodeDomSerializer)
				);
			return LBaseSerializer.Deserialize(AManager, AValue);
		}
	}
}
