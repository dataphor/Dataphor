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
		
		protected override bool IsInputChar(char AChar)
		{
			return 
				(
					!Char.IsLetter(AChar)
						|| (FormatInfo.PositiveSign.IndexOf(AChar) > -1)
						|| (FormatInfo.NegativeSign.IndexOf(AChar) > -1)
						|| (FormatInfo.NumberDecimalSeparator.IndexOf(AChar) > -1)
				)
					&& base.IsInputChar(AChar);
		}

		/// <summary>Filters out all key input except for 0 to 9, - and ..</summary>
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs AArgs)
		{
			if 
			(
				(AArgs.KeyChar >= (char)32) 
					&& (AArgs.KeyChar <= (char)255)
					&&
					(
						(!((AArgs.KeyChar >= (char)48) && (AArgs.KeyChar <= (char)57))) &&
						(!(FormatInfo.PositiveSign.IndexOf(AArgs.KeyChar) > -1)) &&
						(!(FormatInfo.NegativeSign.IndexOf(AArgs.KeyChar) > -1)) &&
						(!(FormatInfo.NumberDecimalSeparator.IndexOf(AArgs.KeyChar) > -1))
					)
			)
			{
				AArgs.Handled = true;
				UnsafeNativeMethods.MessageBeep(-1);
			}
			base.OnKeyPress(AArgs);
		}

		private bool CheckPaste(IDataObject AObject)
		{
			try
			{
				Convert.ToDecimal(AObject.GetData(DataFormats.Text,true));
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

		protected override void OnGotFocus(EventArgs AArgs)
		{
			base.OnGotFocus(AArgs);
			SelectAll();
		}

		protected override void WndProc(ref Message AMessage)
		{
			if (AMessage.Msg == NativeMethods.WM_PASTE)
			{
				if(!WMPaste())
					return; 
			}
			base.WndProc(ref AMessage);
		}
	}
 
}
