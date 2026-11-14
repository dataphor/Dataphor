/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Crownwood.Magic.Menus;

namespace Alphora.Dataphor.Dataphoria.SchemaDesigner
{
	public abstract class ObjectDesigner : TextDesignerBox
	{
		public const string CEndRenameMessageSpecifier = "UM_ENDRENAME";

		static ObjectDesigner()
		{
			FEndRenameMessage = NativeMethods.RegisterWindowMessage(CEndRenameMessageSpecifier);
			if (FEndRenameMessage == 0)
				throw new DataphoriaException(DataphoriaException.Codes.CannotRegisterMessage, CEndRenameMessageSpecifier);
		}

		public ObjectDesigner(ObjectSchema AObject, DesignerControl ADesigner)
		{
			FObject = AObject;
			FObject.OnModified += new SchemaHandler(ObjectModified);
			FObject.OnDeleted += new SchemaHandler(ObjectDeleted);
			FDesignerPage = ADesigner;
			UpdateFromObject();
			RoundRadius = 4f;
		}

		protected override void Dispose(bool ADisposed)
		{
			if (FObject != null)
			{
				FObject.OnDeleted -= new SchemaHandler(ObjectDeleted);
				FObject.OnModified -= new SchemaHandler(ObjectModified);
				FObject = null;
			}
			base.Dispose(ADisposed);
		}

		// DesignerControl

		private DesignerControl FDesignerPage;
		public DesignerControl DesignerControl
		{
			get { return FDesignerPage; }
		}

		// Schema Object

		private ObjectSchema FObject;
		public ObjectSchema Object
		{
			get { return FObject; }
		}

		private bool FUpdatingSchema;

		private void ObjectModified(BaseSchema ASchema)
		{
			DesignerControl.Modified();
			if (!FUpdatingSchema)
				UpdateFromObject();
		}

		private void ObjectDeleted(BaseSchema ASchema)
		{
			DesignerControl.Modified();
			if (!FUpdatingSchema)
				Dispose();
		}

		/// <summary> Turns off notification of change from the schema object. </summary>
		public void BeginUpdate()
		{
			FUpdatingSchema = true;
		}

		public void EndUpdate()
		{
			FUpdatingSchema = false;
		}

		protected virtual void UpdateFromObject()
		{
			Text = GetText();
		}

		// Text

		protected virtual string GetText()
		{
			return FObject.Description;
		}

		// Menu

		protected virtual PopupMenu GetPopupMenu() 
		{
			PopupMenu LPopupMenu = new PopupMenu();
			LPopupMenu.MenuCommands.Add(new MenuCommand(Strings.Get("RenameMenuItemText"), new EventHandler(RenameMenuClicked)));
			LPopupMenu.MenuCommands.Add(new MenuCommand(Strings.Get("DetailsMenuItemText"), new EventHandler(DetailsMenuClicked)));
			LPopupMenu.MenuCommands.Add(new MenuCommand(Strings.Get("DeleteMenuItemText"), new EventHandler(DeleteMenuClicked)));
			return LPopupMenu;
		}

		protected void ShowMenu(Point APoint)
		{
			PopupMenu LMenu = GetPopupMenu();
			if (LMenu != null)
				LMenu.TrackPopup(APoint);
		}

		// Delete

		private void DeleteMenuClicked(object ASender, EventArgs AArgs)
		{
			FObject.Delete();
		}

		// Details

		private void DetailsMenuClicked(object ASender, EventArgs AArgs)
		{
			DesignerControl.ZoomIn(this);
		}

		// Rename

		private RenameTextBox FRenameHover;

		private void RenameMenuClicked(object ASender, EventArgs AArgs)
		{
			if (FRenameHover == null)
			{
				FRenameHover = new RenameTextBox();
				FRenameHover.Text = Object.Description;
				FRenameHover.OnSave += new EventHandler(RenameSave);
				FRenameHover.OnCancel += new EventHandler(RenameCancel);
				FRenameHover.AutoSize = false;
				Controls.Add(FRenameHover);
				FRenameHover.Focus();
			}
		}

