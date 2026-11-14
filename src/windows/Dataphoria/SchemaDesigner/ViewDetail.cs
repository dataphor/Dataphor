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
	public class ViewDetailSurface : ObjectDetailSurface
	{
		public ViewDetailSurface(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
		}

		public ViewSchema ViewSchema
		{
			get { return (ViewSchema)Object; }
		}


		public override void Details(Control AControl)
		{
		}
	}
}
