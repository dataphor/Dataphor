/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
using System.Text;
using WinForms = System.Windows.Forms;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.TextEditor')")]
	[DesignerCategory("Data Controls")]
	class TextEditor : TitledElement, ITextEditor
	{
		protected DBTextEdit TextEditControl
		{
			get { return (DBTextEdit)Control; }
		}

		// NilIfBlank
		
		public const bool DefaultNilIfBlank = true;
		private bool _nilIfBlank = DefaultNilIfBlank;
		[DefaultValue(DefaultNilIfBlank)]
		public bool NilIfBlank 
		{ 
			get { return _nilIfBlank; }
			set 
			{ 
				if (_nilIfBlank != value)
				{
					_nilIfBlank = value;
					if (Active)
						InternalUpdateTextEditControl();
				}
			}
		}

		// DocumentType
		
		public const string DefaultDocumentType = "Default";
		private string _documentType = DefaultDocumentType;
		[DefaultValue(DefaultDocumentType)]
		public string DocumentType
		{
			get { return _documentType; }
			set
			{
				if (_documentType != value)
				{
					_documentType = value != null ? value : "";
					if (Active)
						InternalUpdateTextEditControl();
				}
			}
		}

		private void InternalUpdateTextEditControl()
		{
			DBTextEdit control = TextEditControl;
			if (control != null)
			{
				control.NilIfBlank = _nilIfBlank;
				control.DocumentType = _documentType;
			}
		}

		// Height

		public const int DefaultHeight = 6;
		private int _height = DefaultHeight;
		[DefaultValue(DefaultHeight)]
		public int Height
		{
			get { return _height; }
			set
			{
				if (value != _height)
				{
					_height = value;
					UpdateLayout();
				}
			}
		}
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DBTextEdit();
		}

		private int _rowHeight;

		protected override void InitializeControl()
		{
			_rowHeight = TextEditControl.ActiveTextAreaControl.TextArea.TextView.FontHeight + 1;
			InternalUpdateTextEditControl();
			base.InitializeControl();
		}

		// TitledElement

		protected override bool EnforceMaxHeight()
		{
			return false;
		}

		protected override int GetControlNaturalHeight()
		{
			return (WinForms.SystemInformation.Border3DSize.Height * 2) 
				+ (_rowHeight * _height)
				+ WinForms.SystemInformation.HorizontalScrollBarHeight;
		}

		protected override int GetControlMinHeight()
		{
			return (WinForms.SystemInformation.Border3DSize.Height * 2) 
				+ _rowHeight
				+ WinForms.SystemInformation.HorizontalScrollBarHeight;
		}

		protected override int GetControlMaxHeight()
		{
			return WinForms.Screen.FromControl(Control).WorkingArea.Height;
		}
	}
}
