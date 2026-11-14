/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public abstract class TableVarDesigner : ObjectDesigner
	{
		public TableVarDesigner(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
		}
	}

	public class TableDesigner : TableVarDesigner
	{
		public TableDesigner(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			SurfaceColor = Color.FromArgb(254, 233, 192);
		}

		public TableSchema Table
		{
			get { return (TableSchema)Object; }
		}

		public override void Details() 
		{
			DesignerControl.Push(new TableDetailSurface(Object, DesignerControl));
		}
	}

	public class ViewDesigner : TableVarDesigner
	{
		public ViewDesigner(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			SurfaceColor = Color.FromArgb(219, 230, 230);
		}

		public ViewSchema View
		{
			get { return (ViewSchema)Object; }
		}

		protected override string GetText()
		{
			return String.Format("({0})", base.GetText());
		}

		public override void Details() 
		{
			DesignerControl.Push(new ViewDetailSurface(Object, DesignerControl));
		}
	}

}