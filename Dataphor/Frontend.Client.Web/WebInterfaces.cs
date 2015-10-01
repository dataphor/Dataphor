/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Collections;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public interface IWebHandler
	{
		bool ProcessRequest(HttpContext AContext);
	}

	public interface IWebPrehandler
	{
		void PreprocessRequest(HttpContext AContext);
	}

	public interface IWebElement : IElement
	{
		void Render(HtmlTextWriter AWriter);
		void HandleElementException(Exception AException);
		string ID { get; }
	}

	public interface IWebMenu
	{
		MenuItemList Items { get; }
	}

	public interface IWebFormInterface : IFormInterface, IWebElement
	{
		string BackgroundImageID { get; }
		string IconImageID { get; }
		event CancelEventHandler OnClosing;
		event EventHandler Accepting; 
		void PreprocessRequest(HttpContext AContext);
		ErrorList ErrorList { get; }
	}

	public interface IWebToolbar
	{
		ToolBar ToolBar { get; }
	}

	public interface IWebGrid : IGrid
	{
		DAE.Client.DataLink DataLink { get; }
		void MoveTo(int AIndex);
	}

	public interface IWebGridColumn : IGridColumn
	{
		void RenderHeader(HtmlTextWriter AWriter);
		void RenderCell(HtmlTextWriter AWriter, DAE.Runtime.Data.IRow ACurrentRow, bool AIsCurrentRow, int ARowIndex);
	}

	public interface IWebNotebookPage : IBaseNotebookPage, IWebElement
	{
		void Selected();
		void Unselected();
	}
}
