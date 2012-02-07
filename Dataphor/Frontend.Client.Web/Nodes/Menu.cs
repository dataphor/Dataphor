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
			_menuItem = new MenuItem();
			_menuItem.OnClick += new EventHandler(MenuItemClicked);
		}

		// MenuItem

		private MenuItem _menuItem;
		public MenuItem MenuItem { get { return _menuItem; } }

		private void MenuItemClicked(object sender, EventArgs args)
		{
			if (GetEnabled())
				Action.Execute();
		}

		// IWebMenu

		public MenuItemList Items
		{
			get { return _menuItem.Items; }
		}

		// Node

		public override bool IsValidChild(Type childType)
		{
			if (typeof(IMenu).IsAssignableFrom(childType))
				return true;
			return base.IsValidChild(childType);
		}

		protected override void ChildrenChanged()
		{
			if (Active)
				InternalUpdateEnabled();
			base.ChildrenChanged();
		}

		protected override void Activate()
		{
			int position;
			// calculate the position to insert the menu at
			if (Parent is IMenu)
				position = Parent.Children.IndexOf(this);
			else
			{
				position = 1;  // start at one since there will always be a file menu at position 0
				foreach (INode node in Parent.Children)
				{
					if (node == this)
						break;
					if (node is IMenu)
						position++;
				}
			}
			IWebMenu webMenu = (IWebMenu)FindParent(typeof(IWebMenu));
			position = Math.Min(webMenu.Items.Count, position);
			webMenu.Items.Insert(position, _menuItem);
			base.Activate();
			InternalSetImage(null);
		}

		protected override void Deactivate()
		{
			((IWebMenu)FindParent(typeof(IWebMenu))).Items.Remove(_menuItem);
			base.Deactivate();
			Session.Get(this).ImageCache.Deallocate(_menuItem.ImageID);
		}

		// Client.ActionNode

		protected override void InternalUpdateEnabled()
		{
			_menuItem.Enabled = GetEnabled();
		}

		public override bool GetEnabled()
		{
			return (Children.Count > 0) || base.GetEnabled();
		}

		protected override void InternalUpdateImage()
		{
			ImageCache cache = Session.Get(this).ImageCache;
			cache.Deallocate(_menuItem.ImageID);
			if (Action != null)
				_menuItem.ImageID = cache.Allocate(Action.Image);
			else
				_menuItem.ImageID = String.Empty;
		}

		protected override void InternalUpdateVisible()
		{
			_menuItem.Visible = GetVisible();
		}

		protected override void InternalUpdateText()
		{
			_menuItem.Text = GetText();
		}

		protected override void InternalUpdateHint()
		{
			_menuItem.Hint = GetHint();
		}
	}      

	public class MenuItemList : List
	{
		protected override void Validate(object tempValue)
		{
			base.Validate(tempValue);
			if (!(tempValue is MenuItem))
				throw new WebClientException(WebClientException.Codes.InvalidMenuChild);
		}

		public new MenuItem this[int index]
		{
			get { return (MenuItem)base[index]; }
		}
	}

	public class MainMenu
	{
		// Items

		private MenuItemList _items = new MenuItemList();
		public MenuItemList Items { get { return _items; } }

		// Render

		public void Render(HttpContext context, HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Class, "mainmenu");
			writer.AddAttribute(HtmlTextWriterAttribute.Id, "MainMenu");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
			writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
			writer.RenderBeginTag(HtmlTextWriterTag.Table);

			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			foreach (MenuItem item in Items)
				item.RenderMainItem(context, writer);

			writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
			writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			writer.RenderEndTag();

			writer.RenderEndTag();
			writer.RenderEndTag();

			// Render menus (invisible)
			foreach (MenuItem item in Items)
				item.RenderSubMenu(context, writer);
		}

		// ProcessRequest

		public bool ProcessRequest(HttpContext context)
		{
			foreach (MenuItem item in Items)
				if (item.ProcessRequest(context))
					return true;
			return false;
		}
	}

	public class MenuItem
	{
		public MenuItem()
		{
			_iD = Session.GenerateID();
		}

		// Items

		private MenuItemList _items = new MenuItemList();
		public MenuItemList Items { get { return _items; } }

		// ID

		private string _iD;
		public string ID { get { return _iD; } }

		// MenuID - ID for submenu

		private string _menuID;	// null default is okay here
		public string MenuID
		{
			get
			{
				if ((_menuID == null) || (_menuID == String.Empty))
					_menuID = Session.GenerateID();
				return _menuID;
			}
		}

		// Hint

		private string _hint = String.Empty;
		public string Hint
		{
			get { return _hint; }
			set { _hint = (value == null ? String.Empty : value); }
		}

		// Text

		private string _text = String.Empty;
		public string Text
		{
			get { return _text; }
			set { _text = (value == null ? String.Empty : value); }
		}

		// ImageID

		private string _imageID = String.Empty;
		public string ImageID
		{
			get { return _imageID; }
			set { _imageID = value; }
		}

		// Enabled

		private bool _enabled = true;
		public bool Enabled
		{
			get { return _enabled; }
			set { _enabled = value; }
		}

		// Visible

		private bool _visible = true;
		public bool Visible
		{
			get { return _visible; }
			set { _visible = value; }
		}

		// OnClick

		public event EventHandler OnClick;

		// ProcessRequest

		public bool ProcessRequest(HttpContext context)
		{
			if (Session.IsActionLink(context, _iD))
			{
				if (OnClick != null)
					OnClick(this, EventArgs.Empty);
				return true;
			}
			else
			{
				foreach (MenuItem child in Items)
					if (child.ProcessRequest(context))
						return true;
				return false;
			}
		}

		// Rendering

		protected virtual void RenderMenuSpacer(HtmlTextWriter writer, string height, string classValue)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Height, height);
			writer.RenderBeginTag(HtmlTextWriterTag.Tr);

			writer.AddAttribute(HtmlTextWriterAttribute.Class, "menuimagepane");
			writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(writer, "24", height);
			writer.RenderEndTag();

			writer.AddAttribute(HtmlTextWriterAttribute.Colspan, "2");
			writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
			if (classValue != String.Empty)
				writer.AddAttribute(HtmlTextWriterAttribute.Class, classValue);
			writer.RenderBeginTag(HtmlTextWriterTag.Td);
			Session.RenderDummyImage(writer, "8", height);
			writer.RenderEndTag();

			writer.RenderEndTag();
		}

		public virtual void RenderItem(HttpContext context, HtmlTextWriter writer) 
		{
			if (_visible)
			{
				if (_text == "-")
				{
					RenderMenuSpacer(writer, "3", String.Empty);
					RenderMenuSpacer(writer, "1", "separator");
					RenderMenuSpacer(writer, "3", String.Empty);
				}
				else
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Id, ID);
					if (_enabled)
					{
						if (Items.Count > 0)
						{
							writer.AddAttribute("submenu", MenuID);
							writer.AddAttribute(HtmlTextWriterAttribute.Onclick, @"MenuItemClick(this, event)");
						}
						else
							writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(context, ID));
						writer.AddAttribute("onmouseover", @"MenuItemOver(this)");
						writer.AddAttribute("onmouseout", @"MenuItemOut(this)");
					}
					writer.RenderBeginTag(HtmlTextWriterTag.Tr);

					writer.AddAttribute(HtmlTextWriterAttribute.Class, "menuimagepane");
					writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					writer.RenderBeginTag(HtmlTextWriterTag.Td);
					Session.RenderDummyImage(writer, "4", "1");
					if (_imageID != String.Empty)
						writer.AddAttribute(HtmlTextWriterAttribute.Src, "ViewImage.aspx?ImageID=" + _imageID);
					else
						writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/pixel.gif");
					writer.AddAttribute(HtmlTextWriterAttribute.Width, "16");
					writer.AddAttribute(HtmlTextWriterAttribute.Height, "16");
					writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
					writer.RenderBeginTag(HtmlTextWriterTag.Img);
					writer.RenderEndTag();
					Session.RenderDummyImage(writer, "4", "1");
					writer.RenderEndTag();

					if (!_enabled)
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "disabledmenuitem");
					writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
					writer.RenderBeginTag(HtmlTextWriterTag.Td);
					Session.RenderDummyImage(writer, "4", "1");
					writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(_text)));
					Session.RenderDummyImage(writer, "4", "1");
					writer.RenderEndTag();

					if (Items.Count > 0)
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
						writer.RenderBeginTag(HtmlTextWriterTag.Td);
						Session.RenderDummyImage(writer, "4", "1");
						writer.AddAttribute(HtmlTextWriterAttribute.Src, "images/submenu.png");
						writer.AddAttribute(HtmlTextWriterAttribute.Width, "5");
						writer.AddAttribute(HtmlTextWriterAttribute.Height, "9");
						writer.RenderBeginTag(HtmlTextWriterTag.Img);
						writer.RenderEndTag();
						Session.RenderDummyImage(writer, "4", "1");
						writer.RenderEndTag();
					}

					writer.RenderEndTag();	// tr
				}
			}	// visible
		}

		public virtual void RenderMainItem(HttpContext context, HtmlTextWriter writer)
		{
			if (_visible)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Id, ID);
				writer.AddAttribute("menu", "MainMenu");
				if (_enabled)
				{
					writer.AddAttribute("onmouseover", "MenuItemOver(this)");
					writer.AddAttribute("onmouseout", "MenuItemOut(this)");
					if (Items.Count > 0)
					{
						writer.AddAttribute("submenu", MenuID);
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, @"MenuItemClick(this, event)");
					}
					else
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, Session.GetActionLink(context, _iD));
				}
				else
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "disabledmenuitem");
				writer.AddAttribute(HtmlTextWriterAttribute.Align, "left");
				writer.AddAttribute(HtmlTextWriterAttribute.Nowrap, null);
				writer.RenderBeginTag(HtmlTextWriterTag.Td);

				Session.RenderDummyImage(writer, "3", "1");

				writer.Write(HttpUtility.HtmlEncode(Session.RemoveAccellerator(_text)));

				Session.RenderDummyImage(writer, "3", "1");

				writer.RenderEndTag();
			}
		}

		public virtual void RenderSubMenu(HttpContext context, HtmlTextWriter writer)
		{
			if (_visible && (Items.Count > 0))
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Class, "menu");
				writer.AddAttribute(HtmlTextWriterAttribute.Style, "display: none");
				writer.AddAttribute(HtmlTextWriterAttribute.Id, _menuID);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
				writer.AddAttribute(HtmlTextWriterAttribute.Border, "0");
				writer.RenderBeginTag(HtmlTextWriterTag.Table);

				RenderMenuSpacer(writer, "3", String.Empty);
	
				foreach (MenuItem item in Items)
					item.RenderItem(context, writer);

				RenderMenuSpacer(writer, "3", String.Empty);

				writer.RenderEndTag();	// table

				writer.RenderEndTag();	// div

				foreach (MenuItem item in Items)
					item.RenderSubMenu(context, writer);
			}
		}

	}      
}
