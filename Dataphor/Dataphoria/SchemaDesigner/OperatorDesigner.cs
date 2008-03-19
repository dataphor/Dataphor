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
	public class OperatorDesigner : ObjectDesigner
	{
		public OperatorDesigner(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			SurfaceColor = Color.FromArgb(246, 237, 237);
		}

		public OperatorSchema Operator
		{
			get { return (OperatorSchema)Object; }
		}

		protected override string GetText()
		{
			return base.GetText() + "(...)";
		}

		public override void Details() 
		{
			DesignerControl.Push(new OperatorDetailSurface(Object, DesignerControl));
		}
	}
}
