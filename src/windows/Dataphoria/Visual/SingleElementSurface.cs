/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public class SingleElementSurface : Surface
	{
		public SingleElementSurface(object element, IElementDesigner designer)
		{
			SuspendLayout();

			_element = element;
			_designer = designer;
			Controls.Add((Control)designer);

			ResumeLayout(false);
		}

		private object _element;
		public object Element { get { return _element; } }

		private IElementDesigner _designer;
		public IElementDesigner Designer { get { return _designer; } }

		private int _borderSize = 10;
		public int BorderSize
		{
			get { return _borderSize; }
			set 
			{
				if (value < 0)
					value = 0;
				if (_borderSize == value)
				{
					_borderSize = value;
					PerformLayout();
				}
			}
		}

		protected override void OnLayout(System.Windows.Forms.LayoutEventArgs args)
		{
			Rectangle bounds = DisplayRectangle;
			bounds.Inflate(-_borderSize, -_borderSize);
			_designer.Bounds = bounds;

			base.OnLayout(args);
		}
	}
}
