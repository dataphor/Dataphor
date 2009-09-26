/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.ComponentModel;
#if !SILVERLIGHT
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;
#endif

namespace Alphora.Dataphor.DAE.Client.Design
{
	/// <summary> Specifies the type of document to use for multi-line property editing. </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class EditorDocumentTypeAttribute : Attribute
	{
		public EditorDocumentTypeAttribute(string ADocumentTypeID)
		{
			FDocumentTypeID = ADocumentTypeID;
		}

		private string FDocumentTypeID;
		public string DocumentTypeID
		{
			get { return FDocumentTypeID; }
			set { FDocumentTypeID = value; }
		}
	}

	#if !SILVERLIGHT
	/// <summary> Creates a list of available column names if and only if the linked DataSet is active. </summary>
	public class ColumnNameEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		protected virtual void SetEditorProperties(ITypeDescriptorContext AContext, ListBox AControl, string AValue)
		{
			AControl.BorderStyle = BorderStyle.None;
			DataSet LDataSet = DataSet(AContext);
			if (LDataSet.FieldCount > 0)
			{
				AControl.Items.Add("(none)");
				foreach (DataField LField in LDataSet.Fields)
					AControl.Items.Add(LField.ColumnName);
				if ((AValue == null) || (AValue == String.Empty))
					AControl.SelectedIndex = 0;
				else
					AControl.SelectedItem = AValue;
			}			
		}

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					if (DataLinkActive(AContext))
					{
						ListBox LListBox = new ListBox();
						try
						{
							SetEditorProperties(AContext, LListBox, AValue as string);
							LListBox.SelectedIndexChanged += new EventHandler(ValueChanged);
							FEditorService.DropDownControl(LListBox);
							if (LListBox.SelectedIndex == 0)
								AValue = String.Empty;
							else
								AValue = LListBox.SelectedItem;
						}
						finally
						{
							LListBox.SelectedIndexChanged -= new EventHandler(ValueChanged);
							LListBox.Dispose();
						}
					}
				}
			}
			return AValue;
		}

		private void ValueChanged(object ASender, EventArgs AArgs) 
		{
			if (FEditorService != null) 
				FEditorService.CloseDropDown();
		}

		/// <summary> Retrieves the DataSet associated with the property. </summary>
		/// <remarks> Override this method in derived classes to return a DataSet from which to build the list of column names. </remarks>
		/// <param name="AContext"> An ITypeDescriptorContext </param>
		/// <returns> The DataSet from which to build the column names list. </returns>
		public virtual DataSet DataSet(ITypeDescriptorContext AContext)
		{
			if
				(
					(AContext != null) && 
					(AContext.Instance != null) &&
					(AContext.Instance is IDataSourceReference) &&
					(((IDataSourceReference)AContext.Instance).Source != null)
				)
				return ((IDataSourceReference)AContext.Instance).Source.DataSet;
			return null;
		}

		protected bool DataLinkActive(ITypeDescriptorContext AContext)
		{
			DataSet LDataSet = DataSet(AContext);
			return (LDataSet != null) && (LDataSet.Active);		
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{ 
			if (DataLinkActive(AContext))
				return UITypeEditorEditStyle.DropDown;
			return base.GetEditStyle(AContext);
		}
	}

	/// <summary> Creates a list of available DataSessions by name. </summary>
	public class SessionEditor : UITypeEditor
	{
		private IWindowsFormsEditorService FEditorService = null;

		protected virtual void SetEditorProperties(ListBox AControl, string AValue)
		{
			AControl.BorderStyle = BorderStyle.None;
			if (DataSessionBase.Sessions.Count > 0)
			{
				AControl.Items.Add("(none)");
				lock (DataSessionBase.Sessions.SyncRoot)
				{
					foreach (DataSessionBase LSession in DataSessionBase.Sessions)
						AControl.Items.Add(LSession.SessionName);
				}
				if ((AValue == null) || (AValue == String.Empty))
					AControl.SelectedIndex = 0;
				else
					AControl.SelectedItem = AValue;
			}			
		}

		public override object EditValue(ITypeDescriptorContext AContext, IServiceProvider AProvider, object AValue) 
		{	
			if (AContext != null && AContext.Instance != null && AProvider != null) 
			{
				FEditorService = (IWindowsFormsEditorService)AProvider.GetService(typeof(IWindowsFormsEditorService));
				if (FEditorService != null)
				{
					ListBox LListBox = new ListBox();
					try
					{
						SetEditorProperties(LListBox, AValue as string);
						LListBox.SelectedIndexChanged += new EventHandler(ValueChanged);
						FEditorService.DropDownControl(LListBox);
						if (LListBox.SelectedIndex == 0)
							AValue = String.Empty;
						else
							AValue = LListBox.SelectedItem;
					}
					finally
					{
						LListBox.SelectedIndexChanged -= new EventHandler(ValueChanged);
						LListBox.Dispose();
					}
				}
			}
			return AValue;
		}

		private void ValueChanged(object ASender, EventArgs AArgs) 
		{
			if (FEditorService != null) 
				FEditorService.CloseDropDown();
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext AContext) 
		{ 
			if (AContext != null && AContext.Instance != null) 
				return UITypeEditorEditStyle.DropDown;
			return base.GetEditStyle(AContext);
		}
	}
	#endif
}
