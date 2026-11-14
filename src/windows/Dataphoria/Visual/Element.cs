/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Visual
{
	public interface IElementDesigner : IDisposable, IDisposableNotify
	{
		object Element { get; }
		void ZoomIn();
		Rectangle Bounds { get; set; }
	}

	public class ElementDesignerData : DataObject
	{
		public ElementDesignerData(object element)
		{
			_element = element;
		}

		private object _element;
		public object Element
		{
			get { return _element; }
		}
	}
}
