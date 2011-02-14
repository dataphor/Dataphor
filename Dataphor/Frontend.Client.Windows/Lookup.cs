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
	public abstract class LookupBase : ControlContainer, ILookup
	{
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Source = null;
		}

		#region Source

		private ISource _source;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specified the source node the control will be attached to.")]
		public ISource Source
		{
			get { return _source; }
			set
			{
				if (_source != null)
					_source.Disposed -= new EventHandler(SourceDisposed);
				_source = value;
				if (_source != null)
					_source.Disposed += new EventHandler(SourceDisposed);
			}
		}
		
		protected virtual void SourceDisposed(object sender, EventArgs args)
		{
			Source = null;
		}

		#endregion

		#region ReadOnly

		private bool _readOnly;
		[DefaultValue(false)]
		[Description("If set to true then this element will be made read only and will not update the source it is attached to.")]
		public bool ReadOnly
		{
			get { return _readOnly; }
			set 
			{ 
				if (_readOnly != value)
				{
					_readOnly = value; 
					if (Active)
						InternalUpdateReadOnly();
				}
			}
		}

		public virtual bool GetReadOnly()
		{
			return _readOnly;
		}

		protected virtual void InternalUpdateReadOnly()
		{
			InternalUpdateEnabled();
		}

		#endregion

		#region Document

		private string _document = String.Empty;
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		[Description("A document expression that returns a document used to display a form.")]
		public string Document
		{
			get { return _document; }
			set 
			{ 
				_document = (value == null ? String.Empty : value);
				if (Active)
					InternalUpdateEnabled();
			}
		}

		public virtual bool GetEnabled()
		{
			return _document != String.Empty;
		}

		protected virtual void InternalUpdateEnabled()
		{
			LookupControl.Enabled = GetEnabled() && !GetReadOnly();
		}

		#endregion

		#region AutoLookup

		private bool _autoLookup;
		[DefaultValue(false)]
		[Description("When true, the control automatically invokes its lookup the first time focus reaches it.")]
		public bool AutoLookup
		{
			get { return _autoLookup; }
			set 
			{
				if (_autoLookup != value)
				{
					_autoLookup = value;
					if (Active)
						InternalUpdateAutoLookup();
				}
			}
		}

		private bool _autoLookupWhenNotNil;
		[DefaultValue(false)]
		[Description("When true, the control will still perform an auto-lookup even when there isn't at least one column that is not nil.")]
		public bool AutoLookupWhenNotNil
		{
			get { return _autoLookupWhenNotNil; }
			set { _autoLookupWhenNotNil = value; }
		}

		protected virtual void InternalUpdateAutoLookup()
		{
			LookupControl.AutoLookup = _autoLookup;
		}

		public void ResetAutoLookup()
		{
			if (Active)
				LookupControl.ResetAutoLookup();
		}

		/// <remarks> Only enable auto-lookup if one or more of the columns looked up by this control is nil. </remarks>
		private bool ControlQueryAutoLookupEnabled(Alphora.Dataphor.DAE.Client.Controls.LookupBase panel)
		{
			if (Source != null)
			{
				if (Source.IsEmpty || _autoLookupWhenNotNil)
					return true;

				string[] columnNames = GetColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters);
				foreach (string columnName in columnNames)
					if (!Source[columnName].HasValue())
						return true;
			}
			return false;
		}

		#endregion

		#region Lookup Properties

		// MasterKeyNames

		private string _masterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) which provide the values to filter the target set.  Used with DetailKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set { _masterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string _detailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) by which the target set will be filtered.  Used with MasterKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set { _detailKeyNames = value == null ? String.Empty : value; }
		}

		public abstract string GetLookupColumnNames();

		public abstract string GetColumnNames();

		#endregion

		#region Lookup

		protected void DoLookup(object sender, DAE.Client.Controls.LookupEventArgs args)
		{
			try
			{
				_lookupKey = args.KeyData;
				LookupUtility.DoLookup(this, new FormInterfaceHandler(LookupAccepted), new FormInterfaceHandler(LookupRejected), null);
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);	//Don't rethrow
			}
		}

		public void LookupFormInitialize(IFormInterface form) 
		{
			IWindowsFormInterface localForm = (IWindowsFormInterface)form;
			localForm.BeginUpdate();
			try
			{
				localForm.Form.StartPosition = WinForms.FormStartPosition.Manual;
				localForm.IsLookup = true;

				IWindowsFormInterface ownerForm = (IWindowsFormInterface)FindParent(typeof(IWindowsFormInterface));
				if (ownerForm != null)
				{
					localForm.Form.Owner = (WinForms.Form)ownerForm.Form;
					// LForm.Form.ShowInTaskbar = false;  this would be okay, except that alt-tab doesn't seem to work unless the owned form is also in the task bar
				}

				Size natural = localForm.FormNaturalSize();
				Rectangle bounds =
					DAE.Client.Controls.LookupBoundsUtility.DetermineBounds
					(
						natural,
						localForm.FormMinSize(),
						Control
					);
				localForm.Form.Bounds = bounds;
				if (bounds.Size == natural)
					localForm.Form.AutoResize = true;
				localForm.Form.Activated += new EventHandler(LookupFormActivated);
			}
			finally
			{
				localForm.EndUpdate(false);
			}
		}

		private Keys _lookupKey;

		/// <remarks> Pass along the keystroke that initiated the lookup. </remarks>
		private void LookupFormActivated(object sender, EventArgs args)
		{
			// Detach our handler
			((IBaseForm)sender).Activated -= new EventHandler(LookupFormActivated);

			if (_lookupKey != Keys.None)
			{
				int virtualKeyCode = (int)_lookupKey & 0xFFFF;
				UnsafeNativeMethods.PostMessage(ControlUtility.InnermostActiveControl((WinForms.ContainerControl)sender).Handle, NativeMethods.WM_KEYDOWN, (IntPtr)virtualKeyCode, IntPtr.Zero);
			}
		}

		private void LookupRejected(IFormInterface form) 
		{
			FocusControl();
		}
	
		protected bool CanModify()
		{
			return !GetReadOnly() && (Source != null) && (Source.DataView != null);
		}
		
		private void LookupAccepted(IFormInterface form) 
		{
			FocusControl();

			if (CanModify())
			{
				string[] lookupColumns = GetLookupColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters);
				string[] sourceColumns = GetColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters);

				// Assign the field values
				for (int i = 0; i < sourceColumns.Length; i++)
				{
					DAE.Client.DataField field = form.MainSource.DataView.Fields[lookupColumns[i].Trim()];
					Source.DataView.Fields[sourceColumns[i].Trim()].Value = field.HasValue() ? field.Value : null;
				}

				// Move to the next control
				FindParent(typeof(IWindowsFormInterface)).HandleEvent(new AdvanceFocusEvent(true));
			}
		}

		#endregion

		#region Control

		protected DAE.Client.Controls.LookupBase LookupControl
		{
			get { return (DAE.Client.Controls.LookupBase)Control; }
		}

		private void FocusControl()
		{
			LookupControl.FocusControl();
		}

		protected override void InitializeControl()
		{
			base.InitializeControl();
			LookupControl.Lookup += new DAE.Client.Controls.LookupEventHandler(DoLookup);
			LookupControl.QueryAutoLookupEnabled += new Alphora.Dataphor.DAE.Client.Controls.QueryAutoLookupEnabledHandler(ControlQueryAutoLookupEnabled);
			InternalUpdateReadOnly();
			InternalUpdateEnabled();
			InternalUpdateAutoLookup();
		}

		#endregion

	}
	
	[DesignerImage("Image('Frontend', 'Nodes.QuickLookup')")]
	[DesignerCategory("Data Controls")]
	public class QuickLookup : LookupBase, IQuickLookup
	{
		public const int LabelVSpacing = 4;
		public const int LabelHSpacing = 2;

		#region VerticalAlignment

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

		#endregion

		#region TitleAlignment		

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

		#endregion

		#region ColumnName Properties

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
			if ((Title == String.Empty) && Active)
				InternalUpdateTitle();
		}

		public override string GetColumnNames()
		{
			return ColumnName;
		}

		// LookupColumnName

		private string _lookupColumnName = String.Empty;
		[DefaultValue("")]
		[Description("The column that will be read from the lookup source.")]
		public string LookupColumnName
		{
			get { return _lookupColumnName; }
			set { _lookupColumnName = (value == null ? String.Empty : value); }
		}

		public override string GetLookupColumnNames()
		{
			return _lookupColumnName;
		}

		#endregion

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

		// ControlContainer

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.LookupBox();
		}

		protected override bool AlwaysAccellerate()
		{
			return false;
		}
		
		// AverageCharPixelWidth

		private int _averageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected int AverageCharPixelWidth
		{
			get { return _averageCharPixelWidth; }
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

		protected override void AddChild(INode child)
		{
			base.AddChild(child);
			((IWindowsElement)child).SuppressMargins = true;

			// Remove the title from any titled element that is added
			if (Active)
			{
				ITitledElement element = child as ITitledElement;
				if (element != null)
					element.TitleAlignment = TitleAlignment.None;
			}
		}

		protected override void RemoveChild(INode child)
		{
			((IWindowsElement)child).SuppressMargins = false;
			base.RemoveChild(child);
		}

		private Size _labelPixelSize;

		protected override void SetControlText(string text)
		{
			_label.Text = text;
			using (Graphics graphics = _label.CreateGraphics())
				_labelPixelSize = Size.Ceiling(graphics.MeasureString(_label.Text, _label.Font));
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			UpdateLayout();
		}

		protected override void InternalUpdateVisible() 
		{
			_label.Visible = GetVisible();
			base.InternalUpdateVisible();
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		#region Layout

		protected override void LayoutControl(Rectangle bounds)
		{
			// Alignment within the allotted space
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
					bounds.Y += _labelPixelSize.Height + LabelVSpacing;
				else
					bounds.X += _labelPixelSize.Width + LabelHSpacing;
			}
			Control.Location = bounds.Location;	// Only locate... don't size the LookupBox (it sizes itself)
		}

		protected override void LayoutChild(IWindowsElement child, Rectangle bounds)
		{
			if ((child != null) && child.GetVisible())
			{
				switch (TitleAlignment)
				{
					case TitleAlignment.Top : bounds.Height -= _labelPixelSize.Height + LabelVSpacing; break;
					case TitleAlignment.Left : bounds.Width -= _labelPixelSize.Width + LabelHSpacing; break;
				}
				child.Layout(new Rectangle(Control.DisplayRectangle.Location, bounds.Size - (Control.Size - Control.DisplayRectangle.Size)));
			}
		}

		#endregion

		#region Sizing

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

		protected override Size InternalMinSize
		{
			get
			{
				return AdjustForAlignment(base.InternalMinSize);
			}
		}
		
		protected override Size InternalMaxSize
		{
			get
			{
				return AdjustForAlignment(base.InternalMaxSize);
			}
		}
		
		protected override Size InternalNaturalSize
		{
			get
			{
				return AdjustForAlignment(base.InternalNaturalSize);
			}
		}

		#endregion
	}

	[Description("Lookup of one or more columns.")]
	[DesignerImage("Image('Frontend', 'Nodes.FullLookup')")]
	[DesignerCategory("Data Controls")]
	public class FullLookup : LookupBase, IFullLookup
	{
		#region ColumnName Properties

		private string _columnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be set by the lookup.")]
		public string ColumnNames
		{
			get { return _columnNames; }
			set { _columnNames = (value == null ? String.Empty : value); }
		}

		public override string GetColumnNames()
		{
			return _columnNames;
		}

		// LookupColumnNames

		private string _lookupColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be read from the lookups' source.")]
		public string LookupColumnNames
		{
			get { return _lookupColumnNames; }
			set { _lookupColumnNames = (value == null ? String.Empty : value); }
		}

		public override string GetLookupColumnNames()
		{
			return _lookupColumnNames;
		}

		#endregion

		// ControlContainer

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.LookupPanel();
		}

		protected override void InitializeControl()
		{
			base.InitializeControl();
			Control.Enter += new EventHandler(ControlGotFocus);
			((DAE.Client.Controls.LookupPanel)Control).ClearValue += new EventHandler(ControlClearValue);
		}

		protected void ControlGotFocus(object sender, EventArgs args)
		{
			FindParent(typeof(IFormInterface)).BroadcastEvent(new FocusChangedEvent(this));
		}

		private void ControlClearValue(object sender, EventArgs e)
		{
			if (CanModify())
			{
				string[] sourceColumns = GetColumnNames().Split(DAE.Client.DataView.ColumnNameDelimiters);

				// Clear the field values
				for (int i = 0; i < sourceColumns.Length; i++)
				{
					var field = Source.DataView.Fields[sourceColumns[i].Trim()];
					if (field.HasValue())
						field.ClearValue();
				}
			}
		}

		// Element

		public override bool GetDefaultTabStop()
		{
			return true;
		}

		protected override Size InternalMinSize
		{
			get
			{
				if ((_child != null) && _child.GetVisible())
					return _child.MinSize;
				else
					return new Size(0, LookupControl.MinDisplayHeight());
			}
		}
	}
}
