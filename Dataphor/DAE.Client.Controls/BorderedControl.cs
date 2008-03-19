/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Base class for controls with a border. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
	[ToolboxItem(false)]
	public abstract class BorderedControl : Control
	{
		/// <summary> Initializes a new instance of the <c>BorderedControl</c> class. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		public BorderedControl() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
			FBorderStyle = BorderStyle.Fixed3D;
			FUpdateCount = 0;
		}

		/// <summary> Added support for <c>BorderStyle</c> property. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams LParams = base.CreateParams;
				switch (FBorderStyle)
				{
					case BorderStyle.Fixed3D :
						LParams.ExStyle |= NativeMethods.WS_EX_CLIENTEDGE;
						break;
					case BorderStyle.FixedSingle :
						LParams.Style |= NativeMethods.WS_BORDER;
						break;
				}
				return LParams;
			}
		}

		private BorderStyle FBorderStyle;
		/// <summary> Type of border for the control. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		[Category("Appearance")]
		[DefaultValue(BorderStyle.Fixed3D)]
		public BorderStyle BorderStyle
		{
			get { return FBorderStyle; }
			set
			{
				if (FBorderStyle != value)
				{
					FBorderStyle = value;
					if (IsHandleCreated)
						RecreateHandle();
				}
			}
		}

		private int FUpdateCount;
		/// <summary> Determines the update state of the control. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		protected bool InUpdate
		{
			get { return FUpdateCount > 0; }
		}

		/// <summary> Suspends painting of the control until <c>EndUpdate</c> is called. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		public void BeginUpdate()
		{
			if (++FUpdateCount == 1)
				SetUpdateState(true);
		}

		/// <summary> Resumes painting suspended by BeginUpdate. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		public void EndUpdate()
		{
			if (--FUpdateCount <= 0)
			{
				FUpdateCount = 0;
				SetUpdateState(false);
			}
		}

		private void SetUpdateState(bool AUpdating)
		{
			UnsafeNativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, !AUpdating, IntPtr.Zero);
			if (!AUpdating)
			{
				Invalidate();
				Invalidate(true);
			}
		}
    }
}
