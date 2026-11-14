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
using System.IO;

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

		private int _height = 1;
		[DefaultValue(1)]
		public int Height
		{
			get { return _height; }
			set 
			{ 
				if (_height != value)
				{
					_height = value; 
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
			return _height * TextControl.Font.Height;
		}

	}

	[DesignerImage("Image('Frontend', 'Nodes.CheckBox')")]
	[DesignerCategory("Data Controls")]
	public class CheckBox : ColumnElement, ICheckBox
	{
		public const int DefaultWidth = 10;
		public const int MinCheckBoxWidth = 20;
		
		// CheckBoxControl

		protected DAE.Client.Controls.DBCheckBox CheckBoxControl
		{
			get { return (DAE.Client.Controls.DBCheckBox)base.Control; }
		}

		// Width

		// TODO: Remove Width when common properties are better handled
		// This is only here because it is a common property
		private int _width = 0;
		[DefaultValue(0)]
		[Description("Width has no affect for the CheckBox control.")]
		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}
		
		// AutoUpdate
		
		private bool _autoUpdate = true;
		[DefaultValue(true)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				_autoUpdate = value;
				if (Active)
					InternalUpdateAutoUpdate();
			}
		}

		// TrueFirst

		private bool _trueFirst = true;
		[DefaultValue(true)]
		[Description("Determines the CheckState transition sequence for CheckBoxes with three-states.")]
		public bool TrueFirst
		{
			get { return _trueFirst; }
			set
			{
					_trueFirst = value;
					if (Active)
						InternalUpdateTrueFirst();
			}
		}

		private void InternalUpdateTrueFirst()
		{
			CheckBoxControl.TrueFirst = _trueFirst;
		}

		private void InternalUpdateAutoUpdate()
		{
			CheckBoxControl.AutoUpdate = _autoUpdate;
		}
		
		// AutoUpdateInterval
		
		private int _autoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				_autoUpdateInterval = value;
				if (Active)
					InternalUpdateAutoUpdateInterval();
			}
		}
		
		private void InternalUpdateAutoUpdateInterval()
		{
			CheckBoxControl.AutoUpdateInterval = _autoUpdateInterval;
		}
		
		// DataColumnElement

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.DBCheckBox();
		}

		private static int _checkBoxHeight = 0;

		private static int GetCheckBoxHeight(WinForms.Control control)
		{
			if (_checkBoxHeight == 0)
				_checkBoxHeight = Element.GetPixelHeight(control);
			return _checkBoxHeight;
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

		protected override void InternalLayout(Rectangle bounds)
		{
			int offset = bounds.Height - InternalNaturalSize.Height;
			if (offset > 0)
			{
				bounds.Y += offset;
				bounds.Height -= offset;
			}
			base.InternalLayout(bounds);
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
		public const int DefaultWidth = 10;
		public const int MinWidth = 16;

		// RadioControl

		protected DAE.Client.Controls.DBRadioButtonGroup RadioControl
		{
			get { return (DAE.Client.Controls.DBRadioButtonGroup)Control; }
		}

		// Width

		// TODO: Width in Choice because it is a common derivation prop.  Remove w/ better derivation system.
		private int _width = 0;
		[DefaultValue(0)]
		[Description("Width has no affect for the Choice control.")]
		public int Width
		{
			get { return _width; }
			set { _width = value; }
		}

		// Columns

		private int _columns = 1;
		[DefaultValue(1)]
		[Description("The number of columns to display radio buttons in.")]
		public int Columns
		{
			get { return _columns; }
			set
			{
				if (value != _columns)
				{
					_columns = Math.Max(1, value);
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
			RadioControl.Columns = _columns;
		}

		// Items

		private string _items = String.Empty;
		[DefaultValue("")]
		[Description("Comma or semicolon separated list of available name-value pairs to be listed as choices. (First=1, Second=2)")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string Items
		{
			get { return _items; }
			set
			{
				if (_items != value)
				{
					_items = value;
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
			DAE.Client.Controls.DBRadioButtonGroup control = RadioControl;
			control.Items = new string[0];
			control.Values = new string[0];
			if (_items != String.Empty)
			{
				string[] namesWithValues = _items.Split(new char[] {';', ','});
				string[] nameThenValue;
				string[] names = new string[namesWithValues.Length];
				string[] values = new string[namesWithValues.Length];
				for (int i = 0; i < namesWithValues.Length; i++)
				{
					nameThenValue = namesWithValues[i].Split('=');
					if (nameThenValue.Length != 2)
						throw new ClientException(ClientException.Codes.InvalidChoiceItems, Name);

					names[i] = nameThenValue[0].Trim();
					values[i] = nameThenValue[1].Trim();		
				}
				control.Items = names;
				control.Values = values;
			};
		}

		// AutoUpdate
		
		private bool _autoUpdate;
		[DefaultValue(false)]
		[Description("Determines whether or not the control will automatically update the underlying data value.")]
		public bool AutoUpdate
		{
			get { return _autoUpdate; }
			set
			{
				_autoUpdate = value;
				if (Active)
					InternalUpdateAutoUpdate();
			}
		}
		
		private void InternalUpdateAutoUpdate()
		{
			RadioControl.AutoUpdate = _autoUpdate;
		}
		
		// AutoUpdateInterval
		
		private int _autoUpdateInterval = 200;
		[DefaultValue(200)]
		[Description("Specifies the amount of time to wait before updating the underlying data value.")]
		public int AutoUpdateInterval
		{
			get { return _autoUpdateInterval; }
			set
			{
				_autoUpdateInterval = value;
				if (Active)
					InternalUpdateAutoUpdateInterval();
			}
		}
		
		private void InternalUpdateAutoUpdateInterval()
		{
			RadioControl.AutoUpdateInterval = _autoUpdateInterval;
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
		public const int MinImageWidth = 20;
		public const int DefaultImageHeight = 90;
		public const int DefaultImageWidth = 90;

		// Width - unused common property

		private int _width = 0;
		[DefaultValue(0)]
		[Browsable(false)]
		public int Width { get { return _width; } set { _width = value; } }

		// ImageWidth

		private int _imageWidth = -1;
		[DefaultValue(-1)]
		[Description("The width, in pixels, that the image will be show as.  If set to -1 then the image's width will be used.")]
		public int ImageWidth
		{
			get { return _imageWidth; }
			set
			{
				if (_imageWidth != value)
				{
					_imageWidth = value;
					UpdateLayout();
				}
			}
		}

		// ImageHeight

		private int _imageHeight = -1;
		[DefaultValue(-1)]
		[Description("The height, in pixels, that the image will be show as.  If set to -1 then the image's height will be used.")]
		public int ImageHeight
		{
			get { return _imageHeight; }
			set
			{
				if (_imageHeight != value)
				{
					_imageHeight = value;
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

		private StretchStyles _stretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(DAE.Client.Controls.StretchStyles.StretchRatio)]
		[Description("The method to use to scale the image.  Ratio will preserve the aspect ratio of the image.  Fill will not.  NoStretch will crop the image appropriatly.")]
		public StretchStyles StretchStyle
		{
			get { return _stretchStyle; }
			set
			{
				if (_stretchStyle != value)
				{
					_stretchStyle = value;
					if (Active)
						InternalUpdateStretchStyle();
				}
			}
		}

		protected virtual void InternalUpdateStretchStyle()
		{
			ImageControl.StretchStyle = (DAE.Client.Controls.StretchStyles)_stretchStyle;
			UpdateLayout();
		}


		// Center

		private bool _center = true;
		[DefaultValue(true)]
		[Description("When set to true the image will be centered in it's own area.")]
		public bool Center
		{
			get { return _center; }
			set
			{
				if (_center != value)
				{
					_center = value;
					if (Active)
						InternalUpdateCenter();
				}
			}
		}

		protected virtual void InternalUpdateCenter()
		{
			ImageControl.Center = _center;
		}

		// DataColumnElement

		protected override Control CreateControl()
		{
			return new DAE.Client.Controls.DBImageAspect();
		}

		private IImageSource _imageSource;
		public IImageSource ImageSource
		{
			get 
			{ 
				if (_imageSource == null)
					_imageSource = new ImageCaptureForm();
				return _imageSource; 
			}
			set { _imageSource = value; }
		}

		protected void LoadImage()
		{          
			 if (!ImageSource.Loading)
			 {
				 try
				 {						 
					 ImageSource.LoadImage();
					 if (ImageSource.Stream != null)
					 {
						 using (DAE.Runtime.Data.Scalar newValue = new DAE.Runtime.Data.Scalar(ImageControl.Source.DataSet.Process.ValueManager, ImageControl.Source.DataSet.Process.DataTypes.SystemGraphic))
						 {
							 using (Stream stream = newValue.OpenStream())
							 {
								 using (ImageSource.Stream)
								 {
									 ImageSource.Stream.Position = 0;
									 StreamUtility.CopyStream(ImageSource.Stream, stream);
									 ImageControl.DataField.Value = newValue;
									 ImageControl.LoadImage();
								 }
							 }						 
						 }
					 } 						                       
				 }
				 finally
				 {
					 if (ImageSource.GetType() == typeof(ImageCaptureForm))
						 ImageSource = null;
				 }
			}             
		}

		protected override void InitializeControl()
		{
			ImageControl.BackColor = ((Session)HostNode.Session).Theme.ContainerColor;
			ImageControl.NoValueBackColor = ((Session)HostNode.Session).Theme.NoValueBackColor;
			ImageControl.Height = DefaultImageHeight;
			ImageControl.Width = DefaultImageWidth;
			InternalUpdateStretchStyle();
			InternalUpdateCenter();
			ImageControl.OnImageRequested += LoadImage;
			base.InitializeControl();             
		}
		
		// TitledElement

		protected override int GetControlNaturalHeight()
		{
			if ((_stretchStyle == StretchStyles.NoStretch) && (ImageControl.Image != null))
				return ImageControl.Image.Height;
			if (_imageHeight > 0)
				return _imageHeight;
			else
				return DefaultImageHeight;
		}

		protected override int GetControlNaturalWidth()
		{
			if ((_stretchStyle == StretchStyles.NoStretch) && (ImageControl.Image != null))
				return ImageControl.Image.Width;
			if (_imageWidth > 0)
				return _imageWidth;
			else
				return DefaultImageWidth;
		}

		protected override void Dispose(bool disposing)
		{
			if (ImageControl != null)
				ImageControl.OnImageRequested -= new Alphora.Dataphor.DAE.Client.Controls.RequestImageHandler(LoadImage);
			base.Dispose(disposing);
		}
	}
}
