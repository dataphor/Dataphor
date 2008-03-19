/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;

namespace Alphora.Dataphor.DAE.Client.Controls.Design
{
	public class PasswordEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;
		
		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					PasswordEdit LEdit = new PasswordEdit();
					try
					{
						LEdit.Password = (string)AValue;
						FEditorService.ShowDialog(LEdit);
						if (LEdit.DialogResult == DialogResult.OK)
							AValue = LEdit.Password;
					}
					finally
					{
						LEdit.Dispose();
					}
				}
			}
			return AValue;
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{ 
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			return base.GetEditStyle(AContext);
		}
	}
}