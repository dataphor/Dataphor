/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Date lookup box with embedded DBDateTextBox. </summary>
	[ToolboxItem(true)]
	public class DateLookup : LookupBox
	{
		public DateLookup() : base()
		{
			SuspendLayout();

			FTextBox = new DBDateTextBox();
			FTextBox.Parent = this;
			FTextBox.Location = Point.Empty;
			
			Size = FTextBox.Size + (DisplayRectangle.Size - Size);

			Button.Image = SpeedButton.ResourceBitmap(typeof(DateLookup), "Alphora.Dataphor.DAE.Client.Controls.Images.Calendar.png");

			ResumeLayout(false);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (!IsDisposed)
			{
				if (FPopupForm != null)
				{
					FPopupForm.Dispose();
					FPopupForm = null;
				}
			}
			base.Dispose(ADisposing);
		}

		private DBDateTextBox FTextBox;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public DBDateTextBox TextBox
		{
			get { return FTextBox; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DateTime DateTime
		{
			get { return FTextBox.Date; }
			set { FTextBox.Date = value; }
		}

		#region PopupForm

		private CalendarPopup FPopupForm;

		private void PopupFormDisposed(object ASender, EventArgs AArgs)
		{
			FPopupForm.Disposed -= new EventHandler(PopupFormDisposed);
			FPopupForm = null;
		}

		private void ShowPopupForm()
		{
			if (FPopupForm == null)
			{
				FPopupForm = new CalendarPopup(this);
				FPopupForm.Disposed += new EventHandler(PopupFormDisposed);
				FPopupForm.Accept += new EventHandler(PopupFormAccepted);
			}
			if ((FTextBox.DataField != null) && FTextBox.DataField.HasValue())
				FPopupForm.DateTime = DateTime;
			else
				FPopupForm.DateTime = System.DateTime.Today;
			FPopupForm.Show();
		}

		protected override void OnLookup(LookupEventArgs AArgs)
		{
			base.OnLookup(AArgs);
			ShowPopupForm();
		}

		private void PopupFormAccepted(object sender, EventArgs e)
		{
			DateTime = FPopupForm.DateTime;
		}

		#endregion
	}

	public class CalendarPopup : Form
	{
		public CalendarPopup(Control AParent) : base() 
		{
			FParent = AParent;

			SuspendLayout();

			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			KeyPreview = true;

			FCalendar = new MonthCalendar();
			FCalendar.CausesValidation = false;
			FCalendar.MaxSelectionCount = 1;
			FCalendar.TabStop = true;
			FCalendar.MouseUp += new MouseEventHandler(CalendarMouseUp);
			FCalendar.Parent = this;

			Width = FCalendar.Width + 20;
			Height = FCalendar.Height + 2;

			ResumeLayout(false);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FCalendar != null)
			{
				FCalendar.MouseUp -= new MouseEventHandler(CalendarMouseUp);
				FCalendar.Dispose();
				FCalendar = null;
			}
			base.Dispose(ADisposing);
		}

		private Control FParent;

		private bool FInitialLayout = true;

		protected override void OnLayout(LayoutEventArgs AArgs)
		{
			base.OnLayout(AArgs);
			if ((FParent != null) && FInitialLayout)
			{	
				FInitialLayout = false;
				Point LLocation = FParent.PointToScreen(Point.Empty);
				LLocation.Y += FParent.Height;
				Screen LScreen = Screen.FromControl(FParent);
				if (!LScreen.WorkingArea.IsEmpty)
				{
					if (LLocation.X < LScreen.WorkingArea.Left)
						LLocation.X = LScreen.WorkingArea.Left;
					if (LLocation.Y + Height > LScreen.WorkingArea.Bottom)
						LLocation.Y = Top - FParent.Height - Height;
					if (LLocation.X + Width > LScreen.WorkingArea.Right)
						LLocation.X = LScreen.WorkingArea.Right - Width;
					if (LLocation.Y < LScreen.WorkingArea.Top)
						LLocation.Y = LScreen.WorkingArea.Top;
				}
				Location = LLocation;
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams LParams = base.CreateParams;
				LParams.Style |= NativeMethods.WS_POPUP | NativeMethods.WS_BORDER;
				return LParams;
			}
		}

		private MonthCalendar FCalendar;
		protected MonthCalendar Calendar { get { return FCalendar; } }

		public DateTime DateTime
		{
			get { return FCalendar.SelectionEnd; }
			set { FCalendar.SetDate(value); }
		}

		protected override bool ProcessDialogKey(Keys AKeyData)
		{
			switch (AKeyData)
			{
				case Keys.Escape : Close(); return true;
				case Keys.Enter : OnAccept(); return true;
				default : return base.ProcessDialogKey(AKeyData);
			}
		}

		protected override void OnDeactivate(EventArgs AArgs)
		{
			base.OnDeactivate(AArgs);
			Close();
		}

		public event EventHandler Accept;

		protected virtual void OnAccept()
		{
			if (Accept != null)
				Accept(this, EventArgs.Empty);
			Close();
		}

		protected virtual void CalendarMouseUp(object ASender, MouseEventArgs AArgs)
		{
			if (AArgs.Button == MouseButtons.Left)
			{
				MonthCalendar.HitTestInfo LInfo = Calendar.HitTest(AArgs.X, AArgs.Y);
				switch (LInfo.HitArea)
				{
					case MonthCalendar.HitArea.Date:
					case MonthCalendar.HitArea.TodayLink:
						OnAccept();
						break;
				}
			}
		}
	}
	

}
