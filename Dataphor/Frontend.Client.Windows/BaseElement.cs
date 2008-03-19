/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.ComponentModel;
using WinForms = System.Windows.Forms;

using DAE = Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public abstract class DataElement : Element, IDataElement
	{
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}

		// Source

		private ISource FSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the source node the control will be attached to.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != value)
					SetSource(value);
			}
		}

		protected virtual void SetSource(ISource ASource)
		{
			if (FSource != null)
				FSource.Disposed -= new EventHandler(SourceDisposed);
			FSource = ASource;
			if (FSource != null)
				FSource.Disposed += new EventHandler(SourceDisposed);
			if (Active)
				InternalUpdateSource();
		}
		
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		protected abstract void InternalUpdateSource();
		
		// ReadOnly

		private bool FReadOnly;
		[DefaultValue(false)]
		[Description("When true the control will not modify the data in the data source.")]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set
			{
				if (FReadOnly != value)
				{
					FReadOnly = value;
					if (Active)
						UpdateReadOnly();
				}
			}
		}

		private void UpdateReadOnly()
		{
			InternalUpdateReadOnly();
			InternalUpdateTabStop();
		}

		protected abstract void InternalUpdateReadOnly();

		public virtual bool GetReadOnly()
		{
			return FReadOnly;
		}

		// Element

		public override bool GetTabStop()
		{
			return base.GetTabStop() && !GetReadOnly();
		}

		// Node

		protected override void Activate()
		{
			InternalUpdateReadOnly();
			base.Activate();
			InternalUpdateSource();
		}
	}

	/// <summary> Encapsulates a data aware control. </summary>
	/// <remarks> Encapsulated control must implement Alphora.Dataphor.DAE.Client.IDataSourceReference. </remarks>
	public abstract class ControlElement : DataElement, IWindowsControlElement
	{
		protected abstract WinForms.Control CreateControl();

		protected virtual void InitializeControl()
		{
			Control.Parent = ((IWindowsContainerElement)Parent).Control;
		}

		// Control

		private WinForms.Control FControl;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public WinForms.Control Control
		{
			get { return FControl; }
			set
			{
				if (FControl != value)
				{
					if (FControl != null)
					{
						((Session)HostNode.Session).UnregisterControlHelp(FControl);
						FControl.GotFocus -= new EventHandler(ControlGotFocus);
					}
					FControl = value;
					if (FControl != null)
					{
						((Session)HostNode.Session).RegisterControlHelp(FControl, this);
						FControl.GotFocus += new EventHandler(ControlGotFocus);
					}
				}
			}
		}

		private void ControlGotFocus(object ASender, EventArgs AArgs)
		{
			FindParent(typeof(IFormInterface)).BroadcastEvent(new FocusChangedEvent(this));
		}
		
		// Element

		protected override void InternalUpdateToolTip()
		{
			SetToolTip(Control);
		}

		protected override void InternalUpdateVisible()
		{
			Control.Visible = GetVisible();
		}

		protected override void InternalUpdateTabStop()
		{
			Control.TabStop = GetTabStop();
		}

		// ControlElement

		protected override void InternalUpdateReadOnly()
		{
			DAE.Client.IReadOnly LReadOnlyControl = Control as DAE.Client.IReadOnly;
			if (LReadOnlyControl != null)
				LReadOnlyControl.ReadOnly = GetReadOnly();
		}

		// DataElement

		protected override void InternalUpdateSource()
		{
			((DAE.Client.IDataSourceReference)FControl).Source = (Source == null ? null : Source.DataSource);
		}

		// Node

		protected override void Activate()
		{
			Control = CreateControl();
			try
			{
				FControl.CausesValidation = false;
				InitializeControl();
				InternalUpdateSource();
				base.Activate();
			}
			catch
			{
				if (FControl != null)
				{
					FControl.Dispose();
					FControl = null;
				}
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
				if (FControl != null)
				{
					DAE.Client.IDataSourceReference LSourceReference = FControl as DAE.Client.IDataSourceReference;
					if (LSourceReference != null)
						LSourceReference.Source = null;
					FControl.Dispose();
					FControl = null;
				}
			}
		}

		// Element

		protected override void InternalLayout(Rectangle ABounds)
		{
			Control.Bounds = ABounds;
		}
	}

	/// <remarks> Encapsulated control must implement Alphora.Dataphor.DAE.Client.IColumnNameReference. </remarks>
	public abstract class ColumnElement : ControlElement, IColumnElement
	{
		// ColumnName

		private string FColumnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column in the data source this element is associated with.")]
		public string ColumnName
		{
			get { return FColumnName; }
			set
			{
				if (FColumnName != value)
				{
					FColumnName = value;
					if (Active)
						InternalUpdateColumnName();
				}
			}
		}

		protected virtual void InternalUpdateColumnName()
		{
			DAE.Client.IColumnNameReference LNameReference = Control as DAE.Client.IColumnNameReference;
			if (LNameReference != null)
				LNameReference.ColumnName = ColumnName;
		}

		// Title		

		private string FTitle = String.Empty;
		[DefaultValue("")]
		[Description("Text that describes the control.")]
		public string Title
		{
			get { return FTitle; }
			set
			{
				if (FTitle != value)
				{
					FTitle = value == null ? String.Empty : value;
					if (Active)
						InternalUpdateTitle();
					UpdateLayout();
				}
			}
		}

		private string FAllocatedTitle;

		protected void DeallocateAccelerator()
		{
			if (FAllocatedTitle != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(FAllocatedTitle);
				FAllocatedTitle = null;
			}
		}

		protected virtual void InternalUpdateTitle()
		{
			DeallocateAccelerator();
			FAllocatedTitle = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(FTitle.Equals(String.Empty) ? ColumnName : FTitle, false);
			SetControlText(FAllocatedTitle);
		}

		protected virtual void SetControlText(string AText)
		{
			Control.Text = AText;
		}

		// ControlElement

		protected override void InitializeControl()
		{
			InternalUpdateColumnName();
			InternalUpdateTitle();
			base.InitializeControl();
		}

		// Node

		protected override void Activate()
		{
			try
			{
				base.Activate();
			}
			catch
			{
				DeallocateAccelerator();
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
				DeallocateAccelerator();
			}
		}
	}

	public abstract class TitledElement : ColumnElement, ITitledElement
	{
		public const int CLabelVSpacing = 4;
		public const int CLabelHSpacing = 2;

		// VerticalAlignment

		protected VerticalAlignment FVerticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this control is given more space than it can use, this property specifies where the control will be placed within it's space.")]
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

		// TitleAlignment		

		private TitleAlignment FTitleAlignment = TitleAlignment.Top;
		[DefaultValue(TitleAlignment.Top)]
		[Description("The alignment of the title relative to the control.")]
		public TitleAlignment TitleAlignment
		{
			get { return FTitleAlignment; }
			set
			{
				if (FTitleAlignment != value)
				{
					FTitleAlignment = value;
					UpdateLayout();
				}
			}
		}

		#region Label

		private ExtendedLabel FLabel;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected ExtendedLabel Label
		{
			get { return FLabel; }
		}

		private void CreateLabel()
		{
			FLabel = new ExtendedLabel();
			try
			{
				FLabel.BackColor = ((Session)HostNode.Session).Theme.TextBackgroundColor;
				FLabel.AutoSize = true;
				FLabel.Parent = ((IWindowsContainerElement)Parent).Control;
				FLabel.OnMnemonic += new EventHandler(LabelMnemonic);
			}
			catch
			{
				DisposeLabel();
				throw;
			}
		}

		private void DisposeLabel()
		{
			if (FLabel != null)
			{
				FLabel.Dispose();
				FLabel = null;
			}
		}

		private void LabelMnemonic(object ASender, EventArgs AArgs)
		{
			if ((Control != null) && Control.CanFocus)
				Control.Focus();
		}

		#endregion

		// AverageCharPixelWidth

		private int FAverageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected int AverageCharPixelWidth
		{
			get { return FAverageCharPixelWidth; }
		}

		// ColumnElement

		protected override void InternalUpdateColumnName()
		{
			base.InternalUpdateColumnName();
			if ((Title == String.Empty) && Active)
				InternalUpdateTitle();
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			UpdateLayout();
		}

		private Size FLabelPixelSize;

		protected override void SetControlText(string AText)
		{
			FLabel.Text = AText;
			using (Graphics LGraphics = FLabel.CreateGraphics())
				FLabelPixelSize = Size.Ceiling(LGraphics.MeasureString(FLabel.Text, FLabel.Font));
		}
		
		// Node

		protected override void Activate()
		{
			CreateLabel();
			try
			{
				base.Activate();
				FAverageCharPixelWidth = Element.GetAverageCharPixelWidth(Control);
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

		// Element

		protected override void InternalUpdateVisible()
		{
			FLabel.Visible = (TitleAlignment != TitleAlignment.None) && GetVisible();
			base.InternalUpdateVisible();
		}

		protected virtual void LayoutControl(Rectangle ABounds)
		{
			Control.Bounds = ABounds;
		}

		protected override void InternalLayout(Rectangle ABounds)
		{
			InternalUpdateVisible();

			//Alignment within the allotted space
			if (FVerticalAlignment != VerticalAlignment.Top)
			{
				int LDeltaX = Math.Max(0, ABounds.Height - MaxSize.Height);
				if (FVerticalAlignment == VerticalAlignment.Middle)
					LDeltaX /= 2;
				ABounds.Y += LDeltaX;
				ABounds.Height -= LDeltaX;
			}

			// Title alignment
			if (TitleAlignment != TitleAlignment.None)
			{
				FLabel.Location = ABounds.Location;
				if (TitleAlignment == TitleAlignment.Top)
				{
					int LOffset = FLabelPixelSize.Height + CLabelVSpacing;
					ABounds.Y += LOffset;
					ABounds.Height -= LOffset;
				}
				else
				{
					int LOffset = FLabelPixelSize.Width + CLabelHSpacing;
					ABounds.X += LOffset;
					ABounds.Width -= LOffset;
				}
			}
			
			// Enforce maximums
			if (EnforceMaxHeight())
			{
				Size LSize = ABounds.Size;
				ConstrainMaxHeight(ref LSize, GetControlMaxHeight());
				ABounds.Size = LSize;
			}

			LayoutControl(ABounds);
		}
		
		private Size AdjustForAlignment(Size ASize)
		{
			switch (TitleAlignment)
			{
				case TitleAlignment.Top :
					if (ASize.Width < FLabelPixelSize.Width)
						ASize.Width = FLabelPixelSize.Width;
					ASize.Height += FLabelPixelSize.Height + CLabelVSpacing;
					break;
				case TitleAlignment.Left :
					ASize.Width += FLabelPixelSize.Width + CLabelHSpacing;
					break;
			}
			return ASize;
		}

		protected virtual bool EnforceMaxHeight()
		{
			return false;
		}

		protected virtual int GetControlNaturalWidth()
		{
			return 0;
		}
		
		protected virtual int GetControlMaxWidth()
		{
			return WinForms.Screen.FromControl(Control).WorkingArea.Width;
		}
		
		protected virtual int GetControlMinWidth()
		{
			return FAverageCharPixelWidth;
		}
		
		protected virtual int GetControlNaturalHeight()
		{
			return Control.Height;
		}
		
		protected virtual int GetControlMaxHeight()
		{
			return GetControlNaturalHeight();
		}
		
		protected virtual int GetControlMinHeight()
		{
			return GetControlNaturalHeight();
		}
		
		protected override Size InternalMinSize
		{
			get
			{
				return AdjustForAlignment(new Size(GetControlMinWidth(), GetControlMinHeight()));
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				return AdjustForAlignment(new Size(GetControlMaxWidth(), GetControlMaxHeight()));
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				return AdjustForAlignment(new Size(GetControlNaturalWidth(), GetControlNaturalHeight()));
			}
		}
	}

	public abstract class AlignedElement : TitledElement, IAlignedElement
	{
		public const int CDefaultWidth = 10;
		public const int CMinWidth = 16;

		// Width

		protected int FWidth = CDefaultWidth;
		[Publish(PublishMethod.Value)]
		[DefaultValue(CDefaultWidth)]
		[Description("Specifies the width, in characters, of the control.")]
		public int Width
		{
			get { return FWidth; }
			set
			{
				if (FWidth != value)
				{
					FWidth = value;
					UpdateLayout();
				}
			}
		}

		// MaxWidth

		private int FMaxWidth = -1;
		[DefaultValue(-1)]
		[Description("Specifies the maximum width, in characters, the control can grow to be.")]
		public int MaxWidth
		{
			get { return FMaxWidth; }
			set
			{
				if (FMaxWidth != value)
				{
					FMaxWidth = value;
					if (Active)
						UpdateLayout();
				}
			}
		}

		// TextAlignment		

		private HorizontalAlignment FTextAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("Specifies the alignment of the text within the element.")]
		public HorizontalAlignment TextAlignment
		{
			get { return FTextAlignment; }
			set
			{
				if (FTextAlignment != value)
				{
					FTextAlignment = value;
					if (Active)
						InternalUpdateTextAlignment();
					UpdateLayout();
				}
			}
		}

		protected abstract void InternalUpdateTextAlignment();

		// TitledElement

		protected override int GetControlNaturalWidth()
		{
			return FWidth * AverageCharPixelWidth;
		}
		
		protected override int GetControlMaxWidth()
		{
			if (FMaxWidth > 0)
				return FMaxWidth * AverageCharPixelWidth;
			else
				return WinForms.Screen.FromControl(Control).WorkingArea.Width;
		}
		
		// DataColumnElement

		protected override void InitializeControl()
		{
			InternalUpdateTextAlignment();
			base.InitializeControl();
		}
	}
}