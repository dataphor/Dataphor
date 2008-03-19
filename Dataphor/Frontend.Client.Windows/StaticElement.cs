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
		public const int COffsetWidth = 10;
		public const int COffsetHeight = 12;
		public const int CImageSpacing = 0;

		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Action = null;
		}
		
		// IVerticalAlignedElement

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// Button

		private WinForms.Button FButton;
		protected WinForms.Button Button
		{
			get { return FButton; }
		}

		private void SetButton(WinForms.Button AButton)
		{
			if (FButton != AButton)
			{
				if (FButton != null)
				{
					FButton.Enter -= new EventHandler(ControlEnter);
					FButton.Click -= new EventHandler(ButtonClicked);
					((Session)HostNode.Session).UnregisterControlHelp(FButton);
				}
				FButton = AButton;
				if (FButton != null)
				{
					FButton.Enter += new EventHandler(ControlEnter);
					FButton.Click += new EventHandler(ButtonClicked);
					((Session)HostNode.Session).RegisterControlHelp(FButton, this);
				}
			}
		}

		private void ControlEnter(object ASender, EventArgs AArgs)
		{
			if (Active)
				FindParent(typeof(IFormInterface)).BroadcastEvent(new FocusChangedEvent(this));
		}
		
		private void ButtonClicked(object ASender, EventArgs AArgs)
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

		protected IAction FAction;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("An action that will be executed when this control is pressed.")]
		public IAction Action
		{
			get { return FAction; }
			set
			{
				if (FAction != value)
				{
					if (FAction != null)
					{
						FAction.OnEnabledChanged -= new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged -= new EventHandler(ActionTextChanged);
						FAction.OnImageChanged -= new EventHandler(ActionImageChanged);
						FAction.OnHintChanged -= new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged -= new EventHandler(ActionVisibleChanged);
						FAction.Disposed -= new EventHandler(ActionDisposed);
					}
					FAction = value;
					if (FAction != null)
					{
						FAction.OnEnabledChanged += new EventHandler(ActionEnabledChanged);
						FAction.OnTextChanged += new EventHandler(ActionTextChanged);
						FAction.OnImageChanged += new EventHandler(ActionImageChanged);
						FAction.OnHintChanged += new EventHandler(ActionHintChanged);
						FAction.OnVisibleChanged += new EventHandler(ActionVisibleChanged);
						FAction.Disposed += new EventHandler(ActionDisposed);
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
		
		protected void ActionDisposed(object ASender, EventArgs AArgs)
		{
			Action = null;
		}
		
		// Image

		private void ActionImageChanged(object ASender, EventArgs AArgs)
		{
			if (Active)
				UpdateImage();
		}
		
		protected virtual void InternalUpdateImage()
		{
			FButton.Image = (Action == null ? null : Action.LoadedImage);
			FButton.TextAlign = (Button.Image == null ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleRight);
		}

		protected void UpdateImage()
		{
			InternalUpdateImage();
			if (Active && (Button.Image != null) && (Button.Image.Size != new Size(FImageWidth, FImageHeight)))
				UpdateLayout();
		}

		protected int FImageWidth = 0;
		[Publish(PublishMethod.Value)]
		[DefaultValue(0)]
		[Description("The width in pixels that the actions icon will be show as.")]
		public int ImageWidth
		{
			get { return FImageWidth; }
			set
			{
				if (FImageWidth != value)
				{
					FImageWidth = value;
					if (Active && (Button.Image == null))
						UpdateLayout();
				}
			}
		}

		protected int FImageHeight = 0;
		[Publish(PublishMethod.Value)]
		[DefaultValue(0)]
		[Description("The height in pixels that the actions icon will be show as.")]
		public int ImageHeight
		{
			get { return FImageHeight; }
			set
			{
				if (FImageHeight != value)
				{
					FImageHeight = value;
					if (Active && (Button.Image == null))
						UpdateLayout();
				}
			}
		}
		
		// Text

		private string FText = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("A text string that will be used by this control.  If this is not set the text property of the action will be used.")]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					UpdateText();
				}
			}
		}

		private void ActionTextChanged(object ASender, EventArgs AArgs)
		{
			if (FText == String.Empty)
				UpdateText();
		}

		private string FAllocatedText;

		protected void DeallocateAccelerator()
		{
			if (FAllocatedText != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(FAllocatedText);
				FAllocatedText = null;
			}
		}

		protected virtual void InternalUpdateText()
		{
			DeallocateAccelerator();
			FAllocatedText = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(GetText(), true);
			Button.Text = FAllocatedText;
			using (Graphics LGraphics = Button.CreateGraphics())
				FTextPixelSize = Size.Ceiling(LGraphics.MeasureString(Button.Text, Button.Font));
		}

		public string GetText()
		{
			if ((Action != null) && (FText == String.Empty))
				return Action.Text;
			else
				return FText;
		}

		protected Size FTextPixelSize;

		protected void UpdateText()
		{
			if (Active)
			{
				InternalUpdateText();
				UpdateLayout();
			}
		}
		
		// Enabled

		private bool FEnabled = true;
		[DefaultValue(true)]
		[Description("When this is set to false this control will be disabled.  This control will also be disabled if the action is disabled.")]
		public bool Enabled
		{
			get { return FEnabled; }
			set
			{
				if (FEnabled != value)
				{
					FEnabled = value;
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
			return ( Action == null ? false : Action.GetEnabled() ) && FEnabled;
		}

		private void ActionEnabledChanged(object ASender, EventArgs AArgs)
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

		private void ActionHintChanged(object ASender, EventArgs AArgs)
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
			FButton.Visible = GetVisible();
		}

		private void ActionVisibleChanged(object ASender, EventArgs AArgs)
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
				FButton.Dispose();
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
						FButton.Dispose();
						SetButton(null);
					}
				}
			}
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			//Alignment within the allotted space
			if (FVerticalAlignment != VerticalAlignment.Top)
			{
				int LDeltaX = Math.Max(0, ABounds.Height - InternalNaturalSize.Height);
				if (FVerticalAlignment == VerticalAlignment.Middle)
					LDeltaX /= 2;
				ABounds.Y += LDeltaX;
			}
			ABounds.Height -= Math.Max(0, ABounds.Height - InternalNaturalSize.Height);

			Button.Bounds = ABounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				Size LResult;

				// Account for image image size
				if (Button.Image != null)
					LResult = Button.Image.Size;
				else
					LResult = new Size(ImageWidth, ImageHeight);

				// Account for text size
				if ((Button.Text != String.Empty) && (Button.Image != null))
					LResult.Width += CImageSpacing;
				LResult.Width += FTextPixelSize.Width + COffsetWidth;
				ConstrainMinHeight(ref LResult, FTextPixelSize.Height);

				// Padding
				LResult.Height += COffsetHeight;

				return LResult;
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

		protected override bool ProcessMnemonic(char AChar)
		{
			if (WinForms.Control.IsMnemonic(AChar, Text) && (OnMnemonic != null))
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
		public const int CDefaultWidth = 40;
				
		// Text

		protected string FText = String.Empty;
		[Publish(PublishMethod.Value)]
		[DefaultValue("")]
		[Description("The text to be shown on the form.")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string Text
		{
			get { return FText; }
			set
			{
				if (FText != value)
				{
					FText = value;
					if (Active)
					{
						SetLabelText(FText);
						UpdateLayout();
					}
				}
			}
		}
		
		protected virtual string GetText()
		{
			return FText;
		}

		// Width

		protected int FWidth = CDefaultWidth;
		[Publish(PublishMethod.Value)]
		[DefaultValue(CDefaultWidth)]
		[Description("The line width in characters that the text will be displayed as.  Extra text will be wrapped to new lines.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					if (FWidth < 1)
						throw new ClientException(ClientException.Codes.CharsPerLineInvalid);
					FWidth = value;
					if (Active)
						UpdateLayout();
				}
			}
		}

		protected ExtendedLabel FLabel;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public ExtendedLabel Label
		{
			get { return FLabel; }
		}
		
		protected Size FLabelSize;
		
		protected void SetLabelText(string AText)
		{
			// Don't allow accelerators here
			Label.Text = AText.Replace("&", "&&");

			Graphics LGraphics = Label.CreateGraphics();
			try
			{
				FLabelSize = 
					Size.Ceiling
					(
						LGraphics.MeasureString
						(
							AText, 
							Label.Font, 
							(int)(LGraphics.MeasureString(Element.CAverageChars, Label.Font).Width / CAverageChars.Length) * FWidth
						)
					);
			}
			finally
			{
				LGraphics.Dispose();
			}
		}

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(Label);
		}

		protected override void InternalUpdateVisible() 
		{
			FLabel.Visible = GetVisible();
		}

		// Element

		protected override Size InternalNaturalSize
		{
			get	{ return FLabelSize + GetOverhead(); }
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			Label.Bounds = ABounds;
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		private void DisposeLabel()
		{
			if (FLabel != null)
			{
				((Session)HostNode.Session).UnregisterControlHelp(FLabel);
				FLabel.Dispose();
				FLabel = null;
			}
		}

		// Node

		protected override void Activate()
		{
			FLabel = new ExtendedLabel();
			try
			{
				FLabel.BackColor = ((Windows.Session)HostNode.Session).Theme.TextBackgroundColor;
				FLabel.AutoSize = false;
				FLabel.Visible = GetVisible();
				FLabel.Parent = ((IWindowsContainerElement)Parent).Control;
				((Session)HostNode.Session).RegisterControlHelp(FLabel, this);
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

		public const int CDefaultWidth = 100;
		public const int CDefaultHeight = 100;


		// IVerticalAlignedElement

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public VerticalAlignment VerticalAlignment
		{
			get { return FVerticalAlignment; }
			set
			{
				if (FVerticalAlignment != value)
				{
					FVerticalAlignment = value;
					UpdateLayout();
				}
			}
		}

		// IHorizontalAlignedElement

		protected HorizontalAlignment FHorizontalAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("When this element is given more space than it can use, this property will control where the element will be placed within it's space.")]
		public HorizontalAlignment HorizontalAlignment
		{
			get { return FHorizontalAlignment; }
			set
			{
				if (FHorizontalAlignment != value)
				{
					FHorizontalAlignment = value;
					UpdateLayout();
				}
			}
		}

		private int FImageWidth = -1;
		[DefaultValue(-1)]
		[Description("The width in pixels that the image will be show as.  If set to -1 then the image's width will be used.")]
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

		private int FImageHeight = -1;
		[DefaultValue(-1)]
		[Description("The height in pixels that the image will be show as.  If set to -1 then the image's height will be used.")]
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

		// StretchStyle
		
		private StretchStyles FStretchStyle = StretchStyles.StretchRatio;
		[DefaultValue(StretchStyles.StretchRatio)]
		[Description("Specifies how to fit the image into the specified space.")]
		public StretchStyles StretchStyle
		{
			get { return FStretchStyle; }
			set 
			{ 
				if (FStretchStyle != value)
				{
					FStretchStyle = value; 
					UpdateLayout();
				}
			}
		}

		private string FImage = String.Empty;
		[DefaultValue("")]
		[Description("An image to be displayed.")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[Alphora.Dataphor.Frontend.Client.DocumentExpressionOperator("Image")]
		public string Image
		{
			get { return FImage; }
			set
			{
				if (FImage != value)
				{
					FImage = value;
					if (Active)
						UpdateImageAspect();
				}
			}
		}

		private DAE.Client.Controls.ImageAspect FImageAspect;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public DAE.Client.Controls.ImageAspect ImageAspect
		{
			get { return FImageAspect; }
		}

		protected PipeRequest FImageRequest;

		private void ClearImageAspectImage()
		{
			if (FImageAspect.Image != null)
			{
				FImageAspect.Image.Dispose();
				FImageAspect.Image = null;
			}
		}

		private void CancelImageRequest()
		{
			if (FImageRequest != null)
			{
				HostNode.Pipe.CancelRequest(FImageRequest);
				FImageRequest = null;
			}
		}

		private void UpdateImageAspect()
		{
			CancelImageRequest();
			if (FImage == String.Empty)
				ClearImageAspectImage();
			else
			{
				// Queue up an asynchronous request
				FImageRequest = new PipeRequest(FImage, new PipeResponseHandler(ImageRead), new PipeErrorHandler(ImageError));
				HostNode.Pipe.QueueRequest(FImageRequest);
			}
		}

		protected void ImageRead(PipeRequest ARequest, Pipe APipe)
		{
			Size LOldSize;
			if (Active)
			{
				if (FImageAspect.Image != null)
					LOldSize = FImageAspect.Image.Size;
				else
					LOldSize = Size.Empty;

				FImageRequest = null;
				try
				{
					if (ARequest.Result.IsNative)
					{
						FImageAspect.Image = System.Drawing.Image.FromStream(new MemoryStream(ARequest.Result.AsByteArray, false));
					}
					else
					{
						using (Stream LStream = ARequest.Result.OpenStream())
						{
							MemoryStream LCopyStream = new MemoryStream();
							StreamUtility.CopyStream(LStream, LCopyStream);
							FImageAspect.Image = System.Drawing.Image.FromStream(LCopyStream);
						}
					}
				}
				catch
				{
					FImageAspect.Image = ImageUtility.GetErrorImage();
				}
				if ((ImageWidth < 0) || (ImageHeight < 0) || !(FImageAspect.Image.Size != LOldSize))
					UpdateLayout();
			}
		}

		protected void ImageError(PipeRequest ARequest, Pipe APipe, Exception AException)
		{
			// TODO: On image error, somehow update the error (warnings) list for the form
			FImageRequest = null;
			if (FImageAspect != null)
				FImageAspect.Image = ImageUtility.GetErrorImage();
			if ((ImageWidth < 0) || (ImageHeight < 0))
				UpdateLayout();
		}

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(ImageAspect);
		}

		protected override void InternalUpdateVisible() 
		{
			FImageAspect.Visible = GetVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			FImageAspect.TabStop = GetTabStop();
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		private void DisposeImageAspect()
		{
			if (FImageAspect != null)
			{
				ClearImageAspectImage();
				((Session)HostNode.Session).UnregisterControlHelp(FImageAspect);
				FImageAspect.Dispose();
				FImageAspect = null;
			}
		}

		protected override void Activate()
		{
			FImageAspect = new DAE.Client.Controls.ImageAspect();
			try
			{
				FImageAspect.BorderStyle = ((Windows.Session)HostNode.Session).Theme.ImageBorderStyle;
				FImageAspect.BackColor = ((Windows.Session)HostNode.Session).Theme.TextBackgroundColor;
				FImageAspect.Center = true;
				FImageAspect.Parent = ((IWindowsContainerElement)Parent).Control;
				((Session)HostNode.Session).RegisterControlHelp(FImageAspect, this);
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

		protected override void InternalLayout(Rectangle ABounds)
		{
			ImageAspect.StretchStyle = (DAE.Client.Controls.StretchStyles)FStretchStyle;

			Size LImageSize;
			if (ImageAspect.Image != null)
				LImageSize = ImageAspect.Image.Size;
			else
				LImageSize = new Size(FImageWidth, FImageHeight);

			// adjust for alignment
			if ((LImageSize.Width > -1) && (LImageSize.Width < ABounds.Width))
			{
				if (FHorizontalAlignment != HorizontalAlignment.Left)
				{
					if (FHorizontalAlignment == HorizontalAlignment.Center)
						ABounds.X += (ABounds.Width - LImageSize.Width) / 2;
					else
						ABounds.X += ABounds.Width - LImageSize.Width;
				}
				ABounds.Width = LImageSize.Width;
			}
			if ((LImageSize.Height > -1) && (LImageSize.Height < ABounds.Height))
			{
				if (FVerticalAlignment != VerticalAlignment.Top)
				{
					if (FVerticalAlignment == VerticalAlignment.Middle)
						ABounds.Y += (ABounds.Height - LImageSize.Height) / 2;
					else
						ABounds.Y += ABounds.Height - LImageSize.Height;
				}
				ABounds.Height = LImageSize.Height;
			}

			ImageAspect.Bounds = ABounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				Size LSize = new Size(FImageWidth, FImageHeight);
				if ((FImageAspect != null) && (FImageAspect.Image != null))
				{
					Size LBorderSize = FImageAspect.Size - FImageAspect.ClientSize;
					if (LSize.Width < 0)
						LSize.Width = FImageAspect.Image.Width + LBorderSize.Width;
					if (LSize.Height < 0)
						LSize.Height = FImageAspect.Image.Height + LBorderSize.Height;
				}
				ConstrainMin(ref LSize, Size.Empty);
				return LSize;
			}
		}
	}


	[Description("A web page control, for showing the contents of a particular URL.")]
	[DesignerImage("Image('Frontend', 'Nodes.HtmlBox')")]
	[DesignerCategory("Static Controls")]
	public class HtmlBox : Element, IHtmlBox
	{
		public const int CDefaultWidth = 100;
		public const int CDefaultHeight = 100;

		private int FPixelWidth = CDefaultWidth;
		[DefaultValue(CDefaultWidth)]
		[Description("The width in pixels that the web page will be show as.")]
		public int PixelWidth
		{
			get { return FPixelWidth; }
			set
			{
				if (FPixelWidth != value)
				{
					FPixelWidth = value;
					UpdateLayout();
				}
			}
		}

		private int FPixelHeight = CDefaultHeight;
		[DefaultValue(CDefaultHeight)]
		[Description("The height in pixels that the web page will be show as.")]
		public int PixelHeight
		{
			get { return FPixelHeight; }
			set
			{
				if (FPixelHeight != value)
				{
					FPixelHeight = value;
					UpdateLayout();
				}
			}
		}

		private string FURL = String.Empty;
		[DefaultValue("")]
		[Description("The URL of the web page to be displayed.")]
		public string URL
		{
			get { return FURL; }
			set
			{
				if (FURL != value)
				{
					FURL = value;
					if (Active)
						InternalUpdateURL();
				}
			}
		}

		private WinForms.WebBrowser FWebBrowser;
		[Browsable(false)]
		public WinForms.WebBrowser WebBrowser
		{
			get { return FWebBrowser; }
		}

		protected override void InternalUpdateVisible() 
		{
			FWebBrowser.Visible = GetVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			FWebBrowser.TabStop = GetTabStop();
		}

		private void InternalUpdateURL()
		{
			if (FURL != String.Empty) 
			{
				try 
				{
					FWebBrowser.Navigate(FURL);
					return;
				}
				catch 
				{
					// Do nothing
				}
			}
			FWebBrowser.Navigate("about:blank");
		}

		private void DisposeWebBrowser()
		{
			if (FWebBrowser != null)
			{
				((Session)HostNode.Session).UnregisterControlHelp(FWebBrowser);
				FWebBrowser.Dispose();
				FWebBrowser = null;
			}
		}

		protected override void Activate()
		{
			FWebBrowser = new WinForms.WebBrowser();
			try 
			{
				FWebBrowser.Parent = ((IWindowsContainerElement)Parent).Control;
				FWebBrowser.Visible = GetVisible();
				FWebBrowser.TabStop = GetTabStop();
				base.Activate();
				InternalUpdateURL();
				((Session)HostNode.Session).RegisterControlHelp(FWebBrowser, this);
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

		protected override void InternalLayout(Rectangle ABounds)
		{
			if (FWebBrowser != null) 
				FWebBrowser.Bounds = ABounds;
		}
		
		protected override Size InternalNaturalSize
		{
			get { return new Size(FPixelWidth, FPixelHeight); }
		}
	}
}