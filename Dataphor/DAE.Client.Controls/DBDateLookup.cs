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

			_textBox = new DBDateTextBox();
			_textBox.Parent = this;
			_textBox.Location = Point.Empty;
			
			Size = _textBox.Size + (DisplayRectangle.Size - Size);

			Button.Image = SpeedButton.ResourceBitmap(typeof(DateLookup), "Alphora.Dataphor.DAE.Client.Controls.Images.Calendar.png");

			ResumeLayout(false);
		}

		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (_popupForm != null)
				{
					_popupForm.Dispose();
					_popupForm = null;
				}
			}
			base.Dispose(disposing);
		}

		private DBDateTextBox _textBox;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public DBDateTextBox TextBox
		{
			get { return _textBox; }
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DateTime DateTime
		{
			get { return _textBox.Date; }
			set { _textBox.Date = value; }
		}

		#region PopupForm

		private CalendarPopup _popupForm;

		private void PopupFormDisposed(object sender, EventArgs args)
		{
			_popupForm.Disposed -= new EventHandler(PopupFormDisposed);
			_popupForm = null;
		}

		private void ShowPopupForm()
		{
			if (_popupForm == null)
			{
				_popupForm = new CalendarPopup(this);
				_popupForm.Disposed += new EventHandler(PopupFormDisposed);
				_popupForm.Accept += new EventHandler(PopupFormAccepted);
			}
			if ((_textBox.DataField != null) && _textBox.DataField.HasValue())
				_popupForm.DateTime = DateTime;
			else
				_popupForm.DateTime = System.DateTime.Today;
			_popupForm.Show();
		}

		protected override void OnLookup(LookupEventArgs args)
		{
			base.OnLookup(args);
			ShowPopupForm();
		}

		private void PopupFormAccepted(object sender, EventArgs e)
		{
			DateTime = _popupForm.DateTime;
		}

		#endregion
	}

	public class CalendarPopup : Form
	{
		public CalendarPopup(Control parent) : base() 
		{
			_parent = parent;

			SuspendLayout();

			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			KeyPreview = true;

			_calendar = new MonthCalendar();
			_calendar.CausesValidation = false;
			_calendar.MaxSelectionCount = 1;
			_calendar.TabStop = true;
			_calendar.MouseUp += new MouseEventHandler(CalendarMouseUp);
			_calendar.Parent = this;

			Width = _calendar.Width + 20;
			Height = _calendar.Height + 2;

			ResumeLayout(false);
		}

		protected override void Dispose(bool disposing)
		{
			if (_calendar != null)
			{
				_calendar.MouseUp -= new MouseEventHandler(CalendarMouseUp);
				_calendar.Dispose();
				_calendar = null;
			}
			base.Dispose(disposing);
		}

		private Control _parent;

		private bool _initialLayout = true;

		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);
			if ((_parent != null) && _initialLayout)
			{	
				_initialLayout = false;
				Point location = _parent.PointToScreen(Point.Empty);
				location.Y += _parent.Height;
				Screen screen = Screen.FromControl(_parent);
				if (!screen.WorkingArea.IsEmpty)
				{
					if (location.X < screen.WorkingArea.Left)
						location.X = screen.WorkingArea.Left;
					if (location.Y + Height > screen.WorkingArea.Bottom)
						location.Y = Top - _parent.Height - Height;
					if (location.X + Width > screen.WorkingArea.Right)
						location.X = screen.WorkingArea.Right - Width;
					if (location.Y < screen.WorkingArea.Top)
						location.Y = screen.WorkingArea.Top;
				}
				Location = location;
			}
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams paramsValue = base.CreateParams;
				paramsValue.Style |= NativeMethods.WS_POPUP | NativeMethods.WS_BORDER;
				return paramsValue;
			}
		}

		private MonthCalendar _calendar;
		protected MonthCalendar Calendar { get { return _calendar; } }

		public DateTime DateTime
		{
			get { return _calendar.SelectionStart; }
			set { _calendar.SetDate(value); }
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Escape : Close(); return true;
				case Keys.Enter : OnAccept(); return true;
				default : return base.ProcessDialogKey(keyData);
			}
		}

		protected override void OnDeactivate(EventArgs args)
		{
			base.OnDeactivate(args);
			Close();
		}

		public event EventHandler Accept;

		protected virtual void OnAccept()
		{
			if (Accept != null)
				Accept(this, EventArgs.Empty);
			Close();
		}

		protected virtual void CalendarMouseUp(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Left)
			{
				MonthCalendar.HitTestInfo info = Calendar.HitTest(args.X, args.Y);
				switch (info.HitArea)
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
