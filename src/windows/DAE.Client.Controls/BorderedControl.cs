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
			_borderStyle = BorderStyle.Fixed3D;
			_updateCount = 0;
		}

		/// <summary> Added support for <c>BorderStyle</c> property. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams paramsValue = base.CreateParams;
				switch (_borderStyle)
				{
					case BorderStyle.Fixed3D :
						paramsValue.ExStyle |= NativeMethods.WS_EX_CLIENTEDGE;
						break;
					case BorderStyle.FixedSingle :
						paramsValue.Style |= NativeMethods.WS_BORDER;
						break;
				}
				return paramsValue;
			}
		}

		private BorderStyle _borderStyle;
		/// <summary> Type of border for the control. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		[Category("Appearance")]
		[DefaultValue(BorderStyle.Fixed3D)]
		public BorderStyle BorderStyle
		{
			get { return _borderStyle; }
			set
			{
				if (_borderStyle != value)
				{
					_borderStyle = value;
					if (IsHandleCreated)
						RecreateHandle();
				}
			}
		}

		private int _updateCount;
		/// <summary> Determines the update state of the control. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		protected bool InUpdate
		{
			get { return _updateCount > 0; }
		}

		/// <summary> Suspends painting of the control until <c>EndUpdate</c> is called. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		public void BeginUpdate()
		{
			if (++_updateCount == 1)
				SetUpdateState(true);
		}

		/// <summary> Resumes painting suspended by BeginUpdate. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BorderedControl.dxd"/>
		public void EndUpdate()
		{
			if (--_updateCount <= 0)
			{
				_updateCount = 0;
				SetUpdateState(false);
			}
		}

		private void SetUpdateState(bool updating)
		{
			UnsafeNativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, !updating, IntPtr.Zero);
			if (!updating)
			{
				Invalidate();
				Invalidate(true);
			}
		}
    }
}
