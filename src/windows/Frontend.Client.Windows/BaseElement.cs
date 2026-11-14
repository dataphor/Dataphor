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
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}

		// Source

		private ISource _source;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specifies the source node the control will be attached to.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != value)
					SetSource(value);
			}
		}

		protected virtual void SetSource(ISource source)
		{
			if (_source != null)
				_source.Disposed -= new EventHandler(SourceDisposed);
			_source = source;
			if (_source != null)
				_source.Disposed += new EventHandler(SourceDisposed);
			if (Active)
				InternalUpdateSource();
		}
		
		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		protected abstract void InternalUpdateSource();
		
		// ReadOnly

		private bool _readOnly;
		[DefaultValue(false)]
		[Description("When true the control will not modify the data in the data source.")]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set
			{
				if (_readOnly != value)
				{
					_readOnly = value;
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
			return _readOnly;
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

		private WinForms.Control _control;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		public WinForms.Control Control
		{
			get { return _control; }
			set
			{
				if (_control != value)
				{
					if (_control != null)
					{
						((Session)HostNode.Session).UnregisterControlHelp(_control);
						_control.Enter -= new EventHandler(ControlGotFocus);
					}
					_control = value;
					if (_control != null)
					{
						((Session)HostNode.Session).RegisterControlHelp(_control, this);
						_control.Enter += new EventHandler(ControlGotFocus);
					}
				}
			}
		}

		protected void ControlGotFocus(object sender, EventArgs args)
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
			DAE.Client.IReadOnly readOnlyControl = Control as DAE.Client.IReadOnly;
			if (readOnlyControl != null)
				readOnlyControl.ReadOnly = GetReadOnly();
		}

		// DataElement

		protected override void InternalUpdateSource()
		{
			((DAE.Client.IDataSourceReference)_control).Source = (Source == null ? null : Source.DataSource);
		}

		// Node

		protected override void Activate()
		{
			Control = CreateControl();
			try
			{
				_control.CausesValidation = false;
				InitializeControl();
				InternalUpdateSource();
				base.Activate();
			}
			catch
			{
				if (_control != null)
				{
					_control.Dispose();
					_control = null;
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
				if (_control != null)
				{
					DAE.Client.IDataSourceReference sourceReference = _control as DAE.Client.IDataSourceReference;
					if (sourceReference != null)
						sourceReference.Source = null;
					_control.Dispose();
					_control = null;
				}
			}
		}

		// Element

		protected override void InternalLayout(Rectangle bounds)
		{
			Control.Bounds = bounds;
		}
	}

	/// <remarks> Encapsulated control must implement Alphora.Dataphor.DAE.Client.IColumnNameReference. </remarks>
	public abstract class ColumnElement : ControlElement, IColumnElement
	{
		// ColumnName

		private string _columnName = String.Empty;
		[DefaultValue("")]
		[TypeConverter(typeof(ColumnNameConverter))]
		[Description("The name of the column in the data source this element is associated with.")]
		public string ColumnName
		{
			get { return _columnName; }
			set
			{
				if (_columnName != value)
				{
					_columnName = value;
					if (Active)
						InternalUpdateColumnName();
				}
			}
		}

		protected virtual void InternalUpdateColumnName()
		{
			DAE.Client.IColumnNameReference nameReference = Control as DAE.Client.IColumnNameReference;
			if (nameReference != null)
				nameReference.ColumnName = ColumnName;
		}

		// Title		

		private string _title = String.Empty;
		[DefaultValue("")]
		[Description("Text that describes the control.")]
		public string Title
		{
			get { return _title; }
			set
			{
				if (_title != value)
				{
					_title = value == null ? String.Empty : value;
					if (Active)
						InternalUpdateTitle();
					UpdateLayout();
				}
			}
		}

		private string _allocatedTitle;

		protected void DeallocateAccelerator()
		{
			if (_allocatedTitle != null)
			{
				((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Deallocate(_allocatedTitle);
				_allocatedTitle = null;
			}
		}

		protected virtual void InternalUpdateTitle()
		{
			DeallocateAccelerator();
			_allocatedTitle = ((IAccelerates)FindParent(typeof(IAccelerates))).Accelerators.Allocate(_title.Equals(String.Empty) ? ColumnName : _title, false);
			SetControlText(_allocatedTitle);
		}

		protected virtual void SetControlText(string text)
		{
			Control.Text = text;
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
		public const int LabelVSpacing = 4;
		public const int LabelHSpacing = 2;

		// VerticalAlignment

		protected VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
		[DefaultValue(VerticalAlignment.Top)]
		[Description("When this control is given more space than it can use, this property specifies where the control will be placed within it's space.")]
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

		// TitleAlignment		

		private TitleAlignment _titleAlignment = TitleAlignment.Top;
		[DefaultValue(TitleAlignment.Top)]
		[Description("The alignment of the title relative to the control.")]
		public TitleAlignment TitleAlignment
		{
			get { return _titleAlignment; }
			set
			{
				if (_titleAlignment != value)
				{
					_titleAlignment = value;
					UpdateLayout();
				}
			}
		}

		#region Label

		private ExtendedLabel _label;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected ExtendedLabel Label
		{
			get { return _label; }
		}

		private void CreateLabel()
		{
			_label = new ExtendedLabel();
			try
			{
				_label.BackColor = ((Session)HostNode.Session).Theme.TextBackgroundColor;
				_label.AutoSize = true;
				_label.Parent = ((IWindowsContainerElement)Parent).Control;
				_label.OnMnemonic += new EventHandler(LabelMnemonic);
			}
			catch
			{
				DisposeLabel();
				throw;
			}
		}

		private void DisposeLabel()
		{
			if (_label != null)
			{
				_label.Dispose();
				_label = null;
			}
		}

		private void LabelMnemonic(object sender, EventArgs args)
		{
			if ((Control != null) && Control.CanFocus)
				Control.Focus();
		}

		#endregion

		// AverageCharPixelWidth

		private int _averageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected int AverageCharPixelWidth
		{
			get { return _averageCharPixelWidth; }
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

		private Size _labelPixelSize;

		protected override void SetControlText(string text)
		{
			_label.Text = text;
			using (Graphics graphics = _label.CreateGraphics())
				_labelPixelSize = Size.Ceiling(graphics.MeasureString(_label.Text, _label.Font));
		}
		
		// Node

		protected override void Activate()
		{
			CreateLabel();
			try
			{
				base.Activate();
				_averageCharPixelWidth = Element.GetAverageCharPixelWidth(Control);
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
			_label.Visible = (TitleAlignment != TitleAlignment.None) && GetVisible();
			base.InternalUpdateVisible();
		}

		protected virtual void LayoutControl(Rectangle bounds)
		{
			Control.Bounds = bounds;
		}

		protected override void InternalLayout(Rectangle bounds)
		{
			InternalUpdateVisible();

			//Alignment within the allotted space
			if (_verticalAlignment != VerticalAlignment.Top)
			{
				int deltaX = Math.Max(0, bounds.Height - MaxSize.Height);
				if (_verticalAlignment == VerticalAlignment.Middle)
					deltaX /= 2;
				bounds.Y += deltaX;
				bounds.Height -= deltaX;
			}

			// Title alignment
			if (TitleAlignment != TitleAlignment.None)
			{
				_label.Location = bounds.Location;
				if (TitleAlignment == TitleAlignment.Top)
				{
					int offset = _labelPixelSize.Height + LabelVSpacing;
					bounds.Y += offset;
					bounds.Height -= offset;
				}
				else
				{
					int offset = _labelPixelSize.Width + LabelHSpacing;
					bounds.X += offset;
					bounds.Width -= offset;
				}
			}
			
			// Enforce maximums
			if (EnforceMaxHeight())
			{
				Size size = bounds.Size;
				ConstrainMaxHeight(ref size, GetControlMaxHeight());
				bounds.Size = size;
			}

			LayoutControl(bounds);
		}
		
		private Size AdjustForAlignment(Size size)
		{
			switch (TitleAlignment)
			{
				case TitleAlignment.Top :
					if (size.Width < _labelPixelSize.Width)
						size.Width = _labelPixelSize.Width;
					size.Height += _labelPixelSize.Height + LabelVSpacing;
					break;
				case TitleAlignment.Left :
					size.Width += _labelPixelSize.Width + LabelHSpacing;
					break;
			}
			return size;
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
			return _averageCharPixelWidth;
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
		public const int DefaultWidth = 10;
		public const int MinWidth = 16;

		// Width

		protected int _width = DefaultWidth;
		[Publish(PublishMethod.Value)]
		[DefaultValue(DefaultWidth)]
		[Description("Specifies the width, in characters, of the control.")]
		public int Width
		{
			get { return _width; }
			set
			{
				if (_width != value)
				{
					_width = value;
					UpdateLayout();
				}
			}
		}

		// MaxWidth

		private int _maxWidth = -1;
		[DefaultValue(-1)]
		[Description("Specifies the maximum width, in characters, the control can grow to be.")]
		public int MaxWidth
		{
			get { return _maxWidth; }
			set
			{
				if (_maxWidth != value)
				{
					_maxWidth = value;
					if (Active)
						UpdateLayout();
				}
			}
		}

		// TextAlignment		

		private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
		[DefaultValue(HorizontalAlignment.Left)]
		[Description("Specifies the alignment of the text within the element.")]
		public HorizontalAlignment TextAlignment
		{
			get { return _textAlignment; }
			set
			{
				if (_textAlignment != value)
				{
					_textAlignment = value;
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
			return _width * AverageCharPixelWidth;
		}
		
		protected override int GetControlMaxWidth()
		{
			if (_maxWidth > 0)
				return _maxWidth * AverageCharPixelWidth;
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