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
		
		public const bool CDefaultNilIfBlank = true;
		private bool FNilIfBlank = CDefaultNilIfBlank;
		[DefaultValue(CDefaultNilIfBlank)]
		public bool NilIfBlank 
		{ 
			get { return FNilIfBlank; }
			set 
			{ 
				if (FNilIfBlank != value)
				{
					FNilIfBlank = value;
					InternalUpdateTextEditControl();
				}
			}
		}

		// DocumentType
		
		public const string CDefaultDocumentType = "Default";
		private string FDocumentType = CDefaultDocumentType;
		[DefaultValue(CDefaultDocumentType)]
		public string DocumentType
		{
			get { return FDocumentType; }
			set
			{
				if (FDocumentType != value)
				{
					FDocumentType = value != null ? value : "";
					if (Active)
						InternalUpdateTextEditControl();
				}
			}
		}

		private void InternalUpdateTextEditControl()
		{
			DBTextEdit LControl = TextEditControl;
			if (LControl != null)
			{
				LControl.NilIfBlank = FNilIfBlank;
				LControl.DocumentType = FDocumentType;
			}
		}

		// Height

		public const int CDefaultHeight = 6;
		private int FHeight = CDefaultHeight;
		[DefaultValue(CDefaultHeight)]
		public int Height
		{
			get { return FHeight; }
			set
			{
				if (value != FHeight)
				{
					FHeight = value;
					UpdateLayout();
				}
			}
		}
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DBTextEdit();
		}

		private int FRowHeight;

		protected override void InitializeControl()
		{
			FRowHeight = TextEditControl.ActiveTextAreaControl.TextArea.TextView.FontHeight + 1;
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
				+ (FRowHeight * FHeight)
				+ WinForms.SystemInformation.HorizontalScrollBarHeight;
		}

		protected override int GetControlMinHeight()
		{
			return (WinForms.SystemInformation.Border3DSize.Height * 2) 
				+ FRowHeight
				+ WinForms.SystemInformation.HorizontalScrollBarHeight;
		}

		protected override int GetControlMaxHeight()
		{
			return WinForms.Screen.FromControl(Control).WorkingArea.Height;
		}
	}
}
