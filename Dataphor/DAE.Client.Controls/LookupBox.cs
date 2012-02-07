/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.LookupBox),"Icons.DBLookupTextBox.bmp")]
	public class LookupBox : LookupBase
	{
		public LookupBox() :	base()
		{
			SuspendLayout();

			SetStyle(ControlStyles.Selectable, true);   // Must be selectable (will focus child if focused)
			SetStyle(ControlStyles.CacheText, true);

			TabStop = false;

			Size = MinButtonSize() + (DisplayRectangle.Size - Size);

			ResumeLayout(false);
		}

		#region Focus

		public override bool FocusControl()
		{
			for (int i = 1; i < Controls.Count; i++)	// Skip the 0th control (the button)
			{
				Controls[i].Focus();
				if (Controls[i].ContainsFocus)
					return true;
			}
			return false;
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			FocusControl();
		}

		#endregion

		#region Layout

		/// <remarks> This control "snaps" to fit it's child control(s). </remarks>
		protected override void OnLayout(LayoutEventArgs args)
		{
			base.OnLayout(args);

			// Measure the extent of this control's children
			Size extent = new Size(0, MinButtonSize().Height);
			foreach (Control control in Controls)
			{
				if ((control != Button) && (!this.Visible || control.Visible))	// Note: Cannot accurately check for visibility in this context because visibility is recursive.  The control should be visible, however, if it's parent (this) is visible.
				{
					if (control.Right > extent.Width)
						extent.Width = control.Right;
					if (control.Bottom > extent.Height)
						extent.Height = control.Bottom;
				}
			}
			
			// Stretch to include the button
			extent.Width += Button.Width;

			// Size this control to encompass the child
			ClientSize = extent;

			// Layout the button
			Button.Bounds =
				new Rectangle
				(
					extent.Width - Button.Width,
					0,
					Button.Width,
					extent.Height
				);
		}

		public override Rectangle DisplayRectangle
		{
			get
			{
				Rectangle bounds = base.DisplayRectangle;
				bounds.Width -= Button.Width;
				return bounds;
			}
		}

		#endregion

		// Hide the text property
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string Text
		{
			get { return String.Empty; }
			set { }
		}
	}
}