		private void RenameSave(object ASender, EventArgs AArgs)
		{
			FObject.Description = FRenameHover.Text;
			EndRename();
		}

		// HACK: This is to work around a null reference that happens if we dispose a
		//  control as it is leaving focus.  We defer the disposal using a WM callback.

		private static int FEndRenameMessage;

		private void EndRename()
		{
			if (FRenameHover != null)
				UnsafeNativeMethods.PostMessage(Handle, FEndRenameMessage, IntPtr.Zero, IntPtr.Zero);
		}

		protected override void WndProc(ref Message AMessage)
		{
			base.WndProc(ref AMessage);
			if (AMessage.Msg == FEndRenameMessage)
			{
				if (FRenameHover != null)
				{
					FRenameHover.Dispose();
					FRenameHover = null;
				}
			}
		}

		private void RenameCancel(object ASender, EventArgs AArgs)
		{
			EndRename();
		}

		// Mouse handling

		protected override void OnMouseMove(MouseEventArgs AArgs)
		{
			if (AArgs.Button == MouseButtons.Left)
				DoDragDrop(new ObjectDesignerData(Object), DragDropEffects.Link | DragDropEffects.Move);
		}

//		protected override void OnQueryContinueDrag(QueryContinueDragEventArgs AArgs)
//		{
//			base.OnQueryContinueDrag(AArgs);
//			if (AArgs.EscapePressed)
//				AArgs.Action = DragAction.Cancel;
//		}

		protected override void OnMouseUp(MouseEventArgs AArgs)
		{
			base.OnMouseUp(AArgs);
			if (AArgs.Button == MouseButtons.Right)
				ShowMenu(PointToScreen(new Point(AArgs.X, AArgs.Y)));
		}

		// Keyboard handling

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if ((AKey & Keys.Modifiers) == Keys.None)
			{
				switch (AKey)
				{
					case (Keys.Apps) :
						Size LClientSize = ClientSize;
						ShowMenu(PointToScreen(new Point(LClientSize.Width / 2, LClientSize.Height / 2)));
						return true;
				}
			}
			return base.ProcessDialogKey(AKey);
		}

		// Layout

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			if (FRenameHover != null)
				FRenameHover.Bounds = GetTextBounds();
		}

		// Details

		public virtual void Details() {}
	}

	internal enum RenameState
	{
		Editing,
		Canceled,
		Saved
	}

	public class ObjectDesignerData : DataObject
	{
		public ObjectDesignerData(ObjectSchema AObject)
		{
			FObject = AObject;
		}

		private ObjectSchema FObject;
		public ObjectSchema Object
		{
			get { return FObject; }
		}
	}

	internal class RenameTextBox : TextBox
	{
		public RenameTextBox()
		{
			BorderStyle = BorderStyle.None;
			TextAlign = HorizontalAlignment.Center;
		}

		private RenameState FState = RenameState.Editing;

		protected override bool ProcessDialogKey(Keys AKey)
		{
			if ((AKey & Keys.Modifiers) == Keys.None)
			{
				switch (AKey)
				{
					case (Keys.Enter) :
						Save();
						return true;
					case (Keys.Escape) :
						Cancel();
						return true;
				}
			}
			return base.ProcessDialogKey(AKey);
		}

		public event EventHandler OnSave;

		public void Save()
		{
			if ((FState == RenameState.Editing) && (OnSave != null))
			{
				FState = RenameState.Saved;
				OnSave(this, EventArgs.Empty);
			}
		}

		public event EventHandler OnCancel;

		public void Cancel()
		{
			if ((FState == RenameState.Editing) && (OnCancel != null))
			{
				FState = RenameState.Canceled;
				OnCancel(this, EventArgs.Empty);
			}
		}

		protected override void OnValidating(CancelEventArgs AArgs)
		{
			base.OnValidating(AArgs);
			Save();
		}
	}
}
