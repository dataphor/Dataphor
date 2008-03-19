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
		public SingleElementSurface(object AElement, IElementDesigner ADesigner)
		{
			SuspendLayout();

			FElement = AElement;
			FDesigner = ADesigner;
			Controls.Add((Control)ADesigner);

			ResumeLayout(false);
		}

		private object FElement;
		public object Element { get { return FElement; } }

		private IElementDesigner FDesigner;
		public IElementDesigner Designer { get { return FDesigner; } }

		private int FBorderSize = 10;
		public int BorderSize
		{
			get { return FBorderSize; }
			set 
			{
				if (value < 0)
					value = 0;
				if (FBorderSize == value)
				{
					FBorderSize = value;
					PerformLayout();
				}
			}
		}

		protected override void OnLayout(System.Windows.Forms.LayoutEventArgs AArgs)
		{
			Rectangle LBounds = DisplayRectangle;
			LBounds.Inflate(-FBorderSize, -FBorderSize);
			FDesigner.Bounds = LBounds;

			base.OnLayout(AArgs);
		}
	}
}
