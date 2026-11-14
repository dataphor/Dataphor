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

namespace Alphora.Dataphor.DAE.Client.Controls
{

	/// <summary> Represents a transparent Windows button control. </summary>
	/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
	[ToolboxItem(false)]
	public class ExtendedButton : Button
	{
		/// <summary> Initializes a new instance of the <c>ExtendedButton</c> class. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
		public ExtendedButton() : base()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			CausesValidation = false;
		}

		/// <summary> Recognizes the Enter key as an input key. </summary>
		/// <param name="keyData">A <see cref="System.Windows.Forms.Keys"/> member.</param>
		/// <returns> True if the Enter key is pressed, otherwise returns default <see cref="System.Windows.Forms.Control.IsInputKey"/>. </returns>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData) 
			{
				case Keys.Enter: return true;
				default : return base.IsInputKey(keyData);
			}
		}

		/// <summary> Raises the <c>Click</c> event when the Enter key is pressed. </summary>
		/// <param name="args">A <see cref="System.Windows.Forms.KeyEventArgs"/> </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
		protected override void OnKeyDown(KeyEventArgs args)
		{
			switch (args.KeyData)
			{
				case Keys.Enter :
					PerformClick();
					args.Handled = true;
					break;
			}
			base.OnKeyDown(args);
		}
	}

    /// <summary> Represents a Windows button with a Bitmap. </summary>
    /// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
	public class BitmapButton : ExtendedButton
	{
		/// <summary> Initializes a new instance of the <c>BitmapButton</c> class. </summary>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
		public BitmapButton() : base()
		{
			ImageAlign = ContentAlignment.MiddleCenter;
			Size = new System.Drawing.Size (21, 19);
		}

		/// <summary> Retrievs an embedded resource as a bitmap. </summary>
		/// <param name="type"> An <c>System.Type</c> who's <c>Assembly</c> contains the resource. </param>
		/// <param name="resourceName"> Name of the resource containing a bitmap. </param>
		/// <extdoc href="..\..\..\..\Docs\DAE.Client.Controls\BitmapButton.dxd"/>
		public static Bitmap ResourceBitmap(System.Type type, string resourceName)
		{
			Stream bitmapStream = type.Assembly.GetManifestResourceStream(resourceName);
			try
			{
				Bitmap bitmap = new Bitmap(bitmapStream);
				try
				{
					bitmap.MakeTransparent();
					return bitmap;
				}
				catch
				{
					bitmap.Dispose();
					throw;
				}
			}
			catch
			{
				bitmapStream.Close();
				throw;
			}
		}
	}

	[ToolboxItem(false)]
	public class SpeedButton : BitmapButton
	{
		public SpeedButton() : base()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
		}

		protected override bool ProcessMnemonic(char charCode)
		{
			if (Button.IsMnemonic(charCode, this.Text))	// Process regardless of focusability
			{
				PerformClick();
				return true;
			}
			else
				return base.ProcessMnemonic(charCode);
		}
	}
}
