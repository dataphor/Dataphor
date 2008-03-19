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
		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	 
			try
			{
				if (((string)AValue) == String.Empty)
				{
					if (AContext != null) 
					{
						DocumentExpression LDocumentExpression = new DocumentExpression();
						LDocumentExpression.Type = DocumentType.Document;
						
						DocumentExpressionOperatorAttribute LAttribute = (DocumentExpressionOperatorAttribute)AContext.PropertyDescriptor.Attributes[typeof(DocumentExpressionOperatorAttribute)];
						if (LAttribute != null)
							LDocumentExpression.DocumentArgs.OperatorName = LAttribute.OperatorName;
						AValue = DocumentExpressionForm.Execute(LDocumentExpression);
					}
				}
				else
					AValue = DocumentExpressionForm.Execute((string)AValue);
			}
			catch (Exception LException)
			{
				Frontend.Client.Windows.Session.HandleException(LException);
			}
			return AValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.Modal;
			else
				return base.GetEditStyle(AContext);
		}
	}
}
