/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Drawing;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	/// <summary> Default colors for controls. </summary>
	public sealed class ControlColor
	{
		/// <summary> Background color of the control when it has no value. </summary>
		public static Color NoValueBackColor { get { return Color.FromArgb(240, 255, 240); } }
		/// <summary> Background color of the control when it has no value and it's read only. </summary>
		public static Color NoValueReadOnlyBackColor { get { return Color.FromArgb(200, 220, 200); } }
		/// <summary> Background color of an incremental search control when the input value fails to convert to the fields data type. </summary>
		public static Color ConversionFailBackColor { get { return Color.FromArgb(255, 255, 220); } } 
	}
}
