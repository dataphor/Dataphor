/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Design;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	[Description("Places a button on the form that is bound to an action.")]
	[DesignerImage("Image('Frontend', 'Nodes.Trigger')")]
	[DesignerCategory("Static Controls")]
	public class Trigger : Element, ITrigger // note: a lot of code in this class is repeated in ActionNode.  If we had multiple inheritance this wouldn't be mess.
    {		
		public const int OffsetWidth = 10;
		public const int OffsetHeight = 12;
		public const int ImageSpacing = 0;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Action = null;
		}
		
		// IVerticalAlignedElement

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// Button

		private WinForms.Button _button;
		protected WinForms.Button Button
		{
			get { return _button; }
		}

		private void SetButton(WinForms.Button button)
		{
			if (_button != button)
			{
				if (_button != null)
				{
					_button.Enter -= new EventHandler(ControlEnter);
					_button.Click -= new EventHandler(ButtonClicked);
					((Session)HostNode.Session).UnregisterControlHelp(_button);
				}
				_button = button;
				if (_button != null)
				{
					_button.Enter += new EventHandler(ControlEnter);
					_button.Click += new EventHandler(ButtonClicked);
					((Session)HostNode.Session).RegisterControlHelp(_button, this);
				}
			}
		}

		private void ControlEnter(object sender, EventArgs args)
		{
			if (Active)
				FindParent(typeof(IFormInterface)).BroadcastEvent(new FocusChangedEvent(this));
		}
		
		private void ButtonClicked(object sender, EventArgs args)
		{
			try
			{
				if (Action != null)
					Action.Execute(this, new EventParams());
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);	//don't re-throw
			}
		}
		
		// Action

		protected IAction _action;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed when this control is pressed.")]
		public IAction Action
		{
			get { return _action; }
			set
			{
				if (_action != value)
				{
					if (_action != null)
					{
						_action.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged -= new EventHandler(ActionTextChanged);
						_action.OnImageChanged -= new EventHandler(ActionImageChanged);
						_action.OnHintChanged -= new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						_action.Disposed -= new EventHandler(ActionDisposed);
					}
					_action = value;
					if (_action != null)
					{
						_action.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						_action.OnTextChanged += new EventHandler(ActionTextChanged);
						_action.OnImageChanged += new EventHandler(ActionImageChanged);
						_action.OnHintChanged += new EventHandler(ActionHintChanged);
						_action.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						_action.Disposed += new EventHandler(ActionDisposed);
					}
					if (Active)
					{
						InternalUpdateEnabled();
						InternalUpdateImage();
						InternalUpdateText();
						InternalUpdateToolTip();
						InternalUpdateVisible();
						UpdateLayout();
					}
				}
			}
		}
		
		protected void ActionDisposed(object sender, EventArgs args)
		{
			Action = null;
		}
		
		// Image

		private void ActionImageChanged(object sender, EventArgs args)
		{
			if (Active)
				UpdateImage();
		}
		
		protected virtual void InternalUpdateImage()
		{
			_button.Image = (Action == null ? null : ((Action)Action).LoadedImage);
			_button.TextAlign = (Button.Image == null ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleRight);
		}

		protected void UpdateImage()
		{
			InternalUpdateImage();
			if (Active && (Button.Image != null) && (Button.Image.Size != new Size(_imageWidth, _imageHeight)))
				UpdateLayout();
		}

		protected int _imageWidth = 0;
		[Publish(PublishMethod.Value)]
		[DefaultValue(0)]
		[Description("The width in pixels that the actions icon will be show as.")]
		public int ImageWidth
		{
			get { return _imageWidth; }
			set
			{
				if (_imageWidth != value)
				{
					_imageWidth = value;
					if (Active && (Button.Image == null))
						UpdateLayout();
				}
			}
		}

		protected int _imageHeight = 0;
		[Publish(PublishMethod.Value)]
		[DefaultValue(0)]
		[Description("The height in pixels that the actions icon will be show as.")]
		public int ImageHeight
		{
			get { return _imageHeight; }
			set
			{
				if (_imageHeight != value)
				{
					_imageHeight = value;
					if (Active && (Button.Image == null))
						UpdateLayout();
				}
			}
		}
		
		// Text

		private string _text = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("A text string that will be used by this control.  If this is not set the text property of the action will be used.")]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					UpdateText();
				}
			}
		}

		private void ActionTextChanged(object sender, EventArgs args)
		{
			if (_text == String.Empty)
				UpdateText();
		}

		private string _allocatedText;

		protected void DeallocateAccelerator()
		{
			if (_allocatedText != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(_allocatedText);
				_allocatedText = null;
			}
		}

		protected virtual void InternalUpdateText()
		{
			DeallocateAccelerator();
			_allocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);
			Button.Text = _allocatedText;
			using (Graphics graphics = Button.CreateGraphics())
				_textPixelSize = Size.Ceiling(graphics.MeasureString(Button.Text, Button.Font));
		}

		public string GetText()
		{
			if ((Action != null) && (_text == String.Empty))
				return Action.Text;
			else
				return _text;
		}

		protected Size _textPixelSize;

		protected void UpdateText()
		{
			if (Active)
			{
				InternalUpdateText();
				UpdateLayout();
			}
		}
		
		// Enabled

		private bool _enabled = true;
		[DefaultValue(true)]
		[Description("When this is set to false this control will be disabled.  This control will also be disabled if the action is disabled.")]
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					UpdateEnabled();
				}
			}
		}

		/// <summary> Gets whether the node is actuall enabled (accounting for action). </summary>
		/// <remarks>
		///		The enabled state of the node is the most restrictive between 
		///		the action and the Enabled property.
		///	</remarks>
		public bool GetEnabled()
		{
			return ( Action == null ? false : Action.GetEnabled() ) && _enabled;
		}

		private void ActionEnabledChanged(object sender, EventArgs args)
		{
			UpdateEnabled();
		}

		protected virtual void InternalUpdateEnabled()
		{
			Button.Enabled = GetEnabled();
		}

		protected void UpdateEnabled()
		{
			if (Active)
				InternalUpdateEnabled();
		}

		// TabStop

		protected override void InternalUpdateTabStop()
		{
			Button.TabStop = GetTabStop();
		}

		// Hint/Tooltip

		public override string GetHint()
		{
			if (Hint != String.Empty)
				return base.GetHint();
			else if (Action != null)
				return Action.Hint;
			else
				return String.Empty;
		}

		private void ActionHintChanged(object sender, EventArgs args)
		{
			UpdateToolTip();
		}

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(Button);
		}

		// Element

		public override bool GetVisible()
		{
			return base.GetVisible() && ((Action == null) || Action.Visible);
		}

		protected override void InternalUpdateVisible() 
		{
			_button.Visible = GetVisible();
		}

		private void ActionVisibleChanged(object sender, EventArgs args)
		{
			VisibleChanged();
		}

		public override int GetDefaultMarginLeft()
		{
			return 0;
		}

		public override int GetDefaultMarginRight()
		{
			return 0;
		}

		public override int GetDefaultMarginTop()
		{
			return 0;
		}

		public override int GetDefaultMarginBottom()
		{
			return 0;
		}

		// Node

		protected override void Activate()
		{
			SetButton(new DAE.Client.Controls.ExtendedButton());
			try
			{
				Button.BackColor = ((Windows.Session)HostNode.Session).Theme.ControlColor;
				Button.Parent = ((IWindowsContainerElement)Parent).Control;
				Button.ImageAlign = ContentAlignment.MiddleLeft;
				InternalUpdateText();
				InternalUpdateImage();
				InternalUpdateEnabled();
				base.Activate();
			}
			catch
			{
				_button.Dispose();
				SetButton(null);
				throw;
			}
		}

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				try
				{
					DeallocateAccelerator();
				}
				finally
				{
					if (Button != null)
					{
						_button.Dispose();
						SetButton(null);
					}
				}
			}
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			//Alignment within the allotted space
			if (_verticalAlignment != VerticalAlignment.Top)
			{
				int deltaX = Math.Max(0, bounds.Height - InternalNaturalSize.Height);
				if (_verticalAlignment == VerticalAlignment.Middle)
					deltaX /= 2;
				bounds.Y += deltaX;
			}
			bounds.Height -= Math.Max(0, bounds.Height - InternalNaturalSize.Height);

			Button.Bounds = bounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				Size result;

				// Account for image image size
				if (Button.Image != null)
					result = Button.Image.Size;
				else
					result = new Size(ImageWidth, ImageHeight);

				// Account for text size
				if ((Button.Text != String.Empty) && (Button.Image != null))
					result.Width += ImageSpacing;
				result.Width += _textPixelSize.Width + OffsetWidth;
				ConstrainMinHeight(ref result, _textPixelSize.Height);

				// Padding
				result.Height += OffsetHeight;

				return result;
			}
		}
	}

	public class ExtendedLabel : WinForms.Label
	{
		public ExtendedLabel() : base()
		{
			SetStyle(WinForms.ControlStyles.SupportsTransparentBackColor, true);
			SetStyle(WinForms.ControlStyles.Opaque, false);
			CausesValidation = false;
		}

		public event EventHandler OnMnemonic;

		protected override bool ProcessMnemonic(char charValue)
		{
			if (WinForms.Control.IsMnemonic(charValue, Text) && (OnMnemonic != null))
			{
				OnMnemonic(this, EventArgs.Empty);
				return true;
			}
			else
				return false;
		}
	}
    
	[Description("Displays static text.")]
	[DesignerImage("Image('Frontend', 'Nodes.StaticText')")]
	[DesignerCategory("Static Controls")]
	public class StaticText : Element, IStaticText
    {
		public const int DefaultWidth = 40;
				
		// Text

		protected string _text = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("The text to be shown on the form.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					if (Active)
					{
						SetLabelText(_text);
						UpdateLayout();
					}
				}
			}
		}
		
		protected virtual string GetText()
		{
			return _text;
		}

		// Width

		protected int _width = DefaultWidth;
		[Publish(PublishMethod.Value)]
		[DefaultValue(DefaultWidth)]
		[Description("The line width in characters that the text will be displayed as.  Extra text will be wrapped to new lines.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					if (_width < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					_width = value;
					if (Active)
						UpdateLayout();
				}
			}
		}

		protected ExtendedLabel _label;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public ExtendedLabel Label
		{
			get { return _label; }
		}
		
		protected Size _labelSize;
		
		protected void SetLabelText(string text)
		{
			// Don't allow accelerators here
			Label.Text = text.Replace("&", "&&");

			Graphics graphics = Label.CreateGraphics();
			try
			{
				_labelSize = 
					Size.Ceiling
					(
						graphics.MeasureString
						(
							text, 
							Label.Font, 
							(int)(graphics.MeasureString(Element.AverageChars, Label.Font).Width / AverageChars.Length) * _width
						)
					);
			}
			finally
			{
				graphics.Dispose();
			}
		}

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(Label);
		}

		protected override void InternalUpdateVisible() 
		{
			_label.Visible = GetVisible();
		}

		// Element

		protected override Size InternalNaturalSize
		{
			get	{ return _labelSize + GetOverhead(); }
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			Label.Bounds = bounds;
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		private void DisposeLabel()
		{
			if (_label != null)
			{
				((Session)HostNode.Session).UnregisterControlHelp(_label);
				_label.Dispose();
				_label = null;
			}
		}

		// Node

		protected override void Activate()
		{
			_label = new ExtendedLabel();
			try
			{
				_label.BackColor = ((Windows.Session)HostNode.Session).Theme.TextBackgroundColor;
				_label.AutoSize = false;
				_label.Visible = GetVisible();
				_label.Parent = ((IWindowsContainerElement)Parent).Control;
				((Session)HostNode.Session).RegisterControlHelp(_label, this);
				SetLabelText(GetText());
				base.Activate();
			}
			catch
			{
				DisposeLabel();
				throw;
			}
		}
		
		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				DisposeLabel();
			}
		}
    }

	[Description("Places an image on the form from a URL.")]
	[DesignerImage("Image('Frontend', 'Nodes.StaticImage')")]
	[DesignerCategory("Static Controls")]
	public class StaticImage : Element, IStaticImage
	{
		// TODO: True support for border (with property)

		// IVerticalAlignedElement

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return _verticalAlignment; }
			set
			{
				if (_verticalAlignment != value)
				{
					_verticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// IHorizontalAlignedElement

		protected HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return _horizontalAlignment; }
			set
			{
				if (_horizontalAlignment != value)
				{
					_horizontalAlignment = value;
					UpdateLayout();
				}
			}
		}

		private int _imageWidth = -1;
		[DefaultValue(-1)]
		[Description("The width in pixels that the image will be show as.  If set to -1 then the image's width will be used.")]
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

		private int _imageHeight = -1;
		[DefaultValue(-1)]
		[Description("The height in pixels that the image will be show as.  If set to -1 then the image's height will be used.")]
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

		// StretchStyle
		
		private StretchStyles _stretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		[Description("Specifies how to fit the image into the specified space.")]
		public StretchStyles StretchStyle
		{
			get { return _stretchStyle; }
			set 
			{ 
				if (_stretchStyle != value)
				{
					_stretchStyle = value; 
					UpdateLayout();
				}
			}
		}

		private string _image = String.Empty;
		[DefaultValue("")]
		[Description("An image to be displayed.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[Alphora.Dataphor.Frontend.Client.DocumentExpressionOperator("Image")]
		public string Image
		{
			get { return _image; }
			set
			{
				if (_image != value)
				{
					_image = value;
					if (Active)
						UpdateImageAspect();
				}
			}
		}

		private DAE.Client.Controls.ImageAspect _imageAspect;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DAE.Client.Controls.ImageAspect ImageAspect
		{
			get { return _imageAspect; }
		}

		protected PipeRequest _imageRequest;

		private void ClearImageAspectImage()
		{
			if (_imageAspect.Image != null)
			{
				_imageAspect.Image.Dispose();
				_imageAspect.Image = null;
			}
		}

		private void CancelImageRequest()
		{
			if (_imageRequest != null)
			{
				HostNode.Pipe.CancelRequest(_imageRequest);
				_imageRequest = null;
			}
		}

		private void UpdateImageAspect()
		{
			CancelImageRequest();
			if (_image == String.Empty)
				ClearImageAspectImage();
			else
			{
				// Queue up an asynchronous request
				_imageRequest = new PipeRequest(_image, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(_imageRequest);
			}
		}

		protected void ImageRead(PipeRequest request, Pipe pipe)
		{
			Size oldSize;
			if (Active)
			{
				if (_imageAspect.Image != null)
					oldSize = _imageAspect.Image.Size;
				else
					oldSize = Size.Empty;

				_imageRequest = null;
				try
				{
					if (request.Result.IsNative)
					{
						byte[] resultBytes = request.Result.AsByteArray;
						_imageAspect.Image = System.Drawing.Image.FromStream(new MemoryStream(resultBytes, 0, resultBytes.Length, false, true));
					}
					else
					{
						using (Stream stream = request.Result.OpenStream())
						{
							MemoryStream copyStream = new MemoryStream();
							StreamUtility.CopyStream(stream, copyStream);
							_imageAspect.Image = System.Drawing.Image.FromStream(copyStream);
						}
					}
				}
				catch
				{
					_imageAspect.Image = ImageUtility.GetErrorImage();
				}
				if ((ImageWidth < 0) || (ImageHeight < 0) || !(_imageAspect.Image.Size != oldSize))
					UpdateLayout();
			}
		}

		protected void ImageError(PipeRequest request, Pipe pipe, Exception exception)
		{
			// TODO: On image error, somehow update the error (warnings) list for the form
			_imageRequest = null;
			if (_imageAspect != null)
				_imageAspect.Image = ImageUtility.GetErrorImage();
			if ((ImageWidth < 0) || (ImageHeight < 0))
				UpdateLayout();
		}

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(ImageAspect);
		}

		protected override void InternalUpdateVisible() 
		{
			_imageAspect.Visible = GetVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			_imageAspect.TabStop = GetTabStop();
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		private void DisposeImageAspect()
		{
			if (_imageAspect != null)
			{
				ClearImageAspectImage();
				((Session)HostNode.Session).UnregisterControlHelp(_imageAspect);
				_imageAspect.Dispose();
				_imageAspect = null;
			}
		}

		protected override void Activate()
		{
			_imageAspect = new DAE.Client.Controls.ImageAspect();
			try
			{
				_imageAspect.BorderStyle = ((Windows.Session)HostNode.Session).Theme.ImageBorderStyle;
				_imageAspect.BackColor = ((Windows.Session)HostNode.Session).Theme.TextBackgroundColor;
				_imageAspect.Center = true;
				_imageAspect.Parent = ((IWindowsContainerElement)Parent).Control;
				((Session)HostNode.Session).RegisterControlHelp(_imageAspect, this);
				base.Activate();
			}
			catch
			{
				DisposeImageAspect();
				throw;
			}
		}

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				try
				{
					CancelImageRequest();
				}
				finally
				{
					DisposeImageAspect();
				}
			}
		}

		protected override void AfterActivate()
		{
			UpdateImageAspect();
			base.AfterActivate();
		}

		// Element

		protected override void InternalLayout(Rectangle bounds)
		{
			ImageAspect.StretchStyle = (DAE.Client.Controls.StretchStyles)_stretchStyle;

			Size imageSize;
			if (ImageAspect.Image != null)
				imageSize = ImageAspect.Image.Size;
			else
				imageSize = new Size(_imageWidth, _imageHeight);

			// adjust for alignment
			if ((imageSize.Width > -1) && (imageSize.Width < bounds.Width))
			{
				if (_horizontalAlignment != HorizontalAlignment.Left)
				{
					if (_horizontalAlignment == HorizontalAlignment.Center)
						bounds.X += (bounds.Width - imageSize.Width) / 2;
					else
						bounds.X += bounds.Width - imageSize.Width;
				}
				bounds.Width = imageSize.Width;
			}
			if ((imageSize.Height > -1) && (imageSize.Height < bounds.Height))
			{
				if (_verticalAlignment != VerticalAlignment.Top)
				{
					if (_verticalAlignment == VerticalAlignment.Middle)
						bounds.Y += (bounds.Height - imageSize.Height) / 2;
					else
						bounds.Y += bounds.Height - imageSize.Height;
				}
				bounds.Height = imageSize.Height;
			}

			ImageAspect.Bounds = bounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				Size size = new Size(_imageWidth, _imageHeight);
				if ((_imageAspect != null) && (_imageAspect.Image != null))
				{
					Size borderSize = _imageAspect.Size - _imageAspect.ClientSize;
					if (size.Width < 0)
						size.Width = _imageAspect.Image.Width + borderSize.Width;
					if (size.Height < 0)
						size.Height = _imageAspect.Image.Height + borderSize.Height;
				}
				ConstrainMin(ref size, Size.Empty);
				return size;
			}
		}
	}


	[Description("A web page control, for showing the contents of a particular URL.")]
	[DesignerImage("Image('Frontend', 'Nodes.HtmlBox')")]
	[DesignerCategory("Static Controls")]
	public class HtmlBox : Element, IHtmlBox
	{
		public const int DefaultWidth = 100;
		public const int DefaultHeight = 100;

		private int _pixelWidth = DefaultWidth;
		[DefaultValue(DefaultWidth)]
		[Description("The width in pixels that the web page will be show as.")]
		public int PixelWidth
		{
			get { return _pixelWidth; }
			set
			{
				if (_pixelWidth != value)
				{
					_pixelWidth = value;
					UpdateLayout();
				}
			}
		}

		private int _pixelHeight = DefaultHeight;
		[DefaultValue(DefaultHeight)]
		[Description("The height in pixels that the web page will be show as.")]
		public int PixelHeight
		{
			get { return _pixelHeight; }
			set
			{
				if (_pixelHeight != value)
				{
					_pixelHeight = value;
					UpdateLayout();
				}
			}
		}

		private string _uRL = String.Empty;
		[DefaultValue("")]
		[Description("The URL of the web page to be displayed.")]
		public string URL
		{
			get { return _uRL; }
			set
			{
				if (_uRL != value)
				{
					_uRL = value;
					if (Active)
						InternalUpdateURL();
				}
			}
		}

		private WinForms.WebBrowser _webBrowser;
		[Browsable(false)]
		public WinForms.WebBrowser WebBrowser
		{
			get { return _webBrowser; }
		}

		protected override void InternalUpdateVisible() 
		{
			_webBrowser.Visible = GetVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			_webBrowser.TabStop = GetTabStop();
		}

		private void InternalUpdateURL()
		{
			if (_uRL != String.Empty) 
			{
				try 
				{
					_webBrowser.Navigate(_uRL);
					return;
				}
				catch 
				{
					// Do nothing
				}
			}
			_webBrowser.Navigate("about:blank");
		}

		private void DisposeWebBrowser()
		{
			if (_webBrowser != null)
			{
				((Session)HostNode.Session).UnregisterControlHelp(_webBrowser);
				_webBrowser.Dispose();
				_webBrowser = null;
			}
		}

		protected override void Activate()
		{
			_webBrowser = new WinForms.WebBrowser();
			try 
			{
				_webBrowser.Parent = ((IWindowsContainerElement)Parent).Control;
				_webBrowser.Visible = GetVisible();
				_webBrowser.TabStop = GetTabStop();
				base.Activate();
				InternalUpdateURL();
				((Session)HostNode.Session).RegisterControlHelp(_webBrowser, this);
			} 
			catch
			{
				DisposeWebBrowser();
				throw;
			}
		}

		protected override void Deactivate()
		{
			try
			{
				base.Deactivate();
			}
			finally
			{
				DisposeWebBrowser();
			}
		}

		// Element

		protected override void InternalLayout(Rectangle bounds)
		{
			if (_webBrowser != null) 
				_webBrowser.Bounds = bounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get { return new Size(_pixelWidth, _pixelHeight); }
		}
	}
}