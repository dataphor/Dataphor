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
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
			Source = null;
		}

		#region Source

		private ISource FSource;
		[TypeConverter(typeof(NodeReferenceConverter))]
		[Description("Specified the source node the control will be attached to.")]
		public ISource Source
		{
			get { return FSource; }
			set
			{
				if (FSource != null)
					FSource.Disposed -= new EventHandler(SourceDisposed);
				FSource = value;
				if (FSource != null)
					FSource.Disposed += new EventHandler(SourceDisposed);
			}
		}
		
		protected virtual void SourceDisposed(object ASender, EventArgs AArgs)
		{
			Source = null;
		}

		#endregion

		#region ReadOnly

		private bool FReadOnly;
		[DefaultValue(false)]
		[Description("If set to true then this element will be made read only and will not update the source it is attached to.")]
		public bool ReadOnly
		{
			get { return FReadOnly; }
			set 
			{ 
				if (FReadOnly != value)
				{
					FReadOnly = value; 
					if (Active)
						InternalUpdateReadOnly();
				}
			}
		}

		public virtual bool GetReadOnly()
		{
			return FReadOnly;
		}

		protected virtual void InternalUpdateReadOnly()
		{
			InternalUpdateEnabled();
		}

		#endregion

		#region Document

		private string FDocument = String.Empty;
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", typeof(System.Drawing.Design.UITypeEditor))]
		[DocumentExpressionOperator("Form")]
		[Description("A document expression that returns a document used to display a form.")]
		public string Document
		{
			get { return FDocument; }
			set 
			{ 
				FDocument = (value == null ? String.Empty : value);
				if (Active)
					InternalUpdateEnabled();
			}
		}

		public virtual bool GetEnabled()
		{
			return FDocument != String.Empty;
		}

		protected virtual void InternalUpdateEnabled()
		{
			LookupControl.Enabled = GetEnabled() && !GetReadOnly();
		}

		#endregion

		#region AutoLookup

		private bool FAutoLookup;
		[DefaultValue(false)]
		[Description("When true, the control automatically invokes its lookup the first time focus reaches it.")]
		public bool AutoLookup
		{
			get { return FAutoLookup; }
			set 
			{
				if (FAutoLookup != value)
				{
					FAutoLookup = value;
					if (Active)
						InternalUpdateAutoLookup();
				}
			}
		}

		private bool FAutoLookupWhenNotNil;
		[DefaultValue(false)]
		[Description("When true, the control will still perform an auto-lookup even when there isn't at least one column that is not nil.")]
		public bool AutoLookupWhenNotNil
		{
			get { return FAutoLookupWhenNotNil; }
			set { FAutoLookupWhenNotNil = value; }
		}

		protected virtual void InternalUpdateAutoLookup()
		{
			LookupControl.AutoLookup = FAutoLookup;
		}

		public void ResetAutoLookup()
		{
			if (Active)
				LookupControl.ResetAutoLookup();
		}

		/// <remarks> Only enable auto-lookup if one or more of the columns looked up by this control is nil. </remarks>
		private bool ControlQueryAutoLookupEnabled(Alphora.Dataphor.DAE.Client.Controls.LookupBase APanel)
		{
			if (Source != null)
			{
				if (Source.IsEmpty || FAutoLookupWhenNotNil)
					return true;

				string[] LColumnNames = GetColumnNames().Split(DAE.Client.DataView.CColumnNameDelimiters);
				foreach (string LColumnName in LColumnNames)
					if (!Source[LColumnName].HasValue())
						return true;
			}
			return false;
		}

		#endregion

		#region Lookup Properties

		// MasterKeyNames

		private string FMasterKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) which provide the values to filter the target set.  Used with DetailKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set { FMasterKeyNames = value == null ? String.Empty : value; }
		}

		// DetailKeyNames

		private string FDetailKeyNames = String.Empty;
		[DefaultValue("")]
		[Description("A set of keys (comma or semicolon seperated) by which the target set will be filtered.  Used with MasterKeyNames to create a master detail filter on the lookup form data set.  Should also be set in the Document property if the lookup form is a derived page.")]
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set { FDetailKeyNames = value == null ? String.Empty : value; }
		}

		public abstract string GetLookupColumnNames();

		public abstract string GetColumnNames();

		#endregion

		#region Lookup

		protected void DoLookup(object ASender, DAE.Client.Controls.LookupEventArgs AArgs)
		{
			try
			{
				FLookupKey = AArgs.KeyData;
				LookupUtility.DoLookup(this, new FormInterfaceHandler(LookupAccepted), new FormInterfaceHandler(LookupRejected), null);
			}
			catch (Exception AException)
			{
				Session.HandleException(AException);	//Don't rethrow
			}
		}

		public void LookupFormInitialize(IFormInterface AForm) 
		{
			IWindowsFormInterface LForm = (IWindowsFormInterface)AForm;
			LForm.BeginUpdate();
			try
			{
				LForm.Form.StartPosition = WinForms.FormStartPosition.Manual;
				LForm.IsLookup = true;

				IWindowsFormInterface LOwnerForm = (IWindowsFormInterface)FindParent(typeof(IWindowsFormInterface));
				if (LOwnerForm != null)
				{
					LForm.Form.Owner = (WinForms.Form)LOwnerForm.Form;
					// LForm.Form.ShowInTaskbar = false;  this would be okay, except that alt-tab doesn't seem to work unless the owned form is also in the task bar
				}

				Size LNatural = LForm.FormNaturalSize();
				Rectangle LBounds =
					DAE.Client.Controls.LookupBoundsUtility.DetermineBounds
					(
						LNatural,
						LForm.FormMinSize(),
						Control
					);
				LForm.Form.Bounds = LBounds;
				if (LBounds.Size == LNatural)
					LForm.Form.AutoResize = true;
				LForm.Form.Activated += new EventHandler(LookupFormActivated);
			}
			finally
			{
				LForm.EndUpdate(false);
			}
		}

		private Keys FLookupKey;

		/// <remarks> Pass along the keystroke that initiated the lookup. </remarks>
		private void LookupFormActivated(object ASender, EventArgs AArgs)
		{
			// Detach our handler
			((IBaseForm)ASender).Activated -= new EventHandler(LookupFormActivated);

			if (FLookupKey != Keys.None)
			{
				int LVirtualKeyCode = (int)FLookupKey & 0xFFFF;
				UnsafeNativeMethods.PostMessage(ControlUtility.InnermostActiveControl((WinForms.ContainerControl)ASender).Handle, NativeMethods.WM_KEYDOWN, (IntPtr)LVirtualKeyCode, IntPtr.Zero);
			}
		}

		private void LookupRejected(IFormInterface AForm) 
		{
			FocusControl();
		}
	
		private void LookupAccepted(IFormInterface AForm) 
		{
			FocusControl();

			if (!GetReadOnly() && (Source != null) && (Source.DataView != null))
			{
				string[] LLookupColumns = GetLookupColumnNames().Split(DAE.Client.DataView.CColumnNameDelimiters);
				string[] LSourceColumns = GetColumnNames().Split(DAE.Client.DataView.CColumnNameDelimiters);

				// Assign the field values
				for (int i = 0; i < LSourceColumns.Length; i++)
				{
					DAE.Client.DataField LField = AForm.MainSource.DataView.Fields[LLookupColumns[i].Trim()];
					Source.DataView.Fields[LSourceColumns[i].Trim()].Value = LField.HasValue() ? LField.Value : null;
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
		public const int CLabelVSpacing = 4;
		public const int CLabelHSpacing = 2;

		#region VerticalAlignment

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

		#endregion

		#region TitleAlignment		

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

		#endregion

		#region ColumnName Properties

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
			if ((Title == String.Empty) && Active)
				InternalUpdateTitle();
		}

		public override string GetColumnNames()
		{
			return ColumnName;
		}

		// LookupColumnName

		private string FLookupColumnName = String.Empty;
		[DefaultValue("")]
		[Description("The column that will be read from the lookup source.")]
		public string LookupColumnName
		{
			get { return FLookupColumnName; }
			set { FLookupColumnName = (value == null ? String.Empty : value); }
		}

		public override string GetLookupColumnNames()
		{
			return FLookupColumnName;
		}

		#endregion

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

		private int FAverageCharPixelWidth;
		[Publish(PublishMethod.None)]
		[Browsable(false)]
		protected int AverageCharPixelWidth
		{
			get { return FAverageCharPixelWidth; }
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

		protected override void AddChild(INode AChild)
		{
			base.AddChild(AChild);
			((IWindowsElement)AChild).SuppressMargins = true;

			// Remove the title from any titled element that is added
			if (Active)
			{
				ITitledElement LElement = AChild as ITitledElement;
				if (LElement != null)
					LElement.TitleAlignment = TitleAlignment.None;
			}
		}

		protected override void RemoveChild(INode AChild)
		{
			((IWindowsElement)AChild).SuppressMargins = false;
			base.RemoveChild(AChild);
		}

		private Size FLabelPixelSize;

		protected override void SetControlText(string AText)
		{
			FLabel.Text = AText;
			using (Graphics LGraphics = FLabel.CreateGraphics())
				FLabelPixelSize = Size.Ceiling(LGraphics.MeasureString(FLabel.Text, FLabel.Font));
		}

		protected override void InternalUpdateTitle()
		{
			base.InternalUpdateTitle();
			UpdateLayout();
		}

		protected override void InternalUpdateVisible() 
		{
			FLabel.Visible = GetVisible();
			base.InternalUpdateVisible();
		}

		public override bool GetDefaultTabStop()
		{
			return false;
		}

		#region Layout

		protected override void LayoutControl(Rectangle ABounds)
		{
			// Alignment within the allotted space
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
					ABounds.Y += FLabelPixelSize.Height + CLabelVSpacing;
				else
					ABounds.X += FLabelPixelSize.Width + CLabelHSpacing;
			}
			Control.Location = ABounds.Location;	// Only locate... don't size the LookupBox (it sizes itself)
		}

		protected override void LayoutChild(IWindowsElement AChild, Rectangle ABounds)
		{
			if ((AChild != null) && AChild.GetVisible())
			{
				switch (TitleAlignment)
				{
					case TitleAlignment.Top : ABounds.Height -= FLabelPixelSize.Height + CLabelVSpacing; break;
					case TitleAlignment.Left : ABounds.Width -= FLabelPixelSize.Width + CLabelHSpacing; break;
				}
				AChild.Layout(new Rectangle(Control.DisplayRectangle.Location, ABounds.Size - (Control.Size - Control.DisplayRectangle.Size)));
			}
		}

		#endregion

		#region Sizing

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

		private string FColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be set by the lookup.")]
		public string ColumnNames
		{
			get { return FColumnNames; }
			set { FColumnNames = (value == null ? String.Empty : value); }
		}

		public override string GetColumnNames()
		{
			return FColumnNames;
		}

		// LookupColumnNames

		private string FLookupColumnNames = String.Empty;
		[DefaultValue("")]
		[Description("The columns (comma or semicolon seperated) that will be read from the lookups' source.")]
		public string LookupColumnNames
		{
			get { return FLookupColumnNames; }
			set { FLookupColumnNames = (value == null ? String.Empty : value); }
		}

		public override string GetLookupColumnNames()
		{
			return FLookupColumnNames;
		}

		#endregion

		// ControlContainer

		protected override WinForms.Control CreateControl()
		{
			return new DAE.Client.Controls.LookupPanel();
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
				if ((FChild != null) && FChild.GetVisible())
					return FChild.MinSize;
				else
					return new Size(0, LookupControl.MinDisplayHeight());
			}
		}
	}
}
