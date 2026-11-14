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
		public EditorDocumentTypeAttribute(string documentTypeID)
		{
			_documentTypeID = documentTypeID;
		}

		private string _documentTypeID;
		public string DocumentTypeID
		{
			get { return _documentTypeID; }
			set { _documentTypeID = value; }
		}
	}

	#if !SILVERLIGHT
	/// <summary> Creates a list of available column names if and only if the linked DataSet is active. </summary>
	public class ColumnNameEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		protected virtual void SetEditorProperties(ITypeDescriptorContext context, ListBox control, string tempValue)
		{
			control.BorderStyle = BorderStyle.None;
			DataSet dataSet = DataSet(context);
			if (dataSet.FieldCount > 0)
			{
				control.Items.Add("(none)");
				foreach (DataField field in dataSet.Fields)
					control.Items.Add(field.ColumnName);
				if ((tempValue == null) || (tempValue == String.Empty))
					control.SelectedIndex = 0;
				else
					control.SelectedItem = tempValue;
			}			
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	
			if (context != null && context.Instance != null && provider != null) 
			{
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					if (DataLinkActive(context))
					{
						ListBox listBox = new ListBox();
						try
						{
							SetEditorProperties(context, listBox, tempValue as string);
							listBox.SelectedIndexChanged += new EventHandler(ValueChanged);
							_editorService.DropDownControl(listBox);
							if (listBox.SelectedIndex == 0)
								tempValue = String.Empty;
							else
								tempValue = listBox.SelectedItem;
						}
						finally
						{
							listBox.SelectedIndexChanged -= new EventHandler(ValueChanged);
							listBox.Dispose();
						}
					}
				}
			}
			return tempValue;
		}

		private void ValueChanged(object sender, EventArgs args) 
		{
			if (_editorService != null) 
				_editorService.CloseDropDown();
		}

		/// <summary> Retrieves the DataSet associated with the property. </summary>
		/// <remarks> Override this method in derived classes to return a DataSet from which to build the list of column names. </remarks>
		/// <param name="context"> An ITypeDescriptorContext </param>
		/// <returns> The DataSet from which to build the column names list. </returns>
		public virtual DataSet DataSet(ITypeDescriptorContext context)
		{
			if
				(
					(context != null) && 
					(context.Instance != null) &&
					(context.Instance is IDataSourceReference) &&
					(((IDataSourceReference)context.Instance).Source != null)
				)
				return ((IDataSourceReference)context.Instance).Source.DataSet;
			return null;
		}

		protected bool DataLinkActive(ITypeDescriptorContext context)
		{
			DataSet dataSet = DataSet(context);
			return (dataSet != null) && (dataSet.Active);		
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{ 
			if (DataLinkActive(context))
				return UITypeEditorEditStyle.DropDown;
			return base.GetEditStyle(context);
		}
	}

	/// <summary> Creates a list of available DataSessions by name. </summary>
	public class SessionEditor : UITypeEditor
	{
		private IWindowsFormsEditorService _editorService = null;

		protected virtual void SetEditorProperties(ListBox control, string tempValue)
		{
			control.BorderStyle = BorderStyle.None;
			if (DataSession.Sessions.Count > 0)
			{
				control.Items.Add("(none)");
				lock (DataSession.Sessions.SyncRoot)
				{
					foreach (DataSession session in DataSession.Sessions)
						control.Items.Add(session.SessionName);
				}
				if ((tempValue == null) || (tempValue == String.Empty))
					control.SelectedIndex = 0;
				else
					control.SelectedItem = tempValue;
			}			
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object tempValue) 
		{	
			if (context != null && context.Instance != null && provider != null) 
			{
				_editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
				if (_editorService != null)
				{
					ListBox listBox = new ListBox();
					try
					{
						SetEditorProperties(listBox, tempValue as string);
						listBox.SelectedIndexChanged += new EventHandler(ValueChanged);
						_editorService.DropDownControl(listBox);
						if (listBox.SelectedIndex == 0)
							tempValue = String.Empty;
						else
							tempValue = listBox.SelectedItem;
					}
					finally
					{
						listBox.SelectedIndexChanged -= new EventHandler(ValueChanged);
						listBox.Dispose();
					}
				}
			}
			return tempValue;
		}

		private void ValueChanged(object sender, EventArgs args) 
		{
			if (_editorService != null) 
				_editorService.CloseDropDown();
		}
	 
		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) 
		{ 
			if (context != null && context.Instance != null) 
				return UITypeEditorEditStyle.DropDown;
			return base.GetEditStyle(context);
		}
	}
	#endif
}
