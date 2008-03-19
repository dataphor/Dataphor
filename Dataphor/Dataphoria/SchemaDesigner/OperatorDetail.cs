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
	public class OperatorDetailSurface : ObjectDetailSurface
	{
		public OperatorDetailSurface(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			FDetail = new OperatorDetail(AObject, ADesigner);
			Controls.Add(FDetail);
		}

		private OperatorDetail FDetail;

		public OperatorSchema Operator
		{
			get { return (OperatorSchema)Object; }
		}

		public override void Details(Control AControl)
		{
		}
			
		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			Size LClientSize = ClientSize;
			if (FDetail != null)
			{
				FDetail.Left = (LClientSize.Width / 2) - (FDetail.Width / 2);
				FDetail.Top = (LClientSize.Height / 2) - (FDetail.Height / 2);
			}
		}
	}

	public class OperatorDetail : OperatorDesigner
	{
		public OperatorDetail(ObjectSchema AObject, DesignerControl ADesigner) : base(AObject, ADesigner)
		{
			Size = new Size(300, 350);
			TextVAlign = VerticalAlignment.Top;
		}
	}
}
