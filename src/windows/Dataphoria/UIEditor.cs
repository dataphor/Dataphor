/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;

using Alphora.Dataphor.Frontend;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary>Enables the creation and editing of a Document Expression</summary>
	public class DocumentExpressionUIEditor : UITypeEditor
	{
		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) 
		{	 
			try
			{
				if (((string)value) == String.Empty)
				{
					if (context != null) 
					{
						DocumentExpression documentExpression = new DocumentExpression();
						documentExpression.Type = DocumentType.Document;
						
						DocumentExpressionOperatorAttribute attribute = (DocumentExpressionOperatorAttribute)context.PropertyDescriptor.Attributes[typeof(DocumentExpressionOperatorAttribute)];
						if (attribute != null)
							documentExpression.DocumentArgs.OperatorName = attribute.OperatorName;
						value = DocumentExpressionForm.Execute(documentExpression);
					}
				}
				else
					value = DocumentExpressionForm.Execute((string)value);
			}
			catch (Exception exception)
			{
				Frontend.Client.Windows.Session.HandleException(exception);
			}
			return value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(context);
		}
	}
}
