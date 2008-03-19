/*
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Text;

using Alphora.Dataphor;
using Alphora.Dataphor.Dataphoria;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria
{
	/// <summary>Class that calls the DocumentExpressionEditor.dfd for DocumentExpression Editing.</summary>
	public class DocumentExpressionForm
	{
		private static void SetEditOpenState(Frontend.Client.IFormInterface AForm)
		{
			AForm.MainSource.OpenState = DAE.Client.DataSetState.Edit;
		}

		public static string Execute(DocumentExpression ADocumentExpression)
		{
			Dataphoria LDataphoria = Dataphoria.DataphoriaInstance;
			Frontend.Client.Windows.IWindowsFormInterface LForm = LDataphoria.FrontendSession.LoadForm(null, ".Frontend.Form('Frontend', 'DocumentExpressionEditor')", new Frontend.Client.FormInterfaceHandler(SetEditOpenState));
			try
			{
				Frontend.Client.INotebook LNotebook = (Frontend.Client.INotebook)LForm.FindNode("Notebook");
				Frontend.Client.ISource LSource = LForm.MainSource;
				switch (ADocumentExpression.Type)
				{
					case DocumentType.Document : 
						LNotebook.ActivePage = (Frontend.Client.INotebookPage)LForm.FindNode("nbpLoad"); 
						LSource.DataView.Fields["Library_Name"].AsString = ADocumentExpression.DocumentArgs.LibraryName;
						LSource.DataView.Fields["Document_Name"].AsString = ADocumentExpression.DocumentArgs.DocumentName;
						break;
					case DocumentType.Derive : 
						LNotebook.ActivePage = (Frontend.Client.INotebookPage)LForm.FindNode("nbpDerive"); 
						LForm.MainSource.DataView["Query"].AsString = ADocumentExpression.DeriveArgs.Query;
						LForm.MainSource.DataView["PageType"].AsString = ADocumentExpression.DeriveArgs.PageType;
						LForm.MainSource.DataView["MKN"].AsString = ADocumentExpression.DeriveArgs.MasterKeyNames;
						LForm.MainSource.DataView["DKN"].AsString = ADocumentExpression.DeriveArgs.DetailKeyNames;
						LForm.MainSource.DataView["Elaborate"].AsBoolean = ADocumentExpression.DeriveArgs.Elaborate;
						break;
					default : 
						LNotebook.ActivePage = (Frontend.Client.INotebookPage)LForm.FindNode("nbpOther"); 
						LForm.MainSource.DataView["Expression"].AsString = ADocumentExpression.Expression;
						break;
				}
				if (LForm.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				if ((LNotebook.ActivePage.Name) == "nbpLoad")
				{	
					ADocumentExpression.Type = DocumentType.Document;
					ADocumentExpression.DocumentArgs.LibraryName = LForm.MainSource.DataView["Library_Name"].AsString;
					ADocumentExpression.DocumentArgs.DocumentName = LForm.MainSource.DataView["Document_Name"].AsString;
				}
				else if ((LNotebook.ActivePage.Name) == "nbpDerive")
				{	
					ADocumentExpression.Type = DocumentType.Derive;
					ADocumentExpression.DeriveArgs.Query = LForm.MainSource.DataView["Query"].AsString;
					ADocumentExpression.DeriveArgs.PageType = LForm.MainSource.DataView["PageType"].AsString;
					ADocumentExpression.DeriveArgs.MasterKeyNames = LForm.MainSource.DataView["MKN"].AsString;
					ADocumentExpression.DeriveArgs.DetailKeyNames = LForm.MainSource.DataView["DKN"].AsString;
					ADocumentExpression.DeriveArgs.Elaborate = LForm.MainSource.DataView["Elaborate"].AsBoolean;
				}
				else if ((LNotebook.ActivePage.Name) == "nbpOther")
				{	
					ADocumentExpression.Type = DocumentType.Other;
					ADocumentExpression.Expression = LForm.MainSource.DataView["Expression"].AsString;
				}
				return ADocumentExpression.Expression;
			}
			finally
			{
				LForm.Dispose();
			}
		}
		
		public static string Execute()
		{	
			DocumentExpression LDocumentExpression = new DocumentExpression();
			LDocumentExpression.Type = DocumentType.None;
			return Execute(LDocumentExpression);
		}
		
		public static string Execute(string AExpression)
		{
			DocumentExpression LDocumentExpression = new DocumentExpression(AExpression);
			return Execute(LDocumentExpression);
		}
	}
}
