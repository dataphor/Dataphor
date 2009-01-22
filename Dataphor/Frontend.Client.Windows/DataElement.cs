/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;
using System.Windows.Forms;
using System.Drawing.Design;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[DesignerImage("Image('Frontend', 'Nodes.Text')")]
	[DesignerCategory("Data Controls")]
	public class Text : AlignedElement, IText
	{
		// Height

		private int FHeight = 1;
		[DefaultValue(1)]
		public int Height
		{
			get { return FHeight; }
			set 
			{ 
				if (FHeight != value)
				{
					FHeight = value; 
					UpdateLayout();
				}
			}
		}

		// TextControl

		protected DAE.Client.Controls.DBText TextControl
		{
			get { return (DAE.Client.Controls.DBText)Control; }
		}

		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBText();
		}

		// AlignedElement

		protected override void InternalUpdateTextAlignment()
		{
			switch (TextAlignment)
			{
				case HorizontalAlignment.Center : 
					TextControl.TextAlign = ContentAlignment.MiddleCenter; 
					break;
				case HorizontalAlignment.Right :
					TextControl.TextAlign = ContentAlignment.MiddleRight;
					break;
				default :
					TextControl.TextAlign = ContentAlignment.MiddleLeft;
					break;
			}
		}

		protected override int GetControlNaturalHeight()
		{
			return FHeight * TextControl.Font.Height;
		}

	}

	[DesignerImage("Image('Frontend', 'Nodes.CheckBox')")]
	[DesignerCategory("Data Controls")]
	public class CheckBox : ColumnElement, ICheckBox
	{
		public const int CDefaultWidth = 10;
		public const int CMinCheckBoxWidth = 20;
		
		// CheckBoxControl

		protected DAE.Client.Controls.DBCheckBox CheckBoxControl
		{
			get { return (DAE.Client.Controls.DBCheckBox)base.Control; }
		}

		// Width

		// TODO: Remove Width when common properties are better handled
		// This is only here because it is a common property
		private int FWidth = 0;
		[DefaultValue(0)]
		[Description("Width has no affect for the CheckBox control.")]
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}
		
		// AutoUpdate
		
		private bool FAutoUpdate = true;
		[DefaultValue(true)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				FAutoUpdate = value;
				if (Active)
					InternalUpdateAutoUpdate();
			}
		}

        // TrueFirst

        private bool FTrueFirst = true;
        [DefaultValue(true)]
        [Description("Determines the CheckState transition sequence for CheckBoxes with three-states.")]
        public bool TrueFirst
        {
            get { return FTrueFirst; }
            set
            {
                    FTrueFirst = value;
                    if (Active)
                        InternalUpdateTrueFirst();
            }
        }

        private void InternalUpdateTrueFirst()
        {
            CheckBoxControl.TrueFirst = FTrueFirst;
        }

        private void InternalUpdateAutoUpdate()
		{
			CheckBoxControl.AutoUpdate = FAutoUpdate;
		}
		
		// AutoUpdateInterval
		
		private int FAutoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return FAutoUpdateInterval; }
			set
			{
				FAutoUpdateInterval = value;
				if (Active)
					InternalUpdateAutoUpdateInterval();
			}
		}
		
		private void InternalUpdateAutoUpdateInterval()
		{
			CheckBoxControl.AutoUpdateInterval = FAutoUpdateInterval;
		}
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBCheckBox();
		}

		private static int FCheckBoxHeight = 0;

		private static int GetCheckBoxHeight(WinForms.Control AControl)
		{
			if (FCheckBoxHeight == 0)
				FCheckBoxHeight = Element.GetPixelHeight(AControl);
			return FCheckBoxHeight;
		}

		protected override void InitializeControl()
		{
			Control.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
			CheckBoxControl.AutoEllipsis = true;
			InternalUpdateAutoUpdate();
			InternalUpdateAutoUpdateInterval();
            InternalUpdateTrueFirst();
			base.InitializeControl();
		}

		// Element

		protected override void InternalLayout(Rectangle ABounds)
		{
			int LOffset = ABounds.Height - InternalNaturalSize.Height;
			if (LOffset > 0)
			{
				ABounds.Y += LOffset;
				ABounds.Height -= LOffset;
			}
			base.InternalLayout(ABounds);
		}
		
		protected override Size InternalNaturalSize
		{
			get 
			{ 
				return Control.GetPreferredSize(Size.Empty);
			}
		}
	}

	// TODO: Do something to allow columns (don't necessarily make that a property though because a "choice" is an abstract concept)
	[DesignerImage("Image('Frontend', 'Nodes.Choice')")]
	[DesignerCategory("Data Controls")]
	public class Choice : ColumnElement, IChoice
	{
		public const int CDefaultWidth = 10;
		public const int CMinWidth = 16;

		// RadioControl

		protected DAE.Client.Controls.DBRadioButtonGroup RadioControl
		{
			get { return (DAE.Client.Controls.DBRadioButtonGroup)Control; }
		}

		// Width

		// TODO: Width in Choice because it is a common derivation prop.  Remove w/ better derivation system.
		private int FWidth = 0;
		[DefaultValue(0)]
		[Description("Width has no affect for the Choice control.")]
		public int Width
		{
			get { return FWidth; }
			set { FWidth = value; }
		}

		// Columns

		private int FColumns = 1;
		[DefaultValue(1)]
		[Description("The number of columns to display radio buttons in.")]
		public int Columns
		{
			get { return FColumns; }
			set
			{
				if (value != FColumns)
				{
					FColumns = Math.Max(1, value);
					if (Active)
					{
						InternalUpdateColumns();
						UpdateLayout();
					}
				}
			}
		}

		protected void InternalUpdateColumns()
		{
			RadioControl.Columns = FColumns;
		}

		// Items

		private string FItems = String.Empty;
		[DefaultValue("")]
		[Description("Comma or semicolon separated list of available name-value pairs to be listed as choices. (First=1, Second=2)")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string Items
		{
			get { return FItems; }
			set
			{
				if (FItems != value)
				{
					FItems = value;
					if (Active)
					{
						InternalUpdateItems();
						UpdateLayout();
					}
				}
			}
		}

		protected void InternalUpdateItems()
		{
			DAE.Client.Controls.DBRadioButtonGroup LControl = RadioControl;
			LControl.Items = new string[0];
			LControl.Values = new string[0];
			if (FItems != String.Empty)
			{
				string[] LNamesWithValues = FItems.Split(new char[] {';', ','});
				string[] LNameThenValue;
				string[] LNames = new string[LNamesWithValues.Length];
				string[] LValues = new string[LNamesWithValues.Length];
				for (int i = 0; i < LNamesWithValues.Length; i++)
				{
					LNameThenValue = LNamesWithValues[i].Split('=');
					if (LNameThenValue.Length != 2)
						throw new ClientException(ClientException.Codes.InvalidChoiceItems, Name);

					LNames[i] = LNameThenValue[0].Trim();
					LValues[i] = LNameThenValue[1].Trim();		
				}
				LControl.Items = LNames;
				LControl.Values = LValues;
			};
		}

		// AutoUpdate
		
		private bool FAutoUpdate = false;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return FAutoUpdate; }
			set
			{
				FAutoUpdate = value;
				if (Active)
					InternalUpdateAutoUpdate();
			}
		}
		
		private void InternalUpdateAutoUpdate()
		{
			RadioControl.AutoUpdate = FAutoUpdate;
		}
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBRadioButtonGroup();
		}

		protected override void InitializeControl()
		{
			Control.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
			Control.SuspendLayout();
			try
			{
				InternalUpdateItems();
			}
			finally
			{
				Control.ResumeLayout(false);
			}
			InternalUpdateAutoUpdate();
			InternalUpdateColumns();
			base.InitializeControl();
		}

		// Element

        protected override Size InternalMinSize
        {
            get { return RadioControl.MinimumSize; }
        }

		protected override Size InternalNaturalSize
		{
			get { return RadioControl.NaturalSize(); }
		}
	}

	[Description("Puts an image on the form from an image in a source.")]
	[DesignerImage("Image('Frontend', 'Nodes.Image')")]
	[DesignerCategory("Data Controls")]
	public class Image : TitledElement, IImage
	{
		public const int CMinImageWidth = 20;
		public const int CDefaultImageHeight = 90;
		public const int CDefaultImageWidth = 90;

		// Width - unused common property

		private int FWidth = 0;
		[DefaultValue(0)]
		[Browsable(false)]
		public int Width { get { return FWidth; } set { FWidth = value; } }

		// ImageWidth

		private int FImageWidth = -1;
		[DefaultValue(-1)]
		[Description("The width, in pixels, that the image will be show as.  If set to -1 then the image's width will be used.")]
		public int ImageWidth
		{
			get { return FImageWidth; }
			set
			{
				if (FImageWidth != value)
				{
					FImageWidth = value;
					UpdateLayout();
				}
			}
		}

		// ImageHeight

		private int FImageHeight = -1;
		[DefaultValue(-1)]
		[Description("The height, in pixels, that the image will be show as.  If set to -1 then the image's height will be used.")]
		public int ImageHeight
		{
			get { return FImageHeight; }
			set
			{
				if (FImageHeight != value)
				{
					FImageHeight = value;
					UpdateLayout();
				}
			}
		}

		// ImageControl

		protected DAE.Client.Controls.DBImageAspect ImageControl
		{
			get { return (DAE.Client.Controls.DBImageAspect)base.Control; }
		}

		// StretchStyle

		private StretchStyles FStretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(DAE.Client.Controls.StretchStyles.StretchRatio)]
		[Description("The method to use to scale the image.  Ratio will preserve the aspect ratio of the image.  Fill will not.  NoStretch will crop the image appropriatly.")]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set
			{
				if (FStretchStyle != value)
				{
					FStretchStyle = value;
					if (Active)
						InternalUpdateStretchStyle();
				}
			}
		}

		protected virtual void InternalUpdateStretchStyle()
		{
			ImageControl.StretchStyle = (DAE.Client.Controls.StretchStyles)FStretchStyle;
			UpdateLayout();
		}


		// Center

		private bool FCenter = true;
		[DefaultValue(true)]
		[Description("When set to true the image will be centered in it's own area..")]
		public bool Center
		{
			get { return FCenter; }
			set
			{
				if (FCenter != value)
				{
					FCenter = value;
					if (Active)
						InternalUpdateCenter();
				}
			}
		}

		protected virtual void InternalUpdateCenter()
		{
			ImageControl.Center = FCenter;
		}

		// DataColumnElement

		protected override Control CreateControl()
		{
			return new DAE.Client.Controls.DBImageAspect();
		}

		protected override void InitializeControl()
		{
			ImageControl.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
			ImageControl.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			ImageControl.Height = CDefaultImageHeight;
			ImageControl.Width = CDefaultImageWidth;
			InternalUpdateStretchStyle();
			InternalUpdateCenter();
			base.InitializeControl();
		}
		
		// TitledElement

		protected override int GetControlNaturalHeight()
		{
			if ((FStretchStyle == StretchStyles.NoStretch) && (ImageControl.Image != null))
				return ImageControl.Image.Height;
			if (FImageHeight > 0)
				return FImageHeight;
			else
				return CDefaultImageHeight;
		}

		protected override int GetControlNaturalWidth()
		{
			if ((FStretchStyle == StretchStyles.NoStretch) && (ImageControl.Image != null))
				return ImageControl.Image.Width;
			if (FImageWidth > 0)
				return FImageWidth;
			else
				return CDefaultImageWidth;
		}
	}
}
