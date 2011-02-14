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
		private IWindowsFormsEditorService _editorService = null;
		
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	 
			if (context != null && context.Instance != null && provider != null) 
			{
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					PasswordEdit edit = new PasswordEdit();
					try
					{
						edit.Password = (string)tempValue;
						_editorService.ShowDialog(edit);
						if (edit.DialogResult == DialogResult.OK)
							tempValue = edit.Password;
					}
					finally
					{
						edit.Dispose();
					}
				}
			}
			return tempValue;
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{ 
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			return base.GetEditStyle(context);
		}
	}
}