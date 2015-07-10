/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Data;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public partial class BaseForm : Form, IBaseForm
	{
		public BaseForm()
		{
			SetStyle(ControlStyles.UserPaint, true);

			InitializeComponent();

			CausesValidation = false;

			FMainMenu.CanOverflow = true;	// Now browsable so must be set programmatically
			_menuContainer = new FormMainMenuContainer(FMainMenu);
			((FormMainMenuContainer)_menuContainer).ReservedItems = 1;
			_exposedContainer = new FormExposedContainer(FToolBar);

			// Prepare Accept/Reject/Close menu items
			InitializeAcceptReject();

			// Default dialog key mappings
			DialogKeys[Keys.Enter] = new DialogKeyHandler(ProcessEnter);

			#if TRACEFOCUS
			FDialogKeys[Keys.Control | Keys.Shift | Keys.Alt | Keys.F] = new DialogKeyHandler(ProcessQueryFocus);
			#endif

			Disposed += new EventHandler(UninitializeAcceptReject);
		}

		// ContentPanel

		public virtual ScrollableControl ContentPanel
		{
			get { return FContentPanel; }
		}

		#region Icon & Background

		public virtual void ResetIcon()
		{
			this.Icon = ((System.Drawing.Icon)(new System.Resources.ResourceManager(GetType()).GetObject("$this.Icon")));
		}

		public event PaintHandledEventHandler PaintBackground;

		protected override void OnPaintBackground(PaintEventArgs args)
		{
			// Don't bother painting the form's background, it will not be seen
		}

		private void ContentPanelPaint(object sender, PaintEventArgs args)
		{
			if (PaintBackground != null)
			{
				bool handled;
				PaintBackground(sender, args, out handled);
			}
		}

		#endregion

		#region Default Action

		public event EventHandler DefaultAction;

		protected virtual void OnDefaultAction()
		{
			if (DefaultAction != null)
				DefaultAction(this, EventArgs.Empty);
		}

		public event GetStringHandler GetDefaultActionDescription;

		protected virtual string OnGetDefaultActionDescription()
		{
			if (GetDefaultActionDescription != null)
				return GetDefaultActionDescription(this);
			else
				return String.Empty;
		}

		#endregion

		#region Sizing & Layout

		private bool _initialLayout = true;
		
		private bool _autoResize = true;	// True while the user has not manually resized Form
		[DefaultValue(true)]
		public virtual bool AutoResize
		{ 
			get { return _autoResize; }
			set 
			{
				if (_autoResize != value)
				{
					InternalSetAutoResize(value);
					if (value)
						PerformLayout(this, "Bounds");
				}
			}
		}

		private void InternalSetAutoResize(bool tempValue)
		{
			if (tempValue != _autoResize)
			{
				_autoResize = tempValue;
				UpdateNaturalSizeMenuItem();
			}
		}

		/// <summary> Returns the total bordering area surrounding the client box. </summary>
		public virtual Size GetBorderSize()
		{
			Size size = (Size - DisplayRectangle.Size);
			size.Height += 
				(this.FMainMenu.Visible ? this.FMainMenu.Height : 0)
					+ (this.FToolBar.Visible ? this.FToolBar.Height : 0)
					+ (this.FStatusBar.Visible ? this.FStatusBar.Height : 0);
			return size;
		}

		private bool _performingResize;

		private void InternalSizeToNatural()
		{
			Size naturalSize = OnGetNaturalSize() + GetBorderSize();
			// Don't size larger than the screen
			Rectangle maxWorkingArea = (_initialLayout ? Screen.FromPoint(Control.MousePosition).WorkingArea : Screen.FromControl(this).WorkingArea);
			Element.ConstrainMax(ref naturalSize, new Size(maxWorkingArea.Width, maxWorkingArea.Height));
			Rectangle proposed = new Rectangle(Location, naturalSize);
			proposed.X += Math.Min((maxWorkingArea.Right - proposed.Right), 0);
			proposed.Y += Math.Min((maxWorkingArea.Bottom - proposed.Bottom), 0);

			_performingResize = true;
			try
			{
				this.Bounds = proposed;
			}
			finally
			{
				_performingResize = false;
			}
			InternalSetAutoResize(true);
		}

		protected override void OnLayout(LayoutEventArgs args)
		{
			if
			(
				Visible && !Disposing && IsHandleCreated
					&& (args.AffectedProperty != "PreferredSize")
			)
			{
				FContentPanel.AutoScroll = false;

				// Size the form before layout so that dockers will layout to the appropriate size
				if (_autoResize)
					InternalSizeToNatural();

				FContentPanel.BringToFront();	// Otherwise, sometimes the layout will lay the toolbar over the content

				base.OnLayout(args);

				OnLayoutContents();

				if (_initialLayout)
				{
					_initialLayout = false;

					// if initially laying out, manually position the form centered (setting the StartPosition doesn't seem to do it)
					if ((StartPosition == FormStartPosition.CenterScreen) && !DesignMode)
					{
						Rectangle workingArea = Screen.FromPoint(Control.MousePosition).WorkingArea;
						Location =
							new Point
							(
								((workingArea.Width - Width) / 2) + workingArea.X,
								((workingArea.Height - Height) / 2) + workingArea.Y
							);
					}
				}

				FContentPanel.AutoScroll = true;
			}
		}

		protected override void OnResize(EventArgs args)
		{
			// Update the AutoResize property if the user manually resizes the form
			if (!_performingResize)
				InternalSetAutoResize(_autoResize && (OnGetNaturalSize() + GetBorderSize()) == Size);

			base.OnResize(args);
		}

		public event EventHandler LayoutContents;

		protected virtual void OnLayoutContents()
		{
			if (LayoutContents != null)
				LayoutContents(this, EventArgs.Empty);
		}

		public event GetSizeHandler GetNaturalSize;

		protected virtual Size OnGetNaturalSize()
		{
			if (GetNaturalSize != null)
				return GetNaturalSize(this);
			else
				return Size.Empty;
		}

		void IBaseForm.PerformLayout()
		{
			PerformLayout(this, "Bounds");
		}

		void IBaseForm.ResumeLayout(bool performLayout)
		{
			base.ResumeLayout(false);	// Don't use the base perform layout overload, the layout will appear to come from the ToolBar (we want it to come from the Form)
			if (performLayout)
				PerformLayout(this, "Bounds");
		}

		#endregion

		#region Natural Size Menu Item

		private void UpdateSysMenuItem(int iD, bool disable)
		{
			UnsafeNativeMethods.EnableMenuItem
			(
				UnsafeNativeMethods.GetSystemMenu(Handle, false),
				iD,
				NativeMethods.MF_BYCOMMAND | (disable ? (NativeMethods.MF_GRAYED | NativeMethods.MF_DISABLED ) : NativeMethods.MF_ENABLED)
			);
		}

		private void UpdateNaturalSizeMenuItem()
		{
			UpdateSysMenuItem(NativeMethods.SC_NATURALSIZE, _autoResize || (WindowState != FormWindowState.Normal));
		}

		protected override void OnHandleCreated(EventArgs args)
		{
			base.OnHandleCreated(args);
			string itemText = "Natural Size";
			IntPtr sysMenuHandle = UnsafeNativeMethods.GetSystemMenu(Handle, false);
			NativeMethods.MenuItemInfo info = new NativeMethods.MenuItemInfo();
			info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.MenuItemInfo));
			info.fMask = NativeMethods.MIIM_STRING | NativeMethods.MIIM_STATE | NativeMethods.MIIM_ID | NativeMethods.MIIM_DATA;
			info.fType = NativeMethods.MFT_STRING;
			info.fState = NativeMethods.MFS_DEFAULT;
			info.dwItemData = NativeMethods.SC_NATURALSIZE;
			info.wID = NativeMethods.SC_NATURALSIZE;
			info.dwTypeData = itemText;
			info.cch = itemText.Length;
			info.hSubMenu = IntPtr.Zero;
			info.hbmpChecked = IntPtr.Zero;
			info.hbmpUnchecked = IntPtr.Zero;
			UnsafeNativeMethods.InsertMenuItem(sysMenuHandle, 5, true, ref info);
			UnsafeNativeMethods.DrawMenuBar(Handle);
			UpdateNaturalSizeMenuItem();
		}

		protected override void WndProc(ref Message message)
		{
			switch (message.Msg)
			{
				case NativeMethods.WM_SYSCOMMAND :
					if (message.WParam == (IntPtr)NativeMethods.SC_NATURALSIZE)
						AutoResize = true;
					break;
			}
			base.WndProc(ref message);
		}

		#endregion

		#region Focus advancement

		// Enter navigates 

		private bool _enterNavigates = true;
		public virtual bool EnterNavigates
		{ 
			get { return _enterNavigates; } 
			set
			{
				if (value != _enterNavigates)
				{
					_enterNavigates = value;
					UpdateStatusText();
				}
			}
		}

		/// <summary> Returns true if the last control in the tab stops is active. </summary>
		/// <returns> True if last control is active, or no control is active. </returns>
		public bool LastControlActive()
		{
			IntPtr focusedHandle = UnsafeNativeMethods.GetFocus();
			if (focusedHandle == IntPtr.Zero)
				return false;
			Control active = Control.FromChildHandle(focusedHandle);
			if (active == null)
				return false;
			while (active != null)
			{
				active = GetNextControl(active, true);
				if 
				(
					(active != null)
						&& active.CanSelect 
						&& active.CanFocus
						&& active.TabStop
						&& !ControlProcessesEnter(active)
				)
					return false;
			}
			return true;
		}

		private bool ControlProcessesEnter(Control control)
		{
			// Reflection is necessary because IsInputKey is a protected member
			MethodInfo method = control.GetType().GetMethod("IsInputKey", BindingFlags.Instance | BindingFlags.NonPublic);
			if (method != null)
				return (bool)method.Invoke(control, new object[] {Keys.Enter});
			else
				return false;
		}

		public bool ActiveControlProcessesEnter()
		{
			Control active = ActiveControl;
			if (active != null)
				return ControlProcessesEnter(active);
			else
				return false;
		}

		private bool ProcessEnter(Form form, Keys key)
		{
			if (!_enterNavigates || LastControlActive())
				OnDefaultAction();
			else
				AdvanceFocus(true);
			return true;
		}

		public virtual void AdvanceFocus(bool forward)
		{
			SelectNextControl(ActiveControl, forward, true, true, false);
		}

		#if TRACEFOCUS

		private static string WalkParentControls(Control AControl)
		{
			if (AControl == null)
				return "";
			else
				return 
					WalkParentControls(AControl.Parent)
						+ "->"
						+ (AControl.Parent != null ? "(" + AControl.Parent.Controls.IndexOf(AControl).ToString() + ")" : "")
						+ (AControl.Name != null ? AControl.Name : "")
						+ (AControl.Text != null ? "\"" + AControl.Name + "\"" : "")
						+ "[" + AControl.GetType().Name + "]";
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet=System.Runtime.InteropServices.CharSet.Auto, ExactSpelling=true)]
		public static extern IntPtr GetFocus();
 
		private bool ProcessQueryFocus(Form AForm, Keys AKey)
		{
			IntPtr LFocusPtr = GetFocus();
			if (LFocusPtr != IntPtr.Zero)
			{
				Control LControl = Control.FromHandle(LFocusPtr);
				if (LControl != null)
					System.Diagnostics.Trace.WriteLine("Focus: " + WalkParentControls(LControl));
			}
			return true;
		}

		#endif

		#endregion

		#region Status text

		// StatusBar

		public StatusStrip StatusBar
		{
			get { return FStatusBar; }
		}

		public virtual void SetHintText(string text)
		{
			FHintPanel.Text = text;
		}

		protected void SetStatusText(string text)
		{
			FStatusPanel.Text = text;
		}

		public virtual void UpdateStatusText()
		{
			string text;
			if (!_enterNavigates || LastControlActive())
			{
				string defaultActionDescription = OnGetDefaultActionDescription();
				if (!String.IsNullOrEmpty(defaultActionDescription))
				{
					if (ActiveControlProcessesEnter())
						text = Strings.F9ToDefault;
					else
						text = Strings.EnterToDefault;
					text = String.Format(text, defaultActionDescription);
				}
				else
					text = "";
			}
			else
			{
				if (ActiveControlProcessesEnter())
					text = Strings.CtrlTabToContinue;
				else
					text = Strings.EnterToContinue;
			}
			SetStatusText(text);
		}

		#endregion

		#region Embedded errors

		private ErrorList _errorList;
		private ToolStripButton _errorButton;
		
		public virtual void EmbedErrors(ErrorList errorList)
		{
			if (_errorList == null)
			{
				_errorButton = new ToolStripButton();
				using (Stream stream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.Frontend.Client.Windows.Images.Warning.ico"))
				{
					using (Icon icon = new Icon(stream, 16, 16))
						_errorButton.Image = icon.ToBitmap();
				}
				_errorButton.ImageTransparentColor = Color.FromArgb(13, 11, 12);
				_errorButton.AutoSize = true;
				_errorButton.Click += new EventHandler(EmbeddedErrorsClicked);
				FStatusBar.Items.Add(_errorButton);
				_errorList = errorList;
			}
			else
				_errorList.AddRange(errorList);

			_errorButton.Visible = _errorList.Count > 0;
		}

		private void EmbeddedErrorsClicked(object sender, EventArgs args)
		{
			ErrorListForm.ShowErrorList(_errorList, true);
		}

		#endregion

		#region ToolBar & Exposed

		// ToolBar

		private void ToolBarItemAdded(object sender, ToolStripItemEventArgs args)
		{
			UpdateToolBarVisible();
		}

		private void ToolBarItemDeleted(object sender, ToolStripItemEventArgs args)
		{
			UpdateToolBarVisible();
		}

		private void UpdateToolBarVisible()
		{
			FToolBar.Visible = FToolBar.Items.Count > 0;
		}

		private IWindowsBarContainer _exposedContainer;
		public virtual IWindowsBarContainer ExposedContainer
		{
			get { return _exposedContainer; }
		}

		#endregion

		#region Menu & Custom menu

		// Menu

		private IWindowsBarContainer _menuContainer;
		public virtual IWindowsBarContainer MenuContainer
		{
			get { return _menuContainer; }
		}

		private List<ToolStripMenuItem> _customActions;

		public virtual object AddCustomAction(string text, System.Drawing.Image image, EventHandler handler)
		{
			ToolStripMenuItem item = new ToolStripMenuItem(text, image);
			item.Click += handler;
			if (_customActions == null)
				_customActions = new List<ToolStripMenuItem>();
			_customActions.Add(item);
			FFormMenu.DropDownItems.Insert(0, item);
			return item;
		}

		public virtual void RemoveCustomAction(object action)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)action;
			if (_customActions != null)
				_customActions.Remove(item);
			FFormMenu.DropDownItems.Remove(item);
			item.Dispose();
		}

		public virtual void ClearCustomActions()
		{
			if (_customActions != null)
			{
				while (_customActions.Count > 0)
					RemoveCustomAction(_customActions[_customActions.Count - 1]);
				_customActions = null;
			}
		}

		#endregion

		#region Images

		protected static Bitmap LoadBitmap(string resourceName)
		{
			return new Bitmap(typeof(BaseForm).Assembly.GetManifestResourceStream(resourceName));
		}

		static BaseForm()
		{
			_acceptButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Accept.png");
			_rejectButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Reject.png");
			_closeButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Close.png");
		}

		private static System.Drawing.Image _acceptButtonImage;
		private static System.Drawing.Image _rejectButtonImage;
		private static System.Drawing.Image _closeButtonImage;

		#endregion

		#region Accept/Reject vs. Close handling

		protected void DisposeItem(IDisposable item)
		{
			if (item != null)
				item.Dispose();
		}

		private ToolStripButton _acceptButton;
		private ToolStripButton _rejectButton;
		private ToolStripButton _closeButton;

		private ToolStripMenuItem _acceptMenuItem;
		private ToolStripMenuItem _rejectMenuItem;
		private ToolStripMenuItem _closeMenuItem;

		private void InitializeAcceptReject()
		{
			_acceptButton = new ToolStripButton
			(
				Strings.AcceptButtonText,
				_acceptButtonImage,
				new EventHandler(AcceptClick)
			);
			_rejectButton = new ToolStripButton
			(
				Strings.RejectButtonText,
				_rejectButtonImage,
				new EventHandler(RejectClick)
			);
			_closeButton = new ToolStripButton
			(
				Strings.CloseButtonText,
				_closeButtonImage,
				new EventHandler(CloseClick)
			);

			_acceptMenuItem = new ToolStripMenuItem
			(
				Strings.AcceptButtonText,
				_acceptButtonImage,
				new EventHandler(AcceptClick)
			);
			_acceptMenuItem.ShortcutKeys = Keys.F9;
			_rejectMenuItem = new ToolStripMenuItem
			(
				Strings.RejectButtonText,
				_rejectButtonImage,
				new EventHandler(RejectClick)
			);
			_closeMenuItem = new ToolStripMenuItem
			(
				Strings.CloseButtonText,
				_closeButtonImage,
				new EventHandler(CloseClick)
			);
			_closeMenuItem.ShortcutKeys = Keys.F9;
		}

		private void UninitializeAcceptReject(object sender, EventArgs args)
		{
			UninitializeAcceptReject();
		}

		private void UninitializeAcceptReject()
		{
			DisposeItem(_acceptButton);
			DisposeItem(_rejectButton);
			DisposeItem(_closeButton);
			DisposeItem(_acceptMenuItem);
			DisposeItem(_rejectMenuItem);
			DisposeItem(_closeMenuItem);
		}

		private void AddToToolBar(ToolStripItem item, int position)
		{
			FToolBar.Items.Insert(position, item);
		}

		private void RemoveFromToolBar(ToolStripItem item)
		{
			FToolBar.Items.Remove(item);
		}

		private void AddMenu(ToolStripItem menu)
		{
			FFormMenu.DropDownItems.Add(menu);
		}

		private void RemoveMenu(ToolStripItem menu)
		{
			FFormMenu.DropDownItems.Remove(menu);
		}

		private bool _isAcceptReject;

		public virtual void SetAcceptReject(bool isAcceptReject, bool supressCloseButton)
		{
			if (!IsDisposed)	// TODO: Something better here to determine if form is closing
			{
				FMainMenu.SuspendLayout();
				FToolBar.SuspendLayout();
				try
				{
					if (isAcceptReject)
					{
						_closeMenuItem.ShortcutKeys = Keys.None;	// HACK: If this shortcut key isn't changed, the key for accept does not work (presumably because there is a conflict for a moment)
						AddMenu(_acceptMenuItem);
						AddMenu(_rejectMenuItem);
						RemoveMenu(_closeMenuItem);
						_acceptMenuItem.ShortcutKeys = Keys.F9;

						AddToToolBar(_acceptButton, 0);
						AddToToolBar(_rejectButton, 1);
						RemoveFromToolBar(_closeButton);
					}
					else
					{
						_acceptMenuItem.ShortcutKeys = Keys.None;
						AddMenu(_closeMenuItem);
						RemoveMenu(_acceptMenuItem);
						RemoveMenu(_rejectMenuItem);
						_closeMenuItem.ShortcutKeys = Keys.F9;

						if (!supressCloseButton)
							AddToToolBar(_closeButton, 0);
						RemoveFromToolBar(_acceptButton);
						RemoveFromToolBar(_rejectButton);
					}
					_isAcceptReject = isAcceptReject;
				}
				finally
				{
					FToolBar.ResumeLayout();
					FMainMenu.ResumeLayout();
				}
			}
		}

		protected void CloseClick(object sender, EventArgs args)
		{
			if (Enabled)
				Close(CloseBehavior.AcceptOrClose);
		}

		// Accept Enabled

		private bool _acceptEnabled = true;

		public bool AcceptEnabled
		{
			get { return _acceptEnabled; }
			set { _acceptEnabled = value; }
		}

		public event EventHandler Accepting;  	
		private void AcceptClick(object sender, EventArgs args)
		{
			if (Accepting != null)
				Accepting(this, EventArgs.Empty);
			if (Enabled && AcceptEnabled)
				Close(CloseBehavior.AcceptOrClose);
		}
	
		protected void RejectClick(object sender, EventArgs args)
		{
			if (Enabled)
				Close(CloseBehavior.RejectOrClose);
		}

		public virtual void Show(IFormInterface parent)
		{
			if (parent != null)
				Owner = (Form)((FormInterface)parent).Form;
			base.Show();
		}
		
		public virtual void Close(CloseBehavior behavior)
		{
			if (_isAcceptReject)
			{
				if (behavior == CloseBehavior.AcceptOrClose)
					DialogResult = DialogResult.OK;
				else
					DialogResult = DialogResult.Cancel;
			}
			else	// Close
				DialogResult = DialogResult.Abort;
			if (!Modal)	// Not truly modal
				UnsafeNativeMethods.PostMessage(Handle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);	//Close();   Don't call close directly here so that callers can safely complete their message loop before the form goes away.
		}

		protected override void OnClosing(CancelEventArgs args)
		{
			// Dissociate all owned forms before closing to ensure predictable close event behavior
			foreach (System.Windows.Forms.Form form in OwnedForms)
				form.Owner = null;

			base.OnClosing(args);
		}

		#endregion

		#region Begin/End Update

		private int _updateCount;
		protected bool InUpdate
		{
			get { return _updateCount > 0; }
		}

		public virtual void BeginUpdate()
		{
			if (++_updateCount == 1)
				SetUpdateState(true);
		}

		public virtual void EndUpdate()
		{
			if (--_updateCount <= 0)
			{
				_updateCount = 0;
				SetUpdateState(false);
			}
		}

		private void SetUpdateState(bool updating)
		{
			UnsafeNativeMethods.SendMessage(ContentPanel.Handle, NativeMethods.WM_SETREDRAW, !updating, IntPtr.Zero);
			if (!updating)
				this.Invalidate(true);
		}

		#endregion

		#region Keyboard Handling

		private Dictionary<Keys, DialogKeyHandler> _dialogKeys = new Dictionary<Keys, DialogKeyHandler>();
		public Dictionary<Keys, DialogKeyHandler> DialogKeys { get { return _dialogKeys; } }

		protected override bool ProcessDialogKey(Keys keyData)
		{
			DialogKeyHandler handler;
			if (_dialogKeys.TryGetValue(keyData, out handler) && handler(this, keyData))
				return true;
			else
				return base.ProcessDialogKey(keyData);
		}

		#endregion

		#region Lookup

		private bool _isLookup;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsLookup
		{
			get { return _isLookup; }
			set
			{
				if (_isLookup != value)
				{
					_isLookup = value;
					UpdateIsLookup();
				}
			}
		}

		protected void UpdateIsLookup()
		{
			if (_isLookup)
			{
				ShowInTaskbar = false;
				FormBorderStyle = FormBorderStyle.None;
				ShowIcon = false;
			}
			else
			{
				ShowInTaskbar = true;
				FormBorderStyle = FormBorderStyle.Sizable;
				ShowIcon = true;
			}
		}

		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);
			// TODO: figure out how to get the form to close on loss of activation, but only if no child-modal forms are shown.  The disabled logic below doesn't seem to be picking up owned forms properly.
			//if (FIsLookup && !Disposing && (OwnedForms.Length == 0) && (DialogResult == DialogResult.None))
			    //Close(CloseBehavior.RejectOrClose);
		}

		private const int S_DROPSHADOW = 0x00020000;
		
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams result = base.CreateParams;
				// If this form is a lookup, put a small border and a drop shadow on it
				if (_isLookup)
				{
					result.Style |= 0x800000;
					result.ClassStyle |= S_DROPSHADOW;
				}
				return result;
			}
		}

		#endregion

		#region Hacks

		protected override void OnVisibleChanged(EventArgs args)
		{
			// HACK: This is to avoid the excess layouts performed by Bars during form showing
			if (Visible)
				SuspendLayout();
			try
			{
				base.OnVisibleChanged(args);
			}
			finally
			{
				if (Visible)
					ResumeLayout(true);
			}
		}

		protected override void OnEnabledChanged(EventArgs args)
		{
			// HACK: Don't call base... very slow.  We'll do it ourselves
			//base.OnEnabledChanged(AArgs);

			UnsafeNativeMethods.EnableWindow(Handle, Enabled);
			Invalidate(true);
			if (Enabled)
				BringToFront();

			// HACK: If the form was activated before it was enabled, the focus is lost
			if ((Form.ActiveForm == this) && Enabled)
			{
				if (this.ActiveControl == null)
					this.SelectNextControl(null, true, true, true, false);
				ContainerControl control = (ContainerControl)typeof(ContainerControl).GetProperty("InnerMostActiveContainerControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this, new object[] {});
				typeof(ContainerControl).GetMethod("FocusActiveControlInternal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(control, new object[] {});
			} 	
		}

		protected override bool ProcessCmdKey(ref Message message, Keys keyData)
		{
			bool result;
			try
			{
				result = base.ProcessCmdKey(ref message, keyData);
			}
			catch (Exception exception)
			{
				// HACK: This works around the issue where keys are not treated as handled when an exception is thrown
				Session.HandleException(exception);
				return true;
			}
			return result;
		}

		#endregion
	}

	public delegate void PaintHandledEventHandler(object ASender, PaintEventArgs AArgs, out bool AHandled);

	public delegate Size GetSizeHandler(object ASender);

	public delegate string GetStringHandler(object ASender);

	public delegate bool DialogKeyHandler(Form AForm, Keys AKey);

	public struct BarItemComparer : IComparable
	{
		public BarItemComparer(IWindowsBarItem AItem, GetPriorityHandler AGetPriority)
		{
			Item = AItem;
			GetPriority = AGetPriority;
		}

		public IWindowsBarItem Item;
		public GetPriorityHandler GetPriority;

		public int CompareTo(object AObject)
		{
			BarItemComparer LOther = (BarItemComparer)AObject;
			int LOtherPriority = Int32.MaxValue;
			if (LOther.GetPriority != null)
				LOtherPriority = LOther.GetPriority(LOther.Item);
			int LThisPriority = Int32.MaxValue;
			if (GetPriority != null)
				LThisPriority = GetPriority(Item);
			
			if (LOtherPriority == LThisPriority)
				return Item.GetHashCode().CompareTo(LOther.Item.GetHashCode());	// don't allow ties, arbitrarily distinuish them
			else
				return LThisPriority.CompareTo(LOtherPriority);
		}
	}

	public abstract class FormMenuContainerBase : IWindowsBarContainer
	{
		public abstract void Dispose();

		protected ToolStripItemCollection _items;
		public ToolStripItemCollection Items { get { return _items; } }
	
		public virtual IWindowsBarContainer CreateContainer()
		{
			return new FormMenuContainer();
		}

		public virtual IWindowsBarButton CreateMenuItem(System.EventHandler handler)
		{
			IWindowsBarButton item = new FormMenuContainer();
			item.Item.Click += handler;
			return item;
		}

		public virtual IWindowsBarSeparator CreateSeparator()
		{
			return new FormMenuSeparator();
		}

		private int _reservedItems = 0;
		/// <summary> Number of menu items to lock to the left and not treat as part of the InsertBarItem index space. </summary>
		public int ReservedItems
		{
			get { return _reservedItems; }
			set { _reservedItems = value; }
		}

		private SortedList<BarItemComparer,IWindowsBarItem> _sortedBarItems = new SortedList<BarItemComparer,IWindowsBarItem>();

		public virtual void AddBarItem(IWindowsBarItem item, GetPriorityHandler getPriority)
		{
			// Add the item to the sorted list
			BarItemComparer newBarItemComparer = new BarItemComparer(item, getPriority);
			_sortedBarItems.Add(newBarItemComparer, item);
			int index = _sortedBarItems.IndexOfKey(newBarItemComparer) + _reservedItems;

			if (index > _items.Count)
				_items.Add(((IToolStripItemContainer)item).Item);
			else
				_items.Insert(index, ((IToolStripItemContainer)item).Item);
		}

		public virtual void RemoveBarItem(IWindowsBarItem item)
		{
			_items.Remove(((IToolStripItemContainer)item).Item);
			_sortedBarItems.RemoveAt(_sortedBarItems.IndexOfValue(item));
		}

		public abstract bool Visible { get; set; }
	}

	public interface IToolStripItemContainer
	{
		ToolStripItem Item { get; }
	}

	public class FormMenuContainer : FormMenuContainerBase, IWindowsBarButton
	{
		public FormMenuContainer() : base()
		{
			_item = new ToolStripMenuItem();
			_items = _item.DropDownItems;
		}

		public override void Dispose()
		{
			if (_item != null)
			{
				_item.Dispose();
				_item = null;
			}
		}

		private ToolStripMenuItem _item;
		public ToolStripItem Item { get { return _item; } }

		public string Text
		{
			get { return _item.Text; }
			set { _item.Text = value; }
		}

		public bool Enabled
		{
			get { return _item.Enabled; }
			set { _item.Enabled = value; }
		}

		public System.Drawing.Image Image
		{
			get { return _item.Image; }
			set { _item.Image = value; }
		}

		public override bool Visible
		{
			get { return _item.Visible; }
			set { _item.Visible = value; }
		}
	}

	public class FormMenuSeparator : IWindowsBarItem, IWindowsBarSeparator, IToolStripItemContainer
	{
		public FormMenuSeparator()
		{
			_item = new ToolStripSeparator();
		}

		public virtual void Dispose() 
		{
			if (_item != null)
			{
				_item.Dispose();
				_item = null;
			}
		}

		private ToolStripSeparator _item;
		public ToolStripItem Item { get { return _item; } }

		public bool Visible
		{
			get { return _item.Visible; }
			set { _item.Visible = value; }
		}
	}

	public class FormMainMenuContainer : FormMenuContainerBase, IWindowsBarItem
	{
		public FormMainMenuContainer(MenuStrip mainMenu) : base()
		{
			_mainMenu = mainMenu;
			_items = mainMenu.Items;
		}

		public override void Dispose() {}

		private MenuStrip _mainMenu;
		public MenuStrip MainMenu { get { return _mainMenu; } }

		public override bool Visible
		{
			get { return _mainMenu.Visible; }
			set { _mainMenu.Visible = value; }
		}
	}

	public class FormButton : IWindowsBarButton, IToolStripItemContainer
	{
		public FormButton(EventHandler handler)
		{
			_item = new ToolStripButton();
			_item.Click += handler;
		}

		public virtual void Dispose()
		{
			if (_item != null)
			{
				_item.Dispose();
				_item = null;
			}
		}

		private ToolStripButton _item;
		public ToolStripButton ButtonItem { get { return _item; } }
		public ToolStripItem Item { get { return _item; } }

		public string Text
		{
			get { return _item.Text; }
			set { _item.Text = value; }
		}

		public bool Enabled
		{
			get { return _item.Enabled; }
			set { _item.Enabled = value; }
		}

		public System.Drawing.Image Image
		{
			get { return _item.Image; }
			set { _item.Image = value; }
		}

		public bool Visible
		{
			get { return _item.Visible; }
			set { _item.Visible = value; }
		}
	}

	public class FormExposedContainer : IWindowsBarContainer
	{
		public FormExposedContainer(ToolStrip toolBar)
		{
			_toolBar = toolBar;
		}

		public virtual void Dispose() {}

		private ToolStrip _toolBar;
		public ToolStrip ToolBar { get { return _toolBar; } }

		public IWindowsBarContainer CreateContainer()
		{
			Error.Fail("CreateContainer() is not supported for FormExposedContainer");
			return null;
		}

		public IWindowsBarButton CreateMenuItem(System.EventHandler handler)
		{
			return new FormButton(handler);
		}

		public IWindowsBarSeparator CreateSeparator()
		{
			Error.Fail("CreateSeparator() is not supported for FormExposedContainer");
			return null;
		}

		/// <remarks> The AGetPriority handler is ignored. </remarks>
		public void AddBarItem(IWindowsBarItem item, GetPriorityHandler getPriority)
		{
			_toolBar.Items.Add(((IToolStripItemContainer)item).Item);
		}

		public void RemoveBarItem(IWindowsBarItem item)
		{
			_toolBar.Items.Remove(((IToolStripItemContainer)item).Item);
		}

		public bool Visible
		{
			get { return _toolBar.Visible; }
			set { _toolBar.Visible = value; }
		}
	}
}