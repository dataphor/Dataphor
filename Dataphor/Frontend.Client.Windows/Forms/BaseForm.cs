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
			FMenuContainer = new FormMainMenuContainer(FMainMenu);
			((FormMainMenuContainer)FMenuContainer).ReservedItems = 1;
			FExposedContainer = new FormExposedContainer(FToolBar);

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

		protected override void OnPaintBackground(PaintEventArgs AArgs)
		{
			// Don't bother painting the form's background, it will not be seen
		}

		private void ContentPanelPaint(object ASender, PaintEventArgs AArgs)
		{
			if (PaintBackground != null)
			{
				bool LHandled;
				PaintBackground(ASender, AArgs, out LHandled);
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

		private bool FInitialLayout = true;
		
		private bool FAutoResize = true;	// True while the user has not manually resized Form
		[DefaultValue(true)]
		public virtual bool AutoResize
		{ 
			get { return FAutoResize; }
			set 
			{
				if (FAutoResize != value)
				{
					InternalSetAutoResize(value);
					if (value)
						PerformLayout(this, "Bounds");
				}
			}
		}

		private void InternalSetAutoResize(bool AValue)
		{
			if (AValue != FAutoResize)
			{
				FAutoResize = AValue;
				UpdateNaturalSizeMenuItem();
			}
		}

		/// <summary> Returns the total bordering area surrounding the client box. </summary>
		public virtual Size GetBorderSize()
		{
			Size LSize = (Size - DisplayRectangle.Size);
			LSize.Height += 
				(this.FMainMenu.Visible ? this.FMainMenu.Height : 0)
					+ (this.FToolBar.Visible ? this.FToolBar.Height : 0)
					+ (this.FStatusBar.Visible ? this.FStatusBar.Height : 0);
			return LSize;
		}

		private bool FPerformingResize;

		private void InternalSizeToNatural()
		{
			Size LNaturalSize = OnGetNaturalSize() + GetBorderSize();
			// Don't size larger than the screen
			Rectangle LMaxWorkingArea = (FInitialLayout ? Screen.FromPoint(Control.MousePosition).WorkingArea : Screen.FromControl(this).WorkingArea);
			Element.ConstrainMax(ref LNaturalSize, new Size(LMaxWorkingArea.Width, LMaxWorkingArea.Height));
			Rectangle LProposed = new Rectangle(Location, LNaturalSize);
			LProposed.X += Math.Min((LMaxWorkingArea.Right - LProposed.Right), 0);
			LProposed.Y += Math.Min((LMaxWorkingArea.Bottom - LProposed.Bottom), 0);

			FPerformingResize = true;
			try
			{
				this.Bounds = LProposed;
			}
			finally
			{
				FPerformingResize = false;
			}
			InternalSetAutoResize(true);
		}

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			if
			(
				Visible && !Disposing && IsHandleCreated
					&& (AArgs.AffectedProperty != "PreferredSize")
			)
			{
				FContentPanel.AutoScroll = false;

				// Size the form before layout so that dockers will layout to the appropriate size
				if (FAutoResize)
					InternalSizeToNatural();

				FContentPanel.BringToFront();	// Otherwise, sometimes the layout will lay the toolbar over the content

				base.OnLayout(AArgs);

				OnLayoutContents();

				if (FInitialLayout)
				{
					FInitialLayout = false;

					// if initially laying out, manually position the form centered (setting the StartPosition doesn't seem to do it)
					if ((StartPosition == FormStartPosition.CenterScreen) && !DesignMode)
					{
						Rectangle LWorkingArea = Screen.FromPoint(Control.MousePosition).WorkingArea;
						Location =
							new Point
							(
								((LWorkingArea.Width - Width) / 2) + LWorkingArea.X,
								((LWorkingArea.Height - Height) / 2) + LWorkingArea.Y
							);
					}
				}

				FContentPanel.AutoScroll = true;
			}
		}

		protected override void OnResize(EventArgs AArgs)
		{
			// Update the AutoResize property if the user manually resizes the form
			if (!FPerformingResize)
				InternalSetAutoResize(FAutoResize && (OnGetNaturalSize() + GetBorderSize()) == Size);

			base.OnResize(AArgs);
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

		void IBaseForm.ResumeLayout(bool APerformLayout)
		{
			base.ResumeLayout(false);	// Don't use the base perform layout overload, the layout will appear to come from the ToolBar (we want it to come from the Form)
			if (APerformLayout)
				PerformLayout(this, "Bounds");
		}

		#endregion

		#region Natural Size Menu Item

		private void UpdateSysMenuItem(int AID, bool ADisable)
		{
			UnsafeNativeMethods.EnableMenuItem
			(
				UnsafeNativeMethods.GetSystemMenu(Handle, false),
				AID,
				NativeMethods.MF_BYCOMMAND | (ADisable ? (NativeMethods.MF_GRAYED | NativeMethods.MF_DISABLED ) : NativeMethods.MF_ENABLED)
			);
		}

		private void UpdateNaturalSizeMenuItem()
		{
			UpdateSysMenuItem(NativeMethods.SC_NATURALSIZE, FAutoResize || (WindowState != FormWindowState.Normal));
		}

		protected override void OnHandleCreated(EventArgs AArgs)
		{
			base.OnHandleCreated(AArgs);
			string LItemText = "Natural Size";
			IntPtr LSysMenuHandle = UnsafeNativeMethods.GetSystemMenu(Handle, false);
			NativeMethods.MenuItemInfo LInfo = new NativeMethods.MenuItemInfo();
			LInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.MenuItemInfo));
			LInfo.fMask = NativeMethods.MIIM_STRING | NativeMethods.MIIM_STATE | NativeMethods.MIIM_ID | NativeMethods.MIIM_DATA;
			LInfo.fType = NativeMethods.MFT_STRING;
			LInfo.fState = NativeMethods.MFS_DEFAULT;
			LInfo.dwItemData = NativeMethods.SC_NATURALSIZE;
			LInfo.wID = NativeMethods.SC_NATURALSIZE;
			LInfo.dwTypeData = LItemText;
			LInfo.cch = LItemText.Length;
			LInfo.hSubMenu = IntPtr.Zero;
			LInfo.hbmpChecked = IntPtr.Zero;
			LInfo.hbmpUnchecked = IntPtr.Zero;
			UnsafeNativeMethods.InsertMenuItem(LSysMenuHandle, 5, true, ref LInfo);
			UnsafeNativeMethods.DrawMenuBar(Handle);
			UpdateNaturalSizeMenuItem();
		}

		protected override void WndProc(ref Message AMessage)
		{
			switch (AMessage.Msg)
			{
				case NativeMethods.WM_SYSCOMMAND :
					if (AMessage.WParam == (IntPtr)NativeMethods.SC_NATURALSIZE)
						AutoResize = true;
					break;
			}
			base.WndProc(ref AMessage);
		}

		#endregion

		#region Focus advancement

		// Enter navigates 

		private bool FEnterNavigates = true;
		public virtual bool EnterNavigates
		{ 
			get { return FEnterNavigates; } 
			set
			{
				if (value != FEnterNavigates)
				{
					FEnterNavigates = value;
					UpdateStatusText();
				}
			}
		}

		/// <summary> Returns true if the last control in the tab stops is active. </summary>
		/// <returns> True if last control is active, or no control is active. </returns>
		public bool LastControlActive()
		{
			IntPtr LFocusedHandle = UnsafeNativeMethods.GetFocus();
			if (LFocusedHandle == IntPtr.Zero)
				return false;
			Control LActive = Control.FromChildHandle(LFocusedHandle);
			if (LActive == null)
				return false;
			while (LActive != null)
			{
				LActive = GetNextControl(LActive, true);
				if 
				(
					(LActive != null)
						&& LActive.CanSelect 
						&& LActive.CanFocus
						&& LActive.TabStop
						&& !ControlProcessesEnter(LActive)
				)
					return false;
			}
			return true;
		}

		private bool ControlProcessesEnter(Control AControl)
		{
			// Reflection is necessary because IsInputKey is a protected member
			MethodInfo LMethod = AControl.GetType().GetMethod("IsInputKey", BindingFlags.Instance | BindingFlags.NonPublic);
			if (LMethod != null)
				return (bool)LMethod.Invoke(AControl, new object[] {Keys.Enter});
			else
				return false;
		}

		public bool ActiveControlProcessesEnter()
		{
			Control LActive = ActiveControl;
			if (LActive != null)
				return ControlProcessesEnter(LActive);
			else
				return false;
		}

		private bool ProcessEnter(Form AForm, Keys AKey)
		{
			if (!FEnterNavigates || LastControlActive())
				OnDefaultAction();
			else
				AdvanceFocus(true);
			return true;
		}

		public virtual void AdvanceFocus(bool AForward)
		{
			SelectNextControl(ActiveControl, AForward, true, true, false);
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

		public virtual void SetHintText(string AText)
		{
			FHintPanel.Text = AText;
		}

		protected void SetStatusText(string AText)
		{
			FStatusPanel.Text = AText;
		}

		public virtual void UpdateStatusText()
		{
			string LText;
			if (!FEnterNavigates || LastControlActive())
			{
				if (ActiveControlProcessesEnter())
					LText = Strings.F9ToDefault;
				else
					LText = Strings.EnterToDefault;
				LText = String.Format(LText, OnGetDefaultActionDescription());
			}
			else
			{
				if (ActiveControlProcessesEnter())
					LText = Strings.CtrlTabToContinue;
				else
					LText = Strings.EnterToContinue;
			}
			SetStatusText(LText);
		}

		#endregion

		#region Embedded errors

		private ErrorList FErrorList;
		
		public virtual void EmbedErrors(ErrorList AErrorList)
		{
			if (FErrorList == null)
			{
				ToolStripButton LButton = new ToolStripButton();
				using (Stream LStream = GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.Frontend.Client.Windows.Images.Warning.ico"))
				{
					using (Icon LIcon = new Icon(LStream, 16, 16))
						LButton.Image = LIcon.ToBitmap();
				}
				LButton.ImageTransparentColor = Color.FromArgb(13, 11, 12);
				LButton.AutoSize = true;
				LButton.Click += new EventHandler(EmbeddedErrorsClicked);
				FStatusBar.Items.Add(LButton);
				FErrorList = AErrorList;
			}
			else
				FErrorList.AddRange(AErrorList);
		}

		private void EmbeddedErrorsClicked(object ASender, EventArgs AArgs)
		{
			ErrorListForm.ShowErrorList(FErrorList, true);
		}

		#endregion

		#region ToolBar & Exposed

		// ToolBar

		private void ToolBarItemAdded(object ASender, ToolStripItemEventArgs AArgs)
		{
			UpdateToolBarVisible();
		}

		private void ToolBarItemDeleted(object ASender, ToolStripItemEventArgs AArgs)
		{
			UpdateToolBarVisible();
		}

		private void UpdateToolBarVisible()
		{
			FToolBar.Visible = FToolBar.Items.Count > 0;
		}

		private IWindowsBarContainer FExposedContainer;
		public virtual IWindowsBarContainer ExposedContainer
		{
			get { return FExposedContainer; }
		}

		#endregion

		#region Menu & Custom menu

		// Menu

		private IWindowsBarContainer FMenuContainer;
		public virtual IWindowsBarContainer MenuContainer
		{
			get { return FMenuContainer; }
		}

		private List<ToolStripMenuItem> FCustomActions;

		public virtual object AddCustomAction(string AText, System.Drawing.Image AImage, EventHandler AHandler)
		{
			ToolStripMenuItem LItem = new ToolStripMenuItem(AText, AImage);
			LItem.Click += AHandler;
			if (FCustomActions == null)
				FCustomActions = new List<ToolStripMenuItem>();
			FCustomActions.Add(LItem);
			FFormMenu.DropDownItems.Insert(0, LItem);
			return LItem;
		}

		public virtual void RemoveCustomAction(object AAction)
		{
			ToolStripMenuItem LItem = (ToolStripMenuItem)AAction;
			if (FCustomActions != null)
				FCustomActions.Remove(LItem);
			FFormMenu.DropDownItems.Remove(LItem);
			LItem.Dispose();
		}

		public virtual void ClearCustomActions()
		{
			if (FCustomActions != null)
			{
				while (FCustomActions.Count > 0)
					RemoveCustomAction(FCustomActions[FCustomActions.Count - 1]);
				FCustomActions = null;
			}
		}

		#endregion

		#region Images

		protected static Bitmap LoadBitmap(string AResourceName)
		{
			return new Bitmap(typeof(BaseForm).Assembly.GetManifestResourceStream(AResourceName));
		}

		static BaseForm()
		{
			FAcceptButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Accept.png");
			FRejectButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Reject.png");
			FCloseButtonImage = (System.Drawing.Image)LoadBitmap("Alphora.Dataphor.Frontend.Client.Windows.Images.Close.png");
		}

		private static System.Drawing.Image FAcceptButtonImage;
		private static System.Drawing.Image FRejectButtonImage;
		private static System.Drawing.Image FCloseButtonImage;

		#endregion

		#region Accept/Reject vs. Close handling

		protected void DisposeItem(IDisposable AItem)
		{
			if (AItem != null)
				AItem.Dispose();
		}

		private ToolStripButton FAcceptButton;
		private ToolStripButton FRejectButton;
		private ToolStripButton FCloseButton;

		private ToolStripMenuItem FAcceptMenuItem;
		private ToolStripMenuItem FRejectMenuItem;
		private ToolStripMenuItem FCloseMenuItem;

		private void InitializeAcceptReject()
		{
			FAcceptButton = new ToolStripButton
			(
				Strings.AcceptButtonText,
				FAcceptButtonImage,
				new EventHandler(AcceptClick)
			);
			FRejectButton = new ToolStripButton
			(
				Strings.RejectButtonText,
				FRejectButtonImage,
				new EventHandler(RejectClick)
			);
			FCloseButton = new ToolStripButton
			(
				Strings.CloseButtonText,
				FCloseButtonImage,
				new EventHandler(CloseClick)
			);

			FAcceptMenuItem = new ToolStripMenuItem
			(
				Strings.AcceptButtonText,
				FAcceptButtonImage,
				new EventHandler(AcceptClick)
			);
			FAcceptMenuItem.ShortcutKeys = Keys.F9;
			FRejectMenuItem = new ToolStripMenuItem
			(
				Strings.RejectButtonText,
				FRejectButtonImage,
				new EventHandler(RejectClick)
			);
			FCloseMenuItem = new ToolStripMenuItem
			(
				Strings.CloseButtonText,
				FCloseButtonImage,
				new EventHandler(CloseClick)
			);
			FCloseMenuItem.ShortcutKeys = Keys.F9;
		}

		private void UninitializeAcceptReject(object ASender, EventArgs AArgs)
		{
			UninitializeAcceptReject();
		}

		private void UninitializeAcceptReject()
		{
			DisposeItem(FAcceptButton);
			DisposeItem(FRejectButton);
			DisposeItem(FCloseButton);
			DisposeItem(FAcceptMenuItem);
			DisposeItem(FRejectMenuItem);
			DisposeItem(FCloseMenuItem);
		}

		private void AddToToolBar(ToolStripItem AItem, int APosition)
		{
			FToolBar.Items.Insert(APosition, AItem);
		}

		private void RemoveFromToolBar(ToolStripItem AItem)
		{
			FToolBar.Items.Remove(AItem);
		}

		private void AddMenu(ToolStripItem AMenu)
		{
			FFormMenu.DropDownItems.Add(AMenu);
		}

		private void RemoveMenu(ToolStripItem AMenu)
		{
			FFormMenu.DropDownItems.Remove(AMenu);
		}

		private bool FIsAcceptReject;

		public virtual void SetAcceptReject(bool AIsAcceptReject, bool ASupressCloseButton)
		{
			if (!IsDisposed)	// TODO: Something better here to determine if form is closing
			{
				FMainMenu.SuspendLayout();
				FToolBar.SuspendLayout();
				try
				{
					if (AIsAcceptReject)
					{
						FCloseMenuItem.ShortcutKeys = Keys.None;	// HACK: If this shortcut key isn't changed, the key for accept does not work (presumably because there is a conflict for a moment)
						AddMenu(FAcceptMenuItem);
						AddMenu(FRejectMenuItem);
						RemoveMenu(FCloseMenuItem);
						FAcceptMenuItem.ShortcutKeys = Keys.F9;

						AddToToolBar(FAcceptButton, 0);
						AddToToolBar(FRejectButton, 1);
						RemoveFromToolBar(FCloseButton);
					}
					else
					{
						FAcceptMenuItem.ShortcutKeys = Keys.None;
						AddMenu(FCloseMenuItem);
						RemoveMenu(FAcceptMenuItem);
						RemoveMenu(FRejectMenuItem);
						FCloseMenuItem.ShortcutKeys = Keys.F9;

						if (!ASupressCloseButton)
							AddToToolBar(FCloseButton, 0);
						RemoveFromToolBar(FAcceptButton);
						RemoveFromToolBar(FRejectButton);
					}
					FIsAcceptReject = AIsAcceptReject;
				}
				finally
				{
					FToolBar.ResumeLayout();
					FMainMenu.ResumeLayout();
				}
			}
		}

		protected void CloseClick(object ASender, EventArgs AArgs)
		{
			if (Enabled)
				Close(CloseBehavior.AcceptOrClose);
		}
	
		private void AcceptClick(object ASender, EventArgs AArgs)
		{
			if (Enabled)
				Close(CloseBehavior.AcceptOrClose);
		}
	
		protected void RejectClick(object ASender, EventArgs AArgs)
		{
			if (Enabled)
				Close(CloseBehavior.RejectOrClose);
		}

		public virtual void Show(IFormInterface AParent)
		{
			if (AParent != null)
				Owner = (Form)((FormInterface)AParent).Form;
			base.Show();
		}
		
		public virtual void Close(CloseBehavior ABehavior)
		{
			if (FIsAcceptReject)
			{
				if (ABehavior == CloseBehavior.AcceptOrClose)
					DialogResult = DialogResult.OK;
				else
					DialogResult = DialogResult.Cancel;
			}
			else	// Close
				DialogResult = DialogResult.Abort;
			if (!Modal)	// Not truly modal
				UnsafeNativeMethods.PostMessage(Handle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);	//Close();   Don't call close directly here so that callers can safely complete their message loop before the form goes away.
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			// Dissociate all owned forms before closing to ensure predictable close event behavior
			foreach (System.Windows.Forms.Form LForm in OwnedForms)
				LForm.Owner = null;

			base.OnClosing(AArgs);
		}

		#endregion

		#region Begin/End Update

		private int FUpdateCount;
		protected bool InUpdate
		{
			get { return FUpdateCount > 0; }
		}

		public virtual void BeginUpdate()
		{
			if (++FUpdateCount == 1)
				SetUpdateState(true);
		}

		public virtual void EndUpdate()
		{
			if (--FUpdateCount <= 0)
			{
				FUpdateCount = 0;
				SetUpdateState(false);
			}
		}

		private void SetUpdateState(bool AUpdating)
		{
			UnsafeNativeMethods.SendMessage(ContentPanel.Handle, NativeMethods.WM_SETREDRAW, !AUpdating, IntPtr.Zero);
			if (!AUpdating)
				this.Invalidate(true);
		}

		#endregion

		#region Keyboard Handling

		private Dictionary<Keys, DialogKeyHandler> FDialogKeys = new Dictionary<Keys, DialogKeyHandler>();
		public Dictionary<Keys, DialogKeyHandler> DialogKeys { get { return FDialogKeys; } }

		protected override bool ProcessDialogKey(Keys AKeyData)
		{
			DialogKeyHandler LHandler;
			if (FDialogKeys.TryGetValue(AKeyData, out LHandler) && LHandler(this, AKeyData))
				return true;
			else
				return base.ProcessDialogKey(AKeyData);
		}

		#endregion

		#region Lookup

		private bool FIsLookup;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsLookup
		{
			get { return FIsLookup; }
			set
			{
				if (FIsLookup != value)
				{
					FIsLookup = value;
					UpdateIsLookup();
				}
			}
		}

		protected void UpdateIsLookup()
		{
			if (FIsLookup)
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

		private const int CS_DROPSHADOW = 0x00020000;
		
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams result = base.CreateParams;
				// If this form is a lookup, put a small border and a drop shadow on it
				if (FIsLookup)
				{
					result.Style |= 0x800000;
					result.ClassStyle |= CS_DROPSHADOW;
				}
				return result;
			}
		}

		#endregion

		#region Hacks

		protected override void OnVisibleChanged(EventArgs AArgs)
		{
			// HACK: This is to avoid the excess layouts performed by Bars during form showing
			if (Visible)
				SuspendLayout();
			try
			{
				base.OnVisibleChanged(AArgs);
			}
			finally
			{
				if (Visible)
					ResumeLayout(true);
			}
		}

		protected override void OnEnabledChanged(EventArgs AArgs)
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
				ContainerControl LControl = (ContainerControl)typeof(ContainerControl).GetProperty("InnerMostActiveContainerControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this, new object[] {});
				typeof(ContainerControl).GetMethod("FocusActiveControlInternal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(LControl, new object[] {});
			}
		}

		protected override bool ProcessCmdKey(ref Message AMessage, Keys AKeyData)
		{
			bool LResult;
			try
			{
				LResult = base.ProcessCmdKey(ref AMessage, AKeyData);
			}
			catch (Exception LException)
			{
				// HACK: This works around the issue where keys are not treated as handled when an exception is thrown
				Session.HandleException(LException);
				return true;
			}
			return LResult;
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

		protected ToolStripItemCollection FItems;
		public ToolStripItemCollection Items { get { return FItems; } }
	
		public virtual IWindowsBarContainer CreateContainer()
		{
			return new FormMenuContainer();
		}

		public virtual IWindowsBarButton CreateMenuItem(System.EventHandler AHandler)
		{
			IWindowsBarButton LItem = new FormMenuContainer();
			LItem.Item.Click += AHandler;
			return LItem;
		}

		public virtual IWindowsBarSeparator CreateSeparator()
		{
			return new FormMenuSeparator();
		}

		private int FReservedItems = 0;
		/// <summary> Number of menu items to lock to the left and not treat as part of the InsertBarItem index space. </summary>
		public int ReservedItems
		{
			get { return FReservedItems; }
			set { FReservedItems = value; }
		}

		private SortedList<BarItemComparer,IWindowsBarItem> FSortedBarItems = new SortedList<BarItemComparer,IWindowsBarItem>();

		public virtual void AddBarItem(IWindowsBarItem AItem, GetPriorityHandler AGetPriority)
		{
			// Add the item to the sorted list
			BarItemComparer LNewBarItemComparer = new BarItemComparer(AItem, AGetPriority);
			FSortedBarItems.Add(LNewBarItemComparer, AItem);
			int LIndex = FSortedBarItems.IndexOfKey(LNewBarItemComparer) + FReservedItems;

			if (LIndex > FItems.Count)
				FItems.Add(((IToolStripItemContainer)AItem).Item);
			else
				FItems.Insert(LIndex, ((IToolStripItemContainer)AItem).Item);
		}

		public virtual void RemoveBarItem(IWindowsBarItem AItem)
		{
			FItems.Remove(((IToolStripItemContainer)AItem).Item);
			FSortedBarItems.RemoveAt(FSortedBarItems.IndexOfValue(AItem));
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
			FItem = new ToolStripMenuItem();
			FItems = FItem.DropDownItems;
		}

		public override void Dispose()
		{
			if (FItem != null)
			{
				FItem.Dispose();
				FItem = null;
			}
		}

		private ToolStripMenuItem FItem;
		public ToolStripItem Item { get { return FItem; } }

		public string Text
		{
			get { return FItem.Text; }
			set { FItem.Text = value; }
		}

		public bool Enabled
		{
			get { return FItem.Enabled; }
			set { FItem.Enabled = value; }
		}

		public System.Drawing.Image Image
		{
			get { return FItem.Image; }
			set { FItem.Image = value; }
		}

		public override bool Visible
		{
			get { return FItem.Visible; }
			set { FItem.Visible = value; }
		}
	}

	public class FormMenuSeparator : IWindowsBarItem, IWindowsBarSeparator, IToolStripItemContainer
	{
		public FormMenuSeparator()
		{
			FItem = new ToolStripSeparator();
		}

		public virtual void Dispose() 
		{
			if (FItem != null)
			{
				FItem.Dispose();
				FItem = null;
			}
		}

		private ToolStripSeparator FItem;
		public ToolStripItem Item { get { return FItem; } }

		public bool Visible
		{
			get { return FItem.Visible; }
			set { FItem.Visible = value; }
		}
	}

	public class FormMainMenuContainer : FormMenuContainerBase, IWindowsBarItem
	{
		public FormMainMenuContainer(MenuStrip AMainMenu) : base()
		{
			FMainMenu = AMainMenu;
			FItems = AMainMenu.Items;
		}

		public override void Dispose() {}

		private MenuStrip FMainMenu;
		public MenuStrip MainMenu { get { return FMainMenu; } }

		public override bool Visible
		{
			get { return FMainMenu.Visible; }
			set { FMainMenu.Visible = value; }
		}
	}

	public class FormButton : IWindowsBarButton, IToolStripItemContainer
	{
		public FormButton(EventHandler AHandler)
		{
			FItem = new ToolStripButton();
			FItem.Click += AHandler;
		}

		public virtual void Dispose()
		{
			if (FItem != null)
			{
				FItem.Dispose();
				FItem = null;
			}
		}

		private ToolStripButton FItem;
		public ToolStripButton ButtonItem { get { return FItem; } }
		public ToolStripItem Item { get { return FItem; } }

		public string Text
		{
			get { return FItem.Text; }
			set { FItem.Text = value; }
		}

		public bool Enabled
		{
			get { return FItem.Enabled; }
			set { FItem.Enabled = value; }
		}

		public System.Drawing.Image Image
		{
			get { return FItem.Image; }
			set { FItem.Image = value; }
		}

		public bool Visible
		{
			get { return FItem.Visible; }
			set { FItem.Visible = value; }
		}
	}

	public class FormExposedContainer : IWindowsBarContainer
	{
		public FormExposedContainer(ToolStrip AToolBar)
		{
			FToolBar = AToolBar;
		}

		public virtual void Dispose() {}

		private ToolStrip FToolBar;
		public ToolStrip ToolBar { get { return FToolBar; } }

		public IWindowsBarContainer CreateContainer()
		{
			Error.Fail("CreateContainer() is not supported for FormExposedContainer");
			return null;
		}

		public IWindowsBarButton CreateMenuItem(System.EventHandler AHandler)
		{
			return new FormButton(AHandler);
		}

		public IWindowsBarSeparator CreateSeparator()
		{
			Error.Fail("CreateSeparator() is not supported for FormExposedContainer");
			return null;
		}

		/// <remarks> The AGetPriority handler is ignored. </remarks>
		public void AddBarItem(IWindowsBarItem AItem, GetPriorityHandler AGetPriority)
		{
			FToolBar.Items.Add(((IToolStripItemContainer)AItem).Item);
		}

		public void RemoveBarItem(IWindowsBarItem AItem)
		{
			FToolBar.Items.Remove(((IToolStripItemContainer)AItem).Item);
		}

		public bool Visible
		{
			get { return FToolBar.Visible; }
			set { FToolBar.Visible = value; }
		}
	}
}