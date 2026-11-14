/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Data-aware TextBox for all numeric types. </summary>
	[ToolboxItem(true)]
	[ToolboxBitmap(typeof(Alphora.Dataphor.DAE.Client.Controls.DBNumericTextBox), "Icons.DBNumericTextBox.bmp")]
	public class DBNumericTextBox : DBTextBox
	{	 	
		/// <summary>Instantiates new instance of DBNumericTextBox.</summary>
		public DBNumericTextBox() : base()
		{ 
			TextAlign = System.Windows.Forms.HorizontalAlignment.Right;	
		}

		protected virtual NumberFormatInfo FormatInfo
		{
			get { return NumberFormatInfo.CurrentInfo; }   
		}
		
		protected override bool IsInputChar(char charValue)
		{
			return 
				(
					!Char.IsLetter(charValue)
						|| (FormatInfo.PositiveSign.IndexOf(charValue) > -1)
						|| (FormatInfo.NegativeSign.IndexOf(charValue) > -1)
						|| (FormatInfo.NumberDecimalSeparator.IndexOf(charValue) > -1)
				)
					&& base.IsInputChar(charValue);
		}

		/// <summary>Filters out all key input except for 0 to 9, - and ..</summary>
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs args)
		{
			if 
			(
				(args.KeyChar >= (char)32) 
					&& (args.KeyChar <= (char)255)
					&&
					(
						(!((args.KeyChar >= (char)48) && (args.KeyChar <= (char)57))) &&
						(!(FormatInfo.PositiveSign.IndexOf(args.KeyChar) > -1)) &&
						(!(FormatInfo.NegativeSign.IndexOf(args.KeyChar) > -1)) &&
						(!(FormatInfo.NumberDecimalSeparator.IndexOf(args.KeyChar) > -1))
					)
			)
			{
				args.Handled = true;
				UnsafeNativeMethods.MessageBeep(-1);
			}
			base.OnKeyPress(args);
		}

		private bool CheckPaste(IDataObject objectValue)
		{
			try
			{
				Convert.ToDecimal(objectValue.GetData(DataFormats.Text,true));
			}
			catch
			{
				UnsafeNativeMethods.MessageBeep(-1);
				return false;
			}
			return true;
		}

		protected virtual bool WMPaste()
		{
			return CheckPaste(Clipboard.GetDataObject());
		}

		protected override void OnGotFocus(EventArgs args)
		{
			base.OnGotFocus(args);
			SelectAll();
		}

		protected override void WndProc(ref Message message)
		{
			if (message.Msg == NativeMethods.WM_PASTE)
			{
				if(!WMPaste())
					return; 
			}
			base.WndProc(ref message);
		}
	}
 
}
