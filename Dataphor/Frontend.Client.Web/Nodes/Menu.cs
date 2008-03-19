/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Web;
using System.Web.UI;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class Menu : ActionNode, IMenu, IWebMenu
	{
		public Menu()
		{
			FMenuItem = new MenuItem();
			FMenuItem.OnClick += new EventHandler(MenuItemClicked);
		}

		// MenuItem

		private MenuItem FMenuItem;
		public MenuItem MenuItem { get { return FMenuItem; } }

		private void MenuItemClicked(object ASender, EventArgs AArgs)
		{
			if (GetEnabled())
				Action.Execute();
		}

		// IWebMenu

		public MenuItemList Items
		{
			get { return FMenuItem.Items; }
		}

		// Node

		public override bool IsValidChild(Type AChildType)
		{
			if (typeof(IMenu).IsAssignableFrom(AChildType))
				return true;
			return base.IsValidChild(AChildType);
		}

		protected override void ChildrenChanged()
		{
			if (Active)
				InternalUpdateEnabled();
			base.ChildrenChanged();
		}

		protected override void Activate()
		{
			int LPosition;
			// calculate the position to insert the menu at
			if (Parent is IMenu)
				LPosition = Parent.Children.IndexOf(this);
			else
			{
				LPosition = 1;  // start at one since there will always be a file menu at position 0
				foreach (INode LNode in Parent.Children)
				{
					if (LNode == this)
						break;
					if (LNode is IMenu)
						LPosition++;
				}
			}
			IWebMenu LWebMenu = (IWebMenu)FindParent(typeof(IWebMenu));
			LPosition = Math.Min(LWebMenu.Items.Count, LPosition);
			LWebMenu.Items.Insert(LPosition, FMenuItem);
			base.Activate();
			InternalSetImage(null);
		}

		protected override void Deactivate()
		{
			((IWebMenu)FindParent(typeof(IWebMenu))).Items.Remove(FMenuItem);
			base.Deactivate();
			Session.Get(this).ImageCache.Deallocate(FMenuItem.ImageID);
		}

		// Client.ActionNode

		protected override void InternalUpdateEnabled()
		{
			FMenuItem.Enabled = GetEnabled();
		}

		public override bool GetEnabled()
		{
			return (Children.Count > 0) || base.GetEnabled();
		}

		protected override void InternalUpdateImage()
		{
			ImageCache LCache = Session.Get(this).ImageCache;
			LCache.Deallocate(FMenuItem.ImageID);
			if (Action != null)
				FMenuItem.ImageID = LCache.Allocate(Action.Image);
			else
				FMenuItem.ImageID = String.Empty;
		}

		protected override void InternalUpdateVisible()
		{
			FMenuItem.Visible = GetVisible();
		}

		protected override void InternalUpdateText()
		{
			FMenuItem.Text = GetText();
		}

		protected override void InternalUpdateHint()
		{
			FMenuItem.Hint = GetHint();
		}
	}      

	public class MenuItemList : List
	{
		protected override void Validate(object AValue)
		{
			base.Validate(AValue);
			if (!(AValue is MenuItem))
				throw new WebClientException(WebClientException.Codes.InvalidMenuChild);
		}

		public new MenuItem this[int AIndex]
		{
			get { return (MenuItem)base[AIndex]; }
		}
	}

	public class MainMenu
	{
		// Items

		private MenuItemList FItems = new MenuItemList();
		public MenuItemList Items { get { return FItems; } }

		// Render

		public void Render(HttpContext AContext, HtmlTextWriter AWriter)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "mainmenu");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Id, "MainMenu");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (MenuItem LItem in Items)
				LItem.RenderMainItem(AContext, AWriter);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
			AWriter.RenderEndTag();

			// Render menus (invisible)
			foreach (MenuItem LItem in Items)
				LItem.RenderSubMenu(AContext, AWriter);
		}

		// ProcessRequest

		public bool ProcessRequest(HttpContext AContext)
		{
			foreach (MenuItem LItem in Items)
				if (LItem.ProcessRequest(AContext))
					return true;
			return false;
		}
	}

	public class MenuItem
	{
		public MenuItem()
		{
			FID = Session.GenerateID();
		}

		// Items

		private MenuItemList FItems = new MenuItemList();
		public MenuItemList Items { get { return FItems; } }

		// ID

		private string FID;
		public string ID { get { return FID; } }

		// MenuID - ID for submenu

		private string FMenuID;	// null default is okay here
		public string MenuID
		{
			get
			{
				if ((FMenuID == null) || (FMenuID == String.Empty))
					FMenuID = Session.GenerateID();
				return FMenuID;
			}
		}

		// Hint

		private string FHint = String.Empty;
		public string Hint
		{
			get { return FHint; }
			set { FHint = (value == null ? String.Empty : value); }
		}

		// Text

		private string FText = String.Empty;
		public string Text
		{
			get { return FText; }
			set { FText = (value == null ? String.Empty : value); }
		}

		// ImageID

		private string FImageID = String.Empty;
		public string ImageID
		{
			get { return FImageID; }
			set { FImageID = value; }
		}

		// Enabled

		private bool FEnabled = true;
		public bool Enabled
		{
			get { return FEnabled; }
			set { FEnabled = value; }
		}

		// Visible

		private bool FVisible = true;
		public bool Visible
		{
			get { return FVisible; }
			set { FVisible = value; }
		}

		// OnClick

		public event EventHandler OnClick;

		// ProcessRequest

		public bool ProcessRequest(HttpContext AContext)
		{
			if (Session.IsActionLink(AContext, FID))
			{
				if (OnClick != null)
					OnClick(this, EventArgs.Empty);
				return true;
			}
			else
			{
				foreach (MenuItem LChild in Items)
					if (LChild.ProcessRequest(AContext))
						return true;
				return false;
			}
		}

		// Rendering

		protected virtual void RenderMenuSpacer(HtmlTextWriter AWriter, string AHeight, string AClass)
		{
			AWriter.AddAttribute(HtmlTextWriterAttribute.Height, AHeight);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

			AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "menuimagepane");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(AWriter, "24", AHeight);
			AWriter.RenderEndTag();

			AWriter.AddAttribute(HtmlTextWriterAttribute.Colspan, "2");
			AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			if (AClass != String.Empty)
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, AClass);
			AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(AWriter, "8", AHeight);
			AWriter.RenderEndTag();

			AWriter.RenderEndTag();
		}

		public virtual void RenderItem(HttpContext AContext, HtmlTextWriter AWriter) 
		{
			if (FVisible)
			{
				if (FText == "-")
				{
					RenderMenuSpacer(AWriter, "3", String.Empty);
					RenderMenuSpacer(AWriter, "1", "separator");
					RenderMenuSpacer(AWriter, "3", String.Empty);
				}
				else
				{
					AWriter.AddAttribute(HtmlTextWriterAttribute.Id, ID);
					if (FEnabled)
					{
						if (Items.Count > 0)
						{
							AWriter.AddAttribute("submenu", MenuID);
							AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, @"MenuItemClick(this, event)");
						}
						else
							AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(AContext, ID));
						AWriter.AddAttribute("onmouseover", @"MenuItemOver(this)");
						AWriter.AddAttribute("onmouseout", @"MenuItemOut(this)");
					}
					AWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

					AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "menuimagepane");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
					Session.RenderDummyImage(AWriter, "4", "1");
					if (FImageID != String.Empty)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?ImageID=" + FImageID);
					else
						AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "16");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "16");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
					AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
					AWriter.RenderEndTag();
					Session.RenderDummyImage(AWriter, "4", "1");
					AWriter.RenderEndTag();

					if (!FEnabled)
						AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "disabledmenuitem");
					AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
					Session.RenderDummyImage(AWriter, "4", "1");
					AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(FText)));
					Session.RenderDummyImage(AWriter, "4", "1");
					AWriter.RenderEndTag();

					if (Items.Count > 0)
					{
						AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						AWriter.RenderBeginTag(HtmlTextWriterTag.Td);
						Session.RenderDummyImage(AWriter, "4", "1");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Src, "images/submenu.png");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Width, "5");
						AWriter.AddAttribute(HtmlTextWriterAttribute.Height, "9");
						AWriter.RenderBeginTag(HtmlTextWriterTag.Img);
						AWriter.RenderEndTag();
						Session.RenderDummyImage(AWriter, "4", "1");
						AWriter.RenderEndTag();
					}

					AWriter.RenderEndTag();	// tr
				}
			}	// visible
		}

		public virtual void RenderMainItem(HttpContext AContext, HtmlTextWriter AWriter)
		{
			if (FVisible)
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Id, ID);
				AWriter.AddAttribute("menu", "MainMenu");
				if (FEnabled)
				{
					AWriter.AddAttribute("onmouseover", "MenuItemOver(this)");
					AWriter.AddAttribute("onmouseout", "MenuItemOut(this)");
					if (Items.Count > 0)
					{
						AWriter.AddAttribute("submenu", MenuID);
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, @"MenuItemClick(this, event)");
					}
					else
						AWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(AContext, FID));
				}
				else
					AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "disabledmenuitem");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Align, "left");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Td);

				Session.RenderDummyImage(AWriter, "3", "1");

				AWriter.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(FText)));

				Session.RenderDummyImage(AWriter, "3", "1");

				AWriter.RenderEndTag();
			}
		}

		public virtual void RenderSubMenu(HttpContext AContext, HtmlTextWriter AWriter)
		{
			if (FVisible && (Items.Count > 0))
			{
				AWriter.AddAttribute(HtmlTextWriterAttribute.Class, "menu");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Id, FMenuID);
				AWriter.RenderBeginTag(HtmlTextWriterTag.Div);

				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				AWriter.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				AWriter.RenderBeginTag(HtmlTextWriterTag.Table);

				RenderMenuSpacer(AWriter, "3", String.Empty);
	
				foreach (MenuItem LItem in Items)
					LItem.RenderItem(AContext, AWriter);

				RenderMenuSpacer(AWriter, "3", String.Empty);

				AWriter.RenderEndTag();	// table

				AWriter.RenderEndTag();	// div

				foreach (MenuItem LItem in Items)
					LItem.RenderSubMenu(AContext, AWriter);
			}
		}

	}      
}
