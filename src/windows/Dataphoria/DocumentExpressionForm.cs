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
		private static void SetEditOpenState(Frontend.Client.IFormInterface form)
		{
			form.MainSource.OpenState = DAE.Client.DataSetState.Edit;
		}

		public static string Execute(DocumentExpression documentExpression)
		{
            IDataphoria dataphoria = Program.DataphoriaInstance;
			Frontend.Client.Windows.IWindowsFormInterface form = dataphoria.FrontendSession.LoadForm(null, ".Frontend.Form('Frontend', 'DocumentExpressionEditor')", new Frontend.Client.FormInterfaceHandler(SetEditOpenState));
			try
			{
				Frontend.Client.INotebook notebook = (Frontend.Client.INotebook)form.FindNode("Notebook");
				Frontend.Client.ISource source = form.MainSource;
				switch (documentExpression.Type)
				{
					case DocumentType.Document : 
						notebook.ActivePage = (Frontend.Client.INotebookPage)form.FindNode("nbpLoad"); 
						source.DataView.Fields["Library_Name"].AsString = documentExpression.DocumentArgs.LibraryName;
						source.DataView.Fields["Document_Name"].AsString = documentExpression.DocumentArgs.DocumentName;
						break;
					case DocumentType.Derive : 
						notebook.ActivePage = (Frontend.Client.INotebookPage)form.FindNode("nbpDerive"); 
						form.MainSource.DataView["Query"].AsString = documentExpression.DeriveArgs.Query;
						form.MainSource.DataView["PageType"].AsString = documentExpression.DeriveArgs.PageType;
						form.MainSource.DataView["MKN"].AsString = documentExpression.DeriveArgs.MasterKeyNames;
						form.MainSource.DataView["DKN"].AsString = documentExpression.DeriveArgs.DetailKeyNames;
						form.MainSource.DataView["Elaborate"].AsBoolean = documentExpression.DeriveArgs.Elaborate;
						break;
					default : 
						notebook.ActivePage = (Frontend.Client.INotebookPage)form.FindNode("nbpOther"); 
						form.MainSource.DataView["Expression"].AsString = documentExpression.Expression;
						break;
				}
				if (form.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				if ((notebook.ActivePage.Name) == "nbpLoad")
				{	
					documentExpression.Type = DocumentType.Document;
					documentExpression.DocumentArgs.LibraryName = form.MainSource.DataView["Library_Name"].AsString;
					documentExpression.DocumentArgs.DocumentName = form.MainSource.DataView["Document_Name"].AsString;
				}
				else if ((notebook.ActivePage.Name) == "nbpDerive")
				{	
					documentExpression.Type = DocumentType.Derive;
					documentExpression.DeriveArgs.Query = form.MainSource.DataView["Query"].AsString;
					documentExpression.DeriveArgs.PageType = form.MainSource.DataView["PageType"].AsString;
					documentExpression.DeriveArgs.MasterKeyNames = form.MainSource.DataView["MKN"].AsString;
					documentExpression.DeriveArgs.DetailKeyNames = form.MainSource.DataView["DKN"].AsString;
					documentExpression.DeriveArgs.Elaborate = form.MainSource.DataView["Elaborate"].AsBoolean;
				}
				else if ((notebook.ActivePage.Name) == "nbpOther")
				{	
					documentExpression.Type = DocumentType.Other;
					documentExpression.Expression = form.MainSource.DataView["Expression"].AsString;
				}
				return documentExpression.Expression;
			}
			finally
			{
				form.Dispose();
			}
		}
		
		public static string Execute()
		{	
			DocumentExpression documentExpression = new DocumentExpression();
			documentExpression.Type = DocumentType.None;
			return Execute(documentExpression);
		}
		
		public static string Execute(string expression)
		{
			DocumentExpression documentExpression = new DocumentExpression(expression);
			return Execute(documentExpression);
		}
	}
}
