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
	public abstract class ObjectDetailSurface : Surface
	{
		public ObjectDetailSurface(ObjectSchema AObject, DesignerControl ADesigner) : base(ADesigner)
		{
			FObject = AObject;
			FObject.OnModified += new SchemaHandler(SchemaModified);
			FObject.OnDeleted += new SchemaHandler(SchemaDeleted);
		}

		protected override void Dispose(bool ADisposed)
		{
			if (FObject != null)
			{
				FObject.OnModified -= new SchemaHandler(SchemaModified);
				FObject.OnDeleted -= new SchemaHandler(SchemaDeleted);
				FObject = null;
			}
			base.Dispose(ADisposed);
		}

		private ObjectSchema FObject;
		public ObjectSchema Object
		{
			get { return FObject; }
		}

		private void SchemaModified(BaseSchema ASchema)
		{
			DesignerControl.Modified();
			UpdateFromSchema();
		}

		private void SchemaDeleted(BaseSchema ASchema)
		{
			DesignerControl.Modified();
			while (DesignerControl.ActiveSurface != this)	// pop everything deeper than us
				DesignerControl.Pop();
			DesignerControl.Pop();							// pop us
		}

		protected virtual void UpdateFromSchema()
		{
		}
	}
}
